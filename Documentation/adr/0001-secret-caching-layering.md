# `Corvus.Identity` Azure Key Vault secret caching

## Status

Proposed

## Context

Azure Key Vault is a relatively slow service, and can sometimes take several seconds to return information. Since `Corvus.Identity` often needs to read credentials out of Azure Key Vault, we want to be able to cache information to avoid reading the same secrets repeatedly. We also require an application-controllable cache invalidation mechanism to enable key rotation: the application needs to be able to let `Corvus.Identity` know that the credentials loaded for a particular identity are no longer working, and that if possible, it should try to reload them from source.

### Option 1: embed caching in the Azure.Core pipeline

The Azure Client SDK, like all Azure.Core-based libraries, enables customization of communication via its HTTP Pipeline. We can register custom handlers in this pipeline, and it would be possible to implement caching in this way.

This has the following advantages:

* code designed to work with the Key Vault library does not need to be modified: we can just provide it with a suitably-configured `SecretClient`
* the full functionality of Key Vault is available

It has the following disadvantages:

* if the only thing an application cares about is the actual secret value (just some text), this approach ends up with a great deal of complexity and runtime effort to cache entire responses (which it has to do so that the layers above it in the pipeline continue to work correctly)
* it's not obvious where to put cache invalidation mechanisms—burying the caching invisibly in the pipeline becomes a disadvantage for applications that care about this
* because the cache needs to make a copy of the real response, adding cache entries involves asynchronous work (copying the stream) and none of the readily available ways of caching data in .NET cope especially well with that

Also, at the moment there's no good way to enable application code to bring its own `SecretClientOptions`. With this implementation approach we need to set our own options to ensure that our caching components go into the pipeline. We could let the application pass in an options object that we then modify. I'd much prefer to make a clone and modify that, but it's not currently possible with the Azure Client SDK.

### Option 2: cache as layer over Key Vault

Another approach is to write a complete wrapper for the Azure Key Vault client SDK, and to implement the caching in that layer.

This has the following benefits:

* we can cache only what we need (currently just the secret text value), avoiding dealing with async operations during a cache get-or-add operation
* the implementation is simpler because there's no need to fake things up well enough to fool whatever else may be in the HTTP pipeline
* the client identity affinity can be expressed directly in the API

This has the following disadvantages

* code needs to be written specially or modified to use this cache
* this may limit the use of Key Vault—any capabilities not duplicated in the wrapper API will be unavailable

### Option 3: distinct cache mechanism

Another implementation option is for the secret cache to be a separate feature used alongside the `SecretClient`. An application can consult the cache and then fall back to the `SecretClient` if necessary.

This has the following advantages (which include all the advantages of Option 2):

* the cache becomes very simple: it only has to be a cache, and does not need to be anything else (e.g., a proxy for the Key Vault)
* the client identity affinity can be expressed directly in the API
* the full power of the Azure Key Vault client SDK remains available because that's what the application uses

This has these disadvantages:

* although the application can use any feature of the Azure Key Vault client SDK, it will only enjoy caching for those bits that the cache explicitly supports; in cases where other features are needed, code has to fall back to the "not using the cache" path
* code still requires some modification to use the cache (although less than with Option 2)


## Decision

We are using option 3: the `ICachingKeyVaultSecretClientFactory` provides a cache that can be consulted, with use of `SecretClient` becoming the fallback. The application then adds newly fetched data to the cache. This interface also provides invalidation support.

