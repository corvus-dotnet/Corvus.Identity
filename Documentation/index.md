# Corvus.Identity

When writing code that uses a service, we very often need to authenticate. With web APIs, this is often achieved by supplying an access token as the `Authorization` header of an HTTP request. `Corvus.Identity` supports this in various ways. 

* provides abstractions for obtaining access tokens in various forms:
    * plain text (for direct use in an `Authorization` header)
    * [`TokenCredential`](https://docs.microsoft.com/en-us/dotnet/api/azure.core.tokencredential) (defined in [Azure.Core](https://www.nuget.org/packages/Azure.Core/), and used in modern Azure SDK client libraries)
    * [`ITokenProvider`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.rest.itokenprovider) (defined in [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime), and used in older Azure SDK client libraries, and many [Autorest](https://github.com/Azure/AutoRest)-generated clients)
* provides a migration path from the older `Microsoft.Rest` system to the new `Azure.Core`/`Azure.Identity` model
* provides a way for components to obtain token sources representing the service or application identity

That last point—the idea of a _service identity_—is the most common reason for using `Corvus.Identity`. When writing a service, that service will often need to communicate with other services, and those other services will typically require some form of authentication. For example, a storage service might be configured to allow only a handful of identities to read and write data, so if we're writing an application service that will use that storage service, we will need to be able to present some form of authentication to convinced the storage that the requests we are making are coming from some component authorized to make such requests.

For example, Azure offers ["managed identities"](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) as a solution to exactly this problem: certain kinds of Azure resources (e.g., App Services, VMs) can have an associated Service Principal in Azure AD, for which Azure automatically manages the credentials.

When writing reusable components, we might not want to depend directly on some platform-specific service such as Azure Managed Identities. Other cloud platforms exist. And even within Azure, there are sometimes reasons to create and manage Service Principals yourself. And even if you decide that an Azure Managed Identity is the right solution for you, there are a few different ways to use the facility: Microsoft has released at least two different libraries for it, and it's also possible to write your own code that just communicates directly with the relevant endpoint.

`Corvus.Identity` enables libraries in the Corvus ecosystem (including its extended family, Menes and Marain) to make use of a service identity without being tied to any particular cloud platform, or any particular libraries outside of Corvus. For example, you can write this sort of code:

```cs
public class UseAzureKeyVaultWithNewSdk
{
    private readonly IServiceIdentityAzureTokenCredentialSource tokenCredentialSource;

    public UseAzureKeyVaultWithNewSdk(IServiceIdentityAzureTokenCredentialSource tokenCredentialSource)
    {
        this.tokenCredentialSource = tokenCredentialSource;
    }

    public async Task<string> GetSecretAsync(Uri keyVaultUri, string secretName)
    {
        TokenCredential credential = await this.tokenCredentialSource.GetAccessTokenAsync().ConfigureAwait(false);
        var client = new SecretClient(keyVaultUri, credential);

        Response<KeyVaultSecret> secret = await client.GetSecretAsync(secretName).ConfigureAwait(false);

        return secret.Value.Value;
    }
}
```

This code requires an implementation of `IServiceIdentityAzureTokenCredentialSource`. This indicates that it wants to authenticate as the running service's identity, and that it wants to use `TokenCredential` objects (as defined by `Azure.Core`) to do this. This is typically supplied via dependency injection, so you would have code such as this in the application startup:

```cs
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    config.Get<LegacyAzureServiceTokenProviderOptions>());
```

Note that this particular method refers to a "legacy connection string". That's because the `Corvus.Identity.Azure` library provides support for continuing to use this form of configuration even though Microsoft's own libraries dropped support for it in `Azure.Identity`. The code here presumes `config` is an `IConfiguration` object, and the `LegacyAzureServiceTokenProviderOptions` being used for configuration binding here expects the connection string to be in a setting called `AzureServicesAuthConnectionString`. This supports most of the same formats as `AzureServiceTokenProvider`. E.g., you can set this to `RunAs=App` to indicate that you want to use the ambient Managed Identity. (That only works when the code is running in Azure in an environment that supplies a Managed Identity.)

These old-style connection strings are useful because they provide a level of runtime configurability that `Azure.Identity` doesn't support out of the box. However, if you don't want to use them you can just provide a specific `TokenCredential` implementation, e.g.:

```cs
services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(
    new DefaultAzureCredential());
```

But the key point here is that the `UseAzureKeyVaultWithNewSdk` class doesn't need to know exactly which type of `TokenCredential` it's getting, or how it is created. It is just declaring that it needs to be able to get `TokenCredential`s that represent the service identity, which it can then pass into client APIs that use this kind of credential, such as the Azure Key Vault `SecretClient` shown in the example.


## Components

`Corvus.Identity` is a set of libraries, each of which is described in the following sections.

### `Corvus.Identity.Abstractions`

This defines abstractions that are not specific to any particular technology or authentication mechanism. It defines two interfaces:

| Interface | Purpose |
| --- | --- |
| `IAccessTokenSource` | Provides the ability to obtain an access token |
| `IServiceIdentityAccessTokenSource` | A specialized `IAccessTokenSource` providing tokens that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and which wants to work directly with the raw access token, take a dependency on `IServiceIdentityAccessTokenSource`. (Note that any application using such a component will need to ensure that an implementation of this interface is available. This will typically be supplied by the `Corvus.Identity.Azure` library.)


### Corvus.Identity.Azure

This provides an implementation of the `IServiceIdentityAccessTokenSource` defined by `Corvus.Identity.Abstractions` based on `Azure.Core`/`Azure.Identity`, and also two more specialized abstractions (and implementations of these) intended for use by components that work with `Azure.Core`-based libraries, such as "new"-style Azure SDK client libraries.

| Interface | Purpose |
| --- | --- |
| `IAzureTokenCredentialSource` | Provides the ability to obtain an `Azure.Core` style `TokenCredential` |
| `IServiceIdentityAzureTokenCredentialSource` | A specialized `IAzureTokenCredentialSource` providing tokens that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and to do so through the `TokenCredential` (e.g., to use a new-style Azure SDK client library), take a dependency on `IServiceIdentityAzureTokenCredentialSource`.

TODO: DI init.



### Corvus.Identity.MicrosoftRest

This defines two specialized abstractions intended for use by components that work with `Microsoft.Rest`-based libraries, such as "old"-style Azure SDK client libraries, or Autorest-based clients, and it provides an implementations that is an adapter on top of `IServiceIdentityAccessTokenSource`.

| Interface | Purpose |
| --- | --- |
| `IMicrosoftRestTokenProviderSource` | Provides the ability to obtain a `Microsoft.Rest` style `ITokenProvider` |
| `IServiceIdentityMicrosoftRestTokenProviderSource` | A specialized `IMicrosoftRestTokenProviderSource` suppling token providers that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and to do so through the `Microsoft.Rest` family of libraries' `ITokenProvider` (e.g., to use an old-style Azure SDK client library), take a dependency on `IServiceIdentityMicrosoftRestTokenProviderSource`.

TODO: DI init.


### Corvus.Identity.ManagedServiceIdentity.ClientAuthentication