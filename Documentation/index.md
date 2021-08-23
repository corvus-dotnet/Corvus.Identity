# Corvus.Identity

`Corvus.Identity` is a set of NuGet packages that support services that need to authenticate when calling out to other services. For instructions on how to get up and running quickly, see the [Getting started](articles/getting-started.md) page.

When writing code that uses a service, we very often need to authenticate. This is often achieved by supplying an access token as the `Authorization` header of an HTTP request. `Corvus.Identity` supports this in various ways by providing:

* abstractions for obtaining access tokens in various forms:
    * plain text (for direct use in an `Authorization` header)
    * [`TokenCredential`](https://docs.microsoft.com/en-us/dotnet/api/azure.core.tokencredential) (defined in [Azure.Core](https://www.nuget.org/packages/Azure.Core/), and used in modern Azure SDK client libraries)
    * [`ITokenProvider`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.rest.itokenprovider) (defined in [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime), and used in older Azure SDK client libraries, and many [Autorest](https://github.com/Azure/AutoRest)-generated clients)
* a way for components to obtain token sources representing the service or application identity
* a migration path from the older `Microsoft.Rest` system to the new `Azure.Core`/`Azure.Identity` model

The second point—the idea of a _service identity_—is the most common reason for using `Corvus.Identity`. For example, if our code needs to use a storage service, that service might be configured to allow only a handful of identities to read and write data. If we're writing an application service that will use that storage service, we will need to be able to present some form of authentication to convinced the storage that the requests we are making are coming from some component authorized to make such requests.

Azure offers ["managed identities"](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) as a solution to exactly this problem: certain kinds of Azure resources (e.g., App Services, VMs) can have an associated Service Principal in Azure AD, for which Azure automatically manages the credentials. However, this leaves a few gaps to be filled in.

When writing reusable components, we might not want to depend directly on some platform-specific service such as Azure Managed Identities. Other cloud platforms exist. And even within Azure, there are sometimes reasons to create and manage Service Principals yourself. And even if you decide that an Azure Managed Identity is the right solution for you, there are a few different ways to use the facility: Microsoft has released at least two different libraries for it, and it's also possible to write your own code that just communicates directly with the relevant endpoint.

`Corvus.Identity` enables libraries in the [Corvus](https://github.com/corvus-dotnet) ecosystem (including its extended family, [Menes](https://github.com/menes-dotnet) and [Marain](https://github.com/marain-dotnet)) to make use of a service identity without being tied to any particular cloud platform, or any particular libraries outside of Corvus. For example, you can write this sort of code:

```cs
public class UseAzureKeyVaultWithNewSdk
{
    private readonly IServiceIdentityAzureTokenCredentialSource tokenCredentialSource;

    public UseAzureKeyVaultWithNewSdk(IServiceIdentityAzureTokenCredentialSource tokenCredentialSource)
    {
        this.tokenCredentialSource = tokenCredentialSource
            ?? throw new ArgumentNullException(nameof(tokenCredentialSource));
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

This code requires an implementation of `Corvus.Identity`'s `IServiceIdentityAzureTokenCredentialSource`. That indicates that it wants to authenticate as the running service's identity, and that it wants to use `TokenCredential` objects (as defined by `Azure.Core`) to do this. This is typically supplied via dependency injection, so you would have code such as this in the application startup:

```cs
// Enable use of AzureServiceTokenProvider-style connection strings in the
// AzureServicesAuthConnectionString configuration setting.
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    config.Get<LegacyAzureServiceTokenProviderOptions>());
```

Note that this particular example refers to a "legacy connection string". That's because the `Corvus.Identity.Azure` library provides support for continuing to use and older form of configuration that Microsoft's own libraries dropped support for it in `Azure.Identity`. The code here presumes `config` is an `IConfiguration` object, and the `LegacyAzureServiceTokenProviderOptions` being used for configuration binding here expects the connection string to be in a setting called `AzureServicesAuthConnectionString`. This supports most of the same formats as `AzureServiceTokenProvider`. E.g., you can set this to `RunAs=App` to indicate that you want to use the ambient Managed Identity. (That only works when the code is running in Azure in an environment that supplies a Managed Identity.) Or you can use a connection string of the form `RunAs=App;AppId=<appId>;TenantId=<tenantId>;AppKey=<clientSecret>` when debugging on your local machine, to provide credentials for a specific service principal.

These old-style connection strings are useful because they provide a level of runtime configurability that `Azure.Identity` doesn't support out of the box. However, if you don't want to use them you can just provide a specific `TokenCredential` implementation. E.g., if you want the same behaviour you would have got with the old `AzureServiceTokenProvider` by default (if you leave `AzureServicesAuthConnectionString` blank), in which it will detect automatically when an Azure Managed Identity is available, and if not, fall back to local providers such as the Azure CLI, or Visual Studio) you can write this in your startup:

```cs
// Use Azure Managed Identity where available, and if not, attempt to acquire
// credentials via Visual Studio, Azure CLI, or Azure PowerShell.
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
| [`IAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IAccessTokenSource) | Provides the ability to obtain an access token |
| `IServiceIdentityAccessTokenSource` | A specialized `IAccessTokenSource` providing tokens that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and which wants to work directly with the raw access token, take a dependency on `IServiceIdentityAccessTokenSource`. (Note that any application using such a component will need to ensure that an implementation of this interface is available,typically be by using the `Corvus.Identity.Azure` library.)

### Corvus.Identity.Azure

This provides an implementation of the `IServiceIdentityAccessTokenSource` defined by `Corvus.Identity.Abstractions` based on `Azure.Core`/`Azure.Identity`, and also two more specialized abstractions (and corresponding implementations) intended for use by components that work with `Azure.Core`-based libraries, such as "new"-style Azure SDK client libraries.

| Interface | Purpose |
| --- | --- |
| `IAzureTokenCredentialSource` | Provides the ability to obtain an `Azure.Core` style `TokenCredential` |
| `IServiceIdentityAzureTokenCredentialSource` | A specialized `IAzureTokenCredentialSource` providing tokens that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and to do so through the `TokenCredential` (e.g., to use a new-style Azure SDK client library), take a dependency on `IServiceIdentityAzureTokenCredentialSource`. (The `UseAzureKeyVaultWithNewSdk` shown in the example earlier does this.)

This library also defines extension methods for `IServiceCollection`. All of these have the same effect: they make implementations of `IServiceIdentityAccessTokenSource` and `IServiceIdentityAzureTokenCredentialSource` available via DI. This table describes which to use

| Method | Purpose |
| --- | --- |
| `AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString` | Uses `AzureServiceTokenProvider`-style connection strings to determine which kind of `TokenCredential` to use |
| `AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential` | Uses the `TokenCredential` you provide |



### Corvus.Identity.MicrosoftRest

This defines two specialized abstractions intended for use by components that work with `Microsoft.Rest`-based libraries, such as "old"-style Azure SDK client libraries, or Autorest-based clients, and it provides an implementations that is an adapter on top of `IServiceIdentityAccessTokenSource`.

| Interface | Purpose |
| --- | --- |
| `IMicrosoftRestTokenProviderSource` | Provides the ability to obtain a `Microsoft.Rest` style `ITokenProvider` |
| `IServiceIdentityMicrosoftRestTokenProviderSource` | A specialized `IMicrosoftRestTokenProviderSource` suppling token providers that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and to do so through the `Microsoft.Rest` family of libraries' `ITokenProvider` (e.g., to use an old-style Azure SDK client library), take a dependency on `IServiceIdentityMicrosoftRestTokenProviderSource`.

This library also provides a single extension method for `IServiceCollection`, making an implementation of `IServiceIdentityMicrosoftRestTokenProviderSource` available through DI: `AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource`. Note that this requires an implementation of `IServiceIdentityAccessTokenSource` to provide the actual tokens, because this library only provides adapters from that general-purpose form to the more specialized form required by `Microsoft.Rest`-based code. So you would typically have a pair of calls in your DI startup:

```cs
// Enable use of AzureServiceTokenProvider-style connection strings in the
// AzureServicesAuthConnectionString configuration setting.
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    config.Get<LegacyAzureServiceTokenProviderOptions>());
// Provide tokens in a form that old Microsoft.Rest-style libraries can use.
services.AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource();
```

With this in your startup, you will be able to use both old- and new-style Azure client libraries. You can take a dependency on `IServiceIdentityMicrosoftRestTokenProviderSource` to obtain tokens for old-style libraries, and on `IServiceIdentityAzureTokenCredentialSource` to obtain tokens for new-style libraries. The same underlying `TokenCredential` will be used in each case.

### Corvus.Identity.ManagedServiceIdentity.ClientAuthentication

This library is the oldest in `Corvus.Identity`, and it dates back to before the new-style Azure SDK. You should not use this in new code.

The one benefit this library offers is that if you are working purely in a `Microsoft.Rest` world, you can use this without taking any dependency on `Azure.Core` or `Azure.Identity`. (If you use the startup code shown in the preceding section for the `Corvus.Identity.MicrosoftRest` library, you will get access to credentials in a form suitable for old-style SDKs, but they will be implemented as wrappers on top of the newer mechanisms, meaning you will end up with dependencies on `Azure.Core` and `Azure.Identity` even if you're not using any new-style SDK libraries.) This library depends only on `Microsoft.Azure.Services.AppAuthentication` (the older wrapper for Azure Managed Identities), `Microsoft.Rest.ClientRuntime`, and `Microsoft.Extensions.DependencyInjection.Abstractions`.

This library defines a single interface:

| Interface | Purpose |
| --- | --- |
| `IServiceIdentityTokenSource` | Provides the ability to obtain access tokens that represent the identity of the running service |

If that sounds very similar to the `IServiceIdentityAccessTokenSource` defined by `Corvus.Identity.Abstractions`, it is. On the face of it, we didn't really need to add the new `IServiceIdentityAccessTokenSource`. However, there are two problems with this older `IServiceIdentityTokenSource` interface:

* it is defined in this `Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`, meaning you end up with dependencies on the old `Microsoft.Azure.Services.AppAuthentication` and `Microsoft.Rest.ClientRuntime`
* it has idiosyncracies that made sense when it was written but which are now unhelpful

The idiosyncracies arise from the fact that this interface was specialized for use with either `Microsoft.Rest`-based libraries, or with the old Azure Key Vault client library which, for some reason, used a slightly different approach to authentication for some years.

This library also supplied an adapter, `ServiceIdentityTokenProvider` that provided an implementation of `ITokenProvider` (which is how `Microsoft.Rest`-based libraries obtain credentials) on top of `IServiceIdentityTokenSource`. New code should use either the equivalent `ServiceIdentityMicrosoftRestTokenProvider` in the `Corvus.Identity.MicrosoftRest` library (which wraps any `IServiceIdentityAccessTokenSource` as an `ITokenProvider`), or should work with the `IServiceIdentityMicrosoftRestTokenProviderSource` interface defined by that library in cases where old-style `Microsoft.Rest`-based libraries are still in use. And new code that uses new-style libraries should use `IServiceIdentityAzureTokenCredentialSource`.

The only reason to continue to use this `Corvus.Identity.ManagedServiceIdentity.ClientAuthentication` library would be that you have existing code that uses it, and you don't want to rewrite it. But if you are either writing a new component, or you begin to need to use new-style client libraries, you should use the other components offered by `Corvus.Identity` instead of this one. For that reason, this library's public types and methods are now marked as `Obsolete`.