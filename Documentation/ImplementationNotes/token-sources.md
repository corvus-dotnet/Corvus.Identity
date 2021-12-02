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

**Note**: the reason the interfaces in the third column refer to "dynamic configuration" is that if we just say configuration, methods that added implementations to DI would have names such as `AddAzureTokenCredentialSourceFromConfiguration`, which would sound like it would be reading settings from application configuration. The name `AddAzureTokenCredentialSourceFromDynamicConfiguration` does a slightly better job of conveying the fact that this adds an implementation which expects to be passed configuration that might be different for each operation.

These interfaces are defined across three NuGet packages, to avoid saddling applications with dependencies they don't want:

* `Corvus.Identity.MicrosoftRest` (all the `Microsoft.Rest.ITokenProvider` interfaces)
* `Corvus.Identity.Azure` (all the `Azure.Core.TokenCredential` interfaces, and also `IAccessTokenSourceFromDynamicConfiguration`)
* `Corvus.Identity.Abstractions` (`IAccessTokenSource`, `IServiceIdentityAccessTokenSource`)

These are not entirely independent though: taking a dependency on either of the first two will result in an implicit dependency on the third. Moreover, in the long run we expect most applications to take a direct dependency on `Corvus.Identity.Azure` (entailing an implicit dependency on `Corvus.Identity.Abstractions`). Applications that need to use bits of Azure for which the SDK hasn't yet provided new `Azure.Core`-style client libraries (or which were written before that happened and are still using old libraries) will take a dependency on `Corvus.Identity.MicrosoftRest`, but we consider that to be a transitional state, and all the underpinnings are provided by the other two libraries. (We don't provide a 'native' implementation of any of the `Microsoft.Rest` types: they are all just wrappers over `Corvus.Identity.Azure`, because we consider `Microsoft.Rest` to be something applications "still support, but would like to get rid of" with a pure `Corvus.Identity.Azure`-based implementation being the desired target state.)

Note that neither `IAccessTokenSource` nor `IServiceIdentityAccessTokenSource` necessarily require the use of Azure AD, so these are defined in the `Corvus.Identity.Abstractions` library. However, the only implementations we supply are specific to Azure AD, and therefore live in `Corvus.Identity.Azure`. Also note that the third of the "Raw access tokens" interfaces in the table above, `IAccessTokenSourceFromDynamicConfiguration`, is defined in a different assembly: `Corvus.Identity.Azure`. That's because all of the `...FromDynamicConfiguration` interface define methods that take a `ClientIdentityConfiguration` argument, and that type _is_ tied inextricably to Azure—it depends not only on Azure AD concepts but also on Azure Key Vault.

## Implementation layering

We don't want to write 9 separate token source implementations. We have decided that the `Azure.Core` model (and specifically the `Azure.Identity` implementation of that model) is the heart of all of these mechanisms. (This is embodied in the package dependencies: if you want a `Corvus.Identity`-supplied implementation of any of these things, you will end up with a dependency on `Corvus.Identity.Azure`. And even if you're just writing libraries that consume these interfaces without imposing choices on particular implementations, anything that uses any of the `...FromDynamicConfiguration` forms is in effect imposing the use of Azure AD, and will be taking a dependency on `Corvus.Identity.Azure`.)

So the `Azure.Core.TokenCredential`-based implementation is where the real work happens, and all the other implementations we supply are adaptations of that. (So although `IAccessTokenSource` may look like the more fundamental, low-level implementation, in practice our implementation of that is actually a wrapper over our `IAzureTokenCredentialSource`.)

### The two 'real' implementations

All of the interfaces described above end up at `AzureTokenCredentialSource`, a type that wraps a `TokenCredential` in an implementation of `IAzureTokenCredentialSource`. This does almost nothing: it just hands out the `TokenCredential` it has been given. But all of the different ways of obtaining token will go through it.

Of slightly more interest is `AzureTokenCredentialSourceFromConfiguration` (`Corvus.Identity`'s only implementation of `IAzureTokenCredentialSourceFromDynamicConfiguration`) which hands out `AzureTokenCredentialSource` instances in exchange for `ClientIdentityConfiguration` instances. This is the part of the library that knows how to process an `ClientIdentityConfiguration`.

This diagram shows the simplest use case in which an application works directly with the `IAzureTokenCredentialSourceFromDynamicConfiguration` interface to obtain credentials:

![Diagram showing that an application using the Corvus.Identity.Azure NuGet package passes a ClientIdentityConfiguration to the AzureTokenCredentialSourceFromConfiguration implementation of the IAzureTokenCredentialSourceFromDynamicConfiguration, which creates a suitable TokenCredential as implemented by the Azure.Identity NuGet package, and then wraps this in an AzureTokenCredentialSource which it returns to the application as an IAzureTokenCredentialSource](token-source-layering-real-work.svg)

### The `Microsoft.Rest.ITokenProvider` implementations sit on `IAccessTokenSource`, not `IAzureTokenCredentialSource`

Although the `Azure.Core.TokenCredential` implementations are the only ones that do real work in `Corvus.Identity` (ignoring the legacy `Corvus.Identity.ManagedServiceIdentity.ClientAuthentication` component) the `Microsoft.Rest`-flavoured ones don't wrap it directly. Instead, they wrap `IAccessTokenSource`, `IAccessTokenSourceFromDynamicConfiguration`, and `IServiceIdentityAccessTokenSource` (and our implementations of those then defer to the `TokenCredential`-based implementations).

This might seem unnecessarily inefficient Why not just defer directly to `IAzureTokenCredentialSource`, `IAzureTokenCredentialSourceFromDynamicConfiguration`, and `IServiceIdentityAzureTokenCredentialSource`? Fundamentally, what the `Microsoft.Rest.ITokenProvider`-based implementations need to be able to do is get hold of access tokens. They don't care about `Azure.Core.TokenCredentials`—those are just a means to an end. The `Microsoft.Rest`-flavoured wrappers would need to do the work to ask a `TokenCredential` for an access token, and we already have classes that do that: the implementations of `IAccessToken` etc.

So there are two benefits:

1. we avoid duplicating the code that extracts tokens from a `TokenCredential`
2. we make it possible for applications to plug in alternative access token sources via DI

So although the only implementations of `IAccessTokenSource` and friends that `Corvus.Identity` provides is a wrapper around `IAzureTokenCredentialSource`, nothing stops someone else writing their own implementations, and getting our `Microsoft.Rest`-flavoured wrappers to use those instead.

### Layering of `IServiceIdentityXxx` implementations

In all three of the credential forms in the table above, there is an an interface for obtaining a credential representing the service identity (e.g., an Azure Managed Identity). In all cases, this interface derives from the more general-purpose interface listed in the *Source decides* column in the table above. So `IServiceIdentityAzureTokenCredentialSource` is an `IAzureTokenCredentialSource`, `IServiceIdentityAccessTokenSource` is an `IAccessTokenSource`, and `IServiceIdentityMicrosoftRestTokenProviderSource` is an `IMicrosoftRestTokenProviderSource`.

Given this, you might expect all three source types to implement the service identity interfaces in the same way. However, they don't, and this section explains why.

 The `Azure.Core.TokenCredential` implementation (which, remember, is the only one that really knows how to get credentials) actually has two implementations of `IAzureTokenCredentialSource`, because there are two significantly different ways the application can determine which credential gets used:
 
 * applications can supply a `TokenCredential` directly
 * applications can supply a `ClientIdentityConfiguration`

 The first of these is trivial: we just need to hand back out the token we were given. The second can entail considerable slow work (e.g., looking up a secret or even multiple secrets in Azure Key Vault), and non-trivial behaviour such as caching. While it would be possible to roll these two modes into a single class, with `if` statements to select the strategy, it seems simpler to define two classes. (`AzureTokenCredentialSource` is the trivial one. `AzureTokenCredentialSourceForSpecificConfiguration` is the one that knows how to build a `TokenCredential` from a `ClientIdentityConfiguration`.)
 
When it comes to the service identity `ServiceIdentityAzureTokenCredentialSource` implements `IServiceIdentityAzureTokenCredentialSource` by delegating to an `IAzureTokenCredentialSource`. Delegation is the right approach here because there are multiple `IAzureTokenCredentialSource` credential source implementations. If the application startup code calls `AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential`, it is telling us to use a specific `TokenCredential` as the service identity, so the `ServiceIdentityAzureTokenCredentialSource` ends up delegating to the simple `AzureTokenCredentialSource`. But if the application calls `AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration`, instead we get a `ServiceIdentityAzureTokenCredentialSource` delegating to an `AzureTokenCredentialSourceForSpecificConfiguration`.

Things are slightly different when we come to `IAccessTokenSource` and friends. Remember, our only implementation of these is a wrapper on top of the `Azure.Core.TokenProvider`-based implementations. So the core of this is `AzureTokenCredentialAccessTokenSource`, which implements `IAccessTokenSource` as a wrapper around any `IAzureTokenCredentialSource`. So at this level, we don't need two styles of implementation. We have `AzureTokenCredentialAccessTokenSource`, the only direct implementation of `IAccessTokenSource`, and this is a wrapper around an `IAzureTokenCredentialSource`. (The credential source strategy, then, is determined by the particular `IAzureTokenCredentialSource` that this supplied.) And then for the service identity version, we just derive a type from that, `ServiceIdentityAccessTokenSource`, which implements `IServiceIdentityAccessTokenSource`. Since `IServiceIdentityAccessTokenSource` doesn't define any additional members—it inherits `IAccessTokenSource`, the distinction being that this derived interface specifically represents the service identity—this `ServiceIdentityAccessTokenSource` is very simple. Its only member is a constructor calling the base class. This constructor depends on `IServiceIdentityAzureTokenCredentialSource`, meaning that DI will supply the token credential source that is specifically for the service identity, and that's the job done.

The `Microsoft.Rest` version uses inheritance instead of delegation for exactly the same reason.