# Token source implementation notes

This describes the implementation approach taken for the various credential source implementations.

`Corvus.Identity` can supply applications with credentials in three forms:

* An `Azure.Core`-style `TokenCredential`
* A plain `string` access token for use directly with HTTP `Authentication` headers
* An old `Microsoft.Rest`-style `ITokenProvider`

For each of these, there are three ways in which application code can determine which principal the credentials should represent:

* Non-committal—the code uses an interface that could return credentials representing any principal; the choice is determined by the particular credential source instance the code is using in any particular context
* Service identity (ambient process-wide identity; often, but not always, an Azure Managed Identity)
* Specified in a `ClientIdentityConfiguration`

`Corvus.Identity` defines interfaces that represent the combinations of these options. This table has a row for each form of credentials, and columns for each of the three ways in which application code might determine the principal to use, and shows which interface to use in each case:

|                                 | Source decides                      | Service Identity                                   | `ClientIdentityConfiguration`                               |
| --- | --- | --- | --- |
| `Azure.Core.TokenCredential`    | `IAzureTokenCredentialSource`       | `IServiceIdentityAzureTokenCredentialSource`       | `IAzureTokenCredentialSourceFromDynamicConfiguration`       |
| Raw access tokens               | `IAccessTokenSource`                | `IServiceIdentityAccessTokenSource`                | `IAccessTokenSourceFromDynamicConfiguration`                |
| `Microsoft.Rest.ITokenProvider` | `IMicrosoftRestTokenProviderSource` | `IServiceIdentityMicrosoftRestTokenProviderSource` | `IMicrosoftRestTokenProviderSourceFromDynamicConfiguration` |

**Note**: the reason the interfaces in the third column refer to "dynamic configuration" is that if we just say "configuration," methods that added implementations to DI would have names such as `AddAzureTokenCredentialSourceFromConfiguration`, which would sound like it would be reading settings from application configuration. The name `AddAzureTokenCredentialSourceFromDynamicConfiguration` does a slightly better job of conveying the fact that this adds an implementation which expects to be passed configuration that might be different for each operation.

These interfaces are defined across three NuGet packages, to avoid saddling applications with dependencies they don't want:

* `Corvus.Identity.MicrosoftRest` (all the `Microsoft.Rest.ITokenProvider` interfaces)
* `Corvus.Identity.Azure` (all the `Azure.Core.TokenCredential` interfaces, and also `IAccessTokenSourceFromDynamicConfiguration`)
* `Corvus.Identity.Abstractions` (`IAccessTokenSource`, `IServiceIdentityAccessTokenSource`)

These are not entirely independent: taking a dependency on either of the first two will result in an implicit dependency on the third. Less obviously, a dependency on the first also results in a dependency on the second. That reflects the way it has been implemented, and arises from the fact that in the long run we expect most applications to take a direct dependency on `Corvus.Identity.Azure` (entailing an implicit dependency on `Corvus.Identity.Abstractions`). Applications that need to use bits of Azure for which the SDK hasn't yet provided new `Azure.Core`-style client libraries (or which were written before that happened and are still using old libraries) will take a dependency on `Corvus.Identity.MicrosoftRest`, but we consider that to be a transitional state, and all the underpinnings are provided by the other two libraries. (We don't provide a 'native' implementation of any of the `Microsoft.Rest` types: they are all just wrappers over `Corvus.Identity.Azure`, because we consider `Microsoft.Rest` to be something applications "still support, but would like to get rid of" with a pure `Corvus.Identity.Azure`-based implementation being the desired target state.)

Note that neither `IAccessTokenSource` nor `IServiceIdentityAccessTokenSource` necessarily require the use of Azure AD, so these are defined in the `Corvus.Identity.Abstractions` library. However, the only implementations we supply are specific to Azure AD, and therefore live in `Corvus.Identity.Azure`. Also note that the third of the "Raw access tokens" interfaces in the table above, `IAccessTokenSourceFromDynamicConfiguration`, is defined in a different assembly: `Corvus.Identity.Azure`. That's because all of the `...FromDynamicConfiguration` interface define methods that take a `ClientIdentityConfiguration` argument, and that type _is_ tied inextricably to Azure—it depends not only on Azure AD concepts but also on Azure Key Vault.

## Implementation layering

We don't want to have 9 separate token source implementations. We have decided that the `Azure.Core` model (and specifically the `Azure.Identity` implementation of that model) is the heart of all of these mechanisms. (This is embodied in the package dependencies: if you want a `Corvus.Identity`-supplied implementation of any of these things, you will end up with a dependency on `Corvus.Identity.Azure`. And even if you're just writing libraries that consume these interfaces without imposing choices on particular implementations, anything that uses any of the `...FromDynamicConfiguration` forms is in effect imposing the use of Azure AD, and will be taking a dependency on `Corvus.Identity.Azure`.)

So the `Azure.Core.TokenCredential`-based implementation is where the real work happens, and all the other implementations we supply are adaptations of that. (So although `IAccessTokenSource` may look like the more fundamental, low-level implementation, in practice our implementation of that is actually a wrapper over our `IAzureTokenCredentialSource`.)

### The two 'real' implementations

All of the interfaces described above end up at `AzureTokenCredentialSource`, a type that wraps a `TokenCredential` in an implementation of `IAzureTokenCredentialSource`. This does almost nothing: it just hands out the `TokenCredential` it has been given. But all of the different ways of obtaining token will go through it.

Of slightly more interest is `AzureTokenCredentialSourceFromConfiguration` (`Corvus.Identity`'s only implementation of `IAzureTokenCredentialSourceFromDynamicConfiguration`) which hands out `AzureTokenCredentialSource` instances in exchange for `ClientIdentityConfiguration` instances. This is the part of the library that knows how to process an `ClientIdentityConfiguration`.

This diagram shows the simplest use case in which an application works directly with the `IAzureTokenCredentialSourceFromDynamicConfiguration` interface to obtain credentials:

![Diagram showing that an application using the Corvus.Identity.Azure NuGet package passes a ClientIdentityConfiguration to the AzureTokenCredentialSourceFromConfiguration implementation of the IAzureTokenCredentialSourceFromDynamicConfiguration, which creates a suitable TokenCredential as implemented by the Azure.Identity NuGet package, and then wraps this in an AzureTokenCredentialSource which it returns to the application as an IAzureTokenCredentialSource](token-source-layering-real-work.svg)

Since the `AzureTokenCredentialSource` is trivial, just handing back out the token it wraps and delegating the interesting work to `Azure.Identity`, the real contribution from `Corvus.Identity` is the `AzureTokenCredentialSourceFromConfiguration`. This knows how to resolve a `ClientIdentityConfiguration` to a `TokenCredential`. That can entail considerable slow work (e.g., looking up a secret or even multiple secrets in Azure Key Vault), and non-trivial behaviour such as caching.

### Other `TokenProvider`-oriented types

Although all code using `Corvus.Identity` ultimately obtains an `AzureTokenCredentialSource` from an `AzureTokenCredentialSourceFromConfiguration`, there are other types `Corvus.Identity` defines in this `Azure.Core` `TokenProvider`-oriented feature set. For example, it has two implementations of `IAzureTokenCredentialSource`. This is necessary to support service identity scenarios, because there are two significantly different ways the application can determine which credential represents the service:
 
 * applications can supply a `TokenCredential` directly during application startup
 * applications can supply a `ClientIdentityConfiguration`

When application code wants to authenticate using the service identity via an `Azure.Core` `TokenCredential`, it doesn't need to care which of these two mechanisms was in use. It will just take a dependency on the `IServiceIdentityAzureTokenCredentialSource` interface. This is implemented by `ServiceIdentityAzureTokenCredentialSource` which, as this diagram shows, is just a wrapper for an `IAzureTokenCredentialSource`.

![Diagram showing how the AzureTokenCredentialSourceFromConfiguration implementation of IAzureTokenCredentialSourceFromConfiguration wraps one of two implementations IAzureTokenCredentialSource: either AzureTokenCredentialSource or AzureTokenCredentialSourceForSpecificConfiguration](token-source-layering-other-tokenprovider.svg)

But as this shows, `ServiceIdentityAzureTokenCredentialSource` can end up wrapping either of two `IAzureTokenCredentialSource` implementations. Which one you get depends on how the service identity is configured. Take the case where the application just supplies its own `TokenCredential` using this sort of startup code:

```cs
services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(
    new DefaultAzureCredential());
```

In this case, the `TokenCredential` is supplied up front, so we just wrap it in an `AzureTokenCredentialSource` and pass that to the `ServiceIdentityAzureTokenCredentialSource`. But consider cases where the service identity is defined by configuration, e.g.:

```cs
ClientIdentityConfiguration serviceIdConfig = configuration
    .GetSection("ServiceIdentity")
    .Get<ClientIdentityConfiguration>();
services.AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration(
    serviceIdConfig);
```

We don't want to attempt to resolve the configuration to a specific token during service initialization for two reasons:

1. it's often not practical to invoke asynchronous operations during initialization
1. to support credential rotation, we might need to be able to recreate the token (e.g., because values in key vault may have changed), so even if we did resolve it during initialization, we need to be able to re-resolve it again later

For these cases, we have `AzureTokenCredentialsourceForSpecificConfiguration`, which holds onto the `ClientIdentityConfiguration`, deferring the resolution to a `TokenCredential` until such time as the credential is actually required. (At that point, it just defers to `IAzureTokenCredentialSourceFromDynamicConfiguration` to resolve the configuration to an `IAzureTokenCredential` source—as already stated, this is one of the few the types that do all the real work.)

### Raw access token layering

Applications that work directly with raw access tokens (e.g, because they're using `HttpClient`, or some client SDK that doesn't support either the `Azure.Core` or `Microsoft.Rest` mechanisms) are still ultimately dependent on the `Azure.Core`-based implementations. For example, an application using the `IAccessTokenSourceFromDynamicConfiguration` interface will get the `AccessTokenSourceFromDynamicConfiguration` implementation, and when an application calls its `AccessTokenSourceForConfigurationAsync` method, it gets back an `IAccessTokenSource` implemented by `AzureTokenCredentialAccessTokenSource`, but as this diagram shows, that's just a wrapper around the `AzureTokenCredentialSourceForSpecificConfiguration` we saw earlier.

![Diagram showing that the AccessTokenSourceFromDynamicConfiguration class (which implements IAccessTokenSourceFromDynamicConfiguration) hands out IAccessTokenSource instances implemented by AzureTokenCredentialAccessTokenSource, which is just a wrapper for AzureTokenCredentialSourceForSpecificConfiguration, and that in turn is a wrapper for AzureTokenCredentialSourceFromConfiguration, which, along with AzureTokenCredentialSource and the Azure.Identity NuGet package, does the real work.](token-source-accesstoken-dynamicconfig-layering.svg)

So the actual work of producing tokens continues to be done by the `TokenCredential` implementations supplied by `Azure.Identity`. And the types that resolve a `ClientIdentityConfiguration` to a particular `TokenCredential` are exactly the same in this raw access token case as they are when you work directly with `Corvus.Identity`'s `TokenCredential`-oriented interfaces.

It's a similar story when using service identities. As this diagram shows, the raw access token version of the service identity interface, `IServiceIdentityAccessTokenSource`, is just a wrapper around the `Azure.Core` `TokenCredential` flavoured service identity interface, `IServiceIdentityAzureTokenCredentialSource`:

![Diagram showing that the IServiceIdentityAccessTokenSource implementation, ServiceIdentityAccessTokenSource, derives from AzureTokenCredentialAccessTokenSource, and wraps around the ServiceIdentityAzureTokenCredentialTokenSource implementation, which, as in the earlier diagram, then layers over either an AzureTokenCredentialSource or an AzureTokenCredentialSourceForSpecificConfiguration](token-source-accesstoken-serviceid-layering.svg)

This diagram shows the `IServiceIdentityAzureTokenCredentialSource` being implemented by `ServiceIdentityAzureTokenCredentialSource`; in fact the raw access token wrapper doesn't care which implementation is used—it just acquires it through DI, so this means that the particular identity that `IServiceIdentityAccessTokenSource` returns is determined by `IServiceIdentityAzureTokenCredentialSource`—as ever, it all comes down to the `Azure.Core` `TokenCredential` implementations in the end.

A subtle implementation detail revealed by this diagram is that the raw token service identity implementation (`ServiceIdentityAccessTokenSource`) derives from the same `IAccessTokenSource` implementation (`AzureTokenCredentialSource`) shown in the previous diagram, as handed out by the `IAccessTokenSourceFromDynamicConfiguration` implementation. In fact, they are almost identical—the raw token service ID implementation just declares the implementation of this additional interface (which doesn't even declare any new members). Because the real work is being done at the `TokenCredential` layer (not shown in this last diagram to reduce clutter, but it underpins everything) the raw access token wrappers can get the right behaviour simply by choosing which particular token source to wrap.


### The `Microsoft.Rest.ITokenProvider` implementations sit on `IAccessTokenSource`, not `IAzureTokenCredentialSource`

Although the `Azure.Core.TokenCredential` implementations are the only ones that do real work in `Corvus.Identity` (ignoring the legacy `Corvus.Identity.ManagedServiceIdentity.ClientAuthentication` component) the `Microsoft.Rest`-flavoured ones don't wrap it directly. Instead, they wrap `IAccessTokenSource`, `IAccessTokenSourceFromDynamicConfiguration`, and `IServiceIdentityAccessTokenSource` (and our implementations of those then defer to the `TokenCredential`-based implementations). This diagram shows how this all fits together when using `IMicrosoftRestTokenProviderSourceFromDynamicConfiguration`:

![Diagram showing how the `IMicrosoftRestTokenProviderSourceFromDynamicConfiguration` implementation, `MicrosoftRestTokenProviderSourceFromDynamicConfiguration` provides `MicrosofRestTokenProvider` instances implementing `IMicrosofRestTokenProvider`, and these respectively wrap their raw access token counterparts, `IAccessTokenSourceFromDynamicConfiguration` and `IAccessTokenSource`. The diagram then shows how these are in turn layered on top of the `Azure.Core` `TokenCredential` implementations; this part of the diagram is the same as the earlier diagram showing the corresponding raw access token implementation layering.](token-source-microsoft-rest-dynamicconfig-layering.svg)

Note that the `ITokenProvider` (implemented by our `MicrosoftRestTokenProvider` class) is defined by `Microsoft.Rest`—this is the thing that's actually required by applications using `Microsoft.Rest`—style client SDKs.

This next diagram shows that service identity works the same way. (This picture omits the underlying `Azure.Identity` implementation, to avoid making the diagram too cluttered, but it will be there underneath it all.)

![Diagram showing that the `IServiceIdentityMicrosoftRestTokenProviderSource` implementation, `ServiceIdentityMicrosoftRestTokenProviderSource`, is just a layer over the `IServiceIdentityAccessTokenSource` implementation.](token-source-microsoft-rest-service-identity-layering.svg)

Layering all of these `Microsoft.Rest`—style classes over the raw access token ones might seem unnecessarily inefficient, given that the raw access token implementations are in turn a layer over `Azure.Identity`. Why not just have our `Microsoft.Rest` implementations defer directly to `IAzureTokenCredentialSource`, `IAzureTokenCredentialSourceFromDynamicConfiguration`, and `IServiceIdentityAzureTokenCredentialSource` just like the raw access token implementations do? (So the `Microsoft.Rest`-flavoured versions could essentially mirror the structure of the raw access token ones, instead of being a layer on top of them.) But fundamentally, what the `Microsoft.Rest.ITokenProvider`-based implementations need to be able to do is get hold of access tokens. They don't care about `Azure.Core.TokenCredentials`—those are just a means to an end. The `Microsoft.Rest`-flavoured wrappers would need to do the work to ask a `TokenCredential` for an access token, and we already have classes that do that: the implementations of `IAccessToken` etc.

So there are two benefits to this deeper layering:

1. we avoid duplicating the code that extracts raw tokens from a `TokenCredential`
2. we make it possible for applications to plug in alternative access token sources via DI

So although the only implementations of `IAccessTokenSource` and friends that `Corvus.Identity` provide are wrappers around `IAzureTokenCredentialSource`, nothing stops someone else writing their own implementations, and getting our `Microsoft.Rest`-flavoured wrappers to use those instead.

### Layering of `IServiceIdentityXxx` implementations

In all three of the credential forms in the table above, there is an an interface for obtaining a credential representing the service identity (e.g., an Azure Managed Identity). In all cases, this interface derives from the more general-purpose interface listed in the *Source decides* column in the table above. So `IServiceIdentityAzureTokenCredentialSource` is an `IAzureTokenCredentialSource`, `IServiceIdentityAccessTokenSource` is an `IAccessTokenSource`, and `IServiceIdentityMicrosoftRestTokenProviderSource` is an `IMicrosoftRestTokenProviderSource`.

Given this, you might expect all three source types to implement the service identity interfaces in the same way. However, they don't, and this section explains why.
  
`ServiceIdentityAzureTokenCredentialSource` implements `IServiceIdentityAzureTokenCredentialSource` by delegating to an `IAzureTokenCredentialSource`. Delegation is the right approach here because there are multiple `IAzureTokenCredentialSource` credential source implementations. If the application startup code calls `AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential`, it is telling us to use a specific `TokenCredential` as the service identity, so the `ServiceIdentityAzureTokenCredentialSource` ends up delegating to the simple `AzureTokenCredentialSource`. But if the application calls `AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration`, instead we get a `ServiceIdentityAzureTokenCredentialSource` delegating to an `AzureTokenCredentialSourceForSpecificConfiguration`.

Things are slightly different when we come to `IAccessTokenSource` and friends. Remember, our only implementation of these is a wrapper on top of the `Azure.Core` `TokenProvider`-based implementations. So the core of this is `AzureTokenCredentialAccessTokenSource`, which implements `IAccessTokenSource` as a wrapper around any `IAzureTokenCredentialSource`. So at this level, we don't need two styles of implementation. We have `AzureTokenCredentialAccessTokenSource`, the only direct implementation of `IAccessTokenSource`, and this is a wrapper around an `IAzureTokenCredentialSource`. (The credential source strategy, then, is determined by the particular `IAzureTokenCredentialSource` that this supplied.) And then for the service identity version, we just derive a type from that, `ServiceIdentityAccessTokenSource`, which implements `IServiceIdentityAccessTokenSource`. Since `IServiceIdentityAccessTokenSource` doesn't define any additional members—it inherits `IAccessTokenSource`, the distinction being that this derived interface specifically represents the service identity—this `ServiceIdentityAccessTokenSource` is very simple. Its only member is a constructor calling the base class. This constructor depends on `IServiceIdentityAzureTokenCredentialSource`, meaning that DI will supply the token credential source that is specifically for the service identity, and that's the job done.

The `Microsoft.Rest` version uses inheritance instead of delegation for exactly the same reason.