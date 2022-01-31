# Release notes for Corvus.Identity v3.

## v3.1

New features:

The `IAccessTokenSource` adapter for `Azure.Core`-style `TokenCredentials` (and, by extension, also the `Microsoft.Rest` adapters) now do token caching. This means for for credential types where the underlying mechanism does not have built-in caching (most notably Azure Managed Identities), and the client code also doesn't do any credential caching (which can happen with some Autorest client usage styles) we will now typically be able to avoid calls to the underlying credential provider in cases where the most recently received token for a given scope hasn't expired yet.

## v3.0

New features:

Targets .NET 6.0 only.

Client identity configuration, with support for `Azure.Core`-style authentication. See [Configuration and Azure AD Client Identities](../articles/configuration.md) for details



### Potentially breaking changes

There have been additions to two interfaces. These are breaking changes for any client of this library that chooses to implement these interfaces. However, these are not really designed to be implemented by other libraries; these interfaces exist to enable the implementations to be consumed through DI.

The changes will be binary-compatible for code that only consumes these interfaces, because such code won't even know about the new members. The changes are also source-compatible except for projects that treat warnings as errors, in which case the deprecation of an existing member will cause a compiler error.

* `IAccessTokenSource` has a new `GetReplacementForFailedAccessTokenAsync` method to enable client code to report that some credentials are no longer any good
* `IAzureTokenCredentialSource` has a new `GetReplacementForFailedTokenCredentialAsync` method to enable client code to report that some credentials are no longer any good
* `IAzureTokenCredentialSource` deprecates the `GetAccessTokenAsync` method, and adds a `GetTokenCredentialAsync` method, which is what that method should have been called all along; these methods are logically identical, although `GetTokenCredentialAsync` adds support for a `CancellationToken`
