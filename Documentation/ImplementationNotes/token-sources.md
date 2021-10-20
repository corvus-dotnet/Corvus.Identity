# Token source implementation notes

This describes the implementation approach taken for the various credential source implementations.

`Corvus.Identity` can supply applications with credentials in three forms:

* The `Azure.Core`-style `TokenCredential`
* Plain access tokens for use directly with HTTP `Authentication` headers
* The old `Microsoft.Rest`-style `ITokenProvider`

For each of these, there are three ways in which application code can determine which principal the credentials should represent:

* Non-committal—the caller decides, and passes some object encapsulating its choice
* Service identity (ambient process-wide identity; often, but not always, an Azure Managed Identity)
* Specified in a `ClientIdentityConfiguration`

`Corvus.Identity` defines interfaces that represent the combinations of these options. This table has a row for each form of credentials, and columns for each of the three ways in which application code might determine the principal to use, and shows which interface to use in each case:

|                                 | Caller decides                      | Service Identity                                   | `ClientIdentityConfiguration`                               |
| --- | --- | --- | --- |
| `Azure.Core.TokenCredential`    | `IAzureTokenCredentialSource`       | `IServiceIdentityAzureTokenCredentialSource`       | `IAzureTokenCredentialSourceFromDynamicConfiguration`       |
| Raw access tokens               | `IAccessTokenSource`                | `IServiceIdentityAccessTokenSource`                | `IAccessTokenSourceFromDynamicConfiguration`                |
| `Microsoft.Rest.ITokenProvider` | `IMicrosoftRestTokenProviderSource` | `IServiceIdentityMicrosoftRestTokenProviderSource` | `IMicrosoftRestTokenProviderSourceFromDynamicConfiguration` |

These interfaces are defined across three NuGet packages, to avoid saddling applications with dependencies they don't want:

* `Corvus.Identity.MicrosoftRest` (all the `Microsoft.Rest.ITokenProvider` interfaces)
  * `Corvus.Identity.Azure` (all the `Azure.Core.TokenCredential` interfaces, and also `IAccessTokenSourceFromDynamicConfiguration`)
    * `Corvus.Identity.Abstractions` (`IAccessTokenSource`, `IServiceIdentityAccessTokenSource`)

In the long run, we expect most applications to take a direct dependency on `Corvus.Identity.Azure`, which entails an implicit dependency on `Corvus.Identity.Abstractions`. Applications that need to use bits of Azure for which the SDK hasn't yet provided new `Azure.Core`-style client libraries (or which were written before that happened and are still using old libraries) will take a dependency on `Corvus.Identity.MicrosoftRest`, but we consider that to be a transitional state, and all the underpinnings are provided by `Corvus.Identity.Abstractions`. (We don't provide a 'native' implementation of any of the `Microsoft.Rest` types: they are all just wrappers over `Corvus.Identity.Azure`, because we consider `Microsoft.Rest` to be something applications "still support, but would like to get rid of" with a pure `Corvus.Identity.Azure`-based implementation being the desired target state.)

Note that neither `IAccessTokenSource` nor `IServiceIdentityAccessTokenSource` necessarily require the use of Azure AD, so these are defined in the `Corvus.Identity.Abstractions` library. However, the only implementations we supply are specific to Azure AD, and therefore live in `Corvus.Identity.Azure`. Also note that the third of the "Raw access tokens" interfaces in the table above, `IAccessTokenSourceFromDynamicConfiguration`, is defined in a different assembly: `Corvus.Identity.Azure`. That's because all of the `...FromDynamicConfiguration` interface define methods that take a `ClientIdentityConfiguration` argument, and that type _is_ tied inextricably to Azure—it depends not only on Azure AD concepts but also on Azure Key Vault.

## Implementation layering

We don't want to write 9 separate token source implementations. We have decided that the `Azure.Core` model (and specifically the `Azure.Identity` implementation of that model) is the heart of all of these mechanisms. (This is embodied in the package dependencies: if you want a `Corvus.Identity`-supplied implementation of any of these things, you will end up with a dependency on `Corvus.Identity.Azure`. And even if you're just writing libraries that consume these interfaces without imposing choices on particular implementations, anything that uses any of the `...FromDynamicConfiguration` forms is in effect imposing the use of Azure AD, and will be taking a dependency on `Corvus.Identity.Azure`.)

So the `Azure.Core.TokenCredential`-based implementation is where the real work happens, and all the other implementations we supply are adaptations of that. (So although `IAccessTokenSource` may look like the more fundamental, low-level implementation, in practice our implementation of that is actually a wrapper over our `IAzureTokenCredentialSource`.)

### The two 'real' implementations

All of the interfaces described above end up at `AzureTokenCredentialSource`, a type that wraps a `TokenCredential` in an implementation of `IAzureTokenCredentialSource`. This does almost nothing: it just hands out the `TokenCredential` it has been given. But all of the different ways of obtaining token will go through it.

Of slightly more interest is `AzureTokenCredentialSourceFromConfiguration` (`Corvus.Identity`'s only implementation of `IAzureTokenCredentialSourceFromDynamicConfiguration`) which hands out `AzureTokenCredentialSource` instances in exchange for `ClientIdentityConfiguration` instances. This is the part of the library that knows how to process an `ClientIdentityConfiguration`.

### Layering of `IServiceIdentityXxx` implementations

