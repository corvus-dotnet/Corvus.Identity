# Corvus.Identity

`Corvus.Identity` is a set of NuGet packages that support services that need to authenticate when calling out to other services. For instructions on how to get up and running quickly, see the [Getting started](articles/getting-started.md) page.

When writing code that uses a service, we very often need to authenticate. This is often achieved by supplying an access token as the `Authorization` header of an HTTP request. `Corvus.Identity` supports this in various ways by providing:

* abstractions for obtaining access tokens in various forms:
    * plain text (for direct use in an `Authorization` header)
    * [`TokenCredential`](xref:Azure.Core.TokenCredential) (defined in [`Azure.Core`](https://www.nuget.org/packages/Azure.Core/), and used in modern Azure SDK client libraries)
    * [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) (defined in [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/), and used in older Azure SDK client libraries, and many [AutoRest](https://github.com/Azure/AutoRest)-generated clients)
* a way for components to obtain token sources representing the service or application identity
* a migration path from the older [`Microsoft.Rest`](xref:Microsoft.Rest) system to the new [`Azure.Core`](xref:Azure.Core)/[`Azure.Identity`](xref:Azure.Identity) model

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

This code requires an implementation of `Corvus.Identity`'s [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource). That indicates that it wants to authenticate as the running service's identity, and that it wants to use [`TokenCredential`](xref:Azure.Core.TokenCredential) objects (as defined by [`Azure.Core`](xref:Azure.Core)) to do this. This is typically supplied via dependency injection, so you would have code such as this in the application startup:

```cs
// Enable use of AzureServiceTokenProvider-style connection strings in the
// AzureServicesAuthConnectionString configuration setting.
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    config.Get<LegacyAzureServiceTokenProviderOptions>());
```

Note that this particular example refers to a "legacy connection string". That's because the [`Corvus.Identity.Azure`](https:///www.nuget.org/packages/Corvus.Identity.Azure/) library provides support for continuing to use and older form of configuration that Microsoft's own libraries dropped support for when [`Azure.Identity`](xref:Azure.Identity) shipped. The code here presumes `config` is an [`IConfiguration`](xref:Microsoft.Extensions.Configuration.IConfiguration) object, and the [`LegacyAzureServiceTokenProviderOptions`](xref:Corvus.Identity.ClientAuthentication.Azure.LegacyAzureServiceTokenProviderOptions) being used for configuration binding here expects the connection string to be in a setting called `AzureServicesAuthConnectionString`. This supports most of the same formats as `AzureServiceTokenProvider`. E.g., you can set this to `RunAs=App` to indicate that you want to use the ambient Managed Identity. (That only works when the code is running in Azure in an environment that supplies a Managed Identity.) Or you can use a connection string of the form `RunAs=App;AppId=<appId>;TenantId=<tenantId>;AppKey=<clientSecret>` when debugging on your local machine, to provide credentials for a specific service principal.

These old-style connection strings are useful because they provide a level of runtime configurability that [`Azure.Identity`](xref:Azure.Identity) doesn't support out of the box. However, if you don't want to use them you can just provide a specific [`TokenCredential`](xref:Azure.Core.TokenCredential) implementation. E.g., if you want the same behaviour you would have got with the old `AzureServiceTokenProvider` by default (if you leave `AzureServicesAuthConnectionString` blank), in which it will detect automatically when an Azure Managed Identity is available, and if not, fall back to local providers such as the Azure CLI, or Visual Studio) you can write this in your startup:

```cs
// Use Azure Managed Identity where available, and if not, attempt to acquire
// credentials via Visual Studio, Azure CLI, or Azure PowerShell.
services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(
    new DefaultAzureCredential());
```

But the key point here is that the `UseAzureKeyVaultWithNewSdk` class doesn't need to know exactly which type of [`TokenCredential`](xref:Azure.Core.TokenCredential) it's getting, or how it is created. It is just declaring that it needs to be able to get [`TokenCredential`](xref:Azure.Core.TokenCredential)s that represent the service identity, which it can then pass into client APIs that use this kind of credential, such as the Azure Key Vault [`SecretClient`](xref:Azure.Security.KeyVault.Secrets.SecretClient) shown in the example.


## Components

`Corvus.Identity` is a set of libraries, each of which is described in the following sections.

### `Corvus.Identity.Abstractions`

This defines abstractions that are not specific to any particular technology or authentication mechanism. It defines two interfaces:

| Interface | Purpose |
| --- | --- |
| [`IAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IAccessTokenSource) | Provides the ability to obtain an access token |
| [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource) | A specialized [`IAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IAccessTokenSource) providing tokens that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and which wants to work directly with the raw access token, take a dependency on [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource). (Note that any application using such a component will need to ensure that an implementation of this interface is available, typically by using the [`Corvus.Identity.Azure`](https:///www.nuget.org/packages/Corvus.Identity.Azure/) library.)

### Corvus.Identity.Azure

This provides an implementation of the [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource) defined by [`Corvus.Identity.Abstractions`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) based on [`Azure.Core`](xref:Azure.Core)/[`Azure.Identity`](xref:Azure.Identity), and also two more specialized abstractions (and corresponding implementations) intended for use by components that work with [`Azure.Core`](xref:Azure.Core)-based libraries, such as "new"-style Azure SDK client libraries.

| Interface | Purpose |
| --- | --- |
| [`IAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IAzureTokenCredentialSource) | Provides the ability to obtain an [`Azure.Core`](xref:Azure.Core) style [`TokenCredential`](xref:Azure.Core.TokenCredential) |
| [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource) | A specialized [`IAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IAzureTokenCredentialSource) providing tokens that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and to do so through the [`TokenCredential`](xref:Azure.Core.TokenCredential) (e.g., to use a new-style Azure SDK client library), take a dependency on [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource). (The `UseAzureKeyVaultWithNewSdk` shown in the example earlier does this.)

This library also defines extension methods for [`IServiceCollection`](xref:Microsoft.Extensions.DependencyInjection.IServiceCollection). All of these have the same effect: they make implementations of [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource) and [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource) available via DI. This table describes which to use

| Method | Purpose |
| --- | --- |
| [`AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString`](xref:Microsoft.Extensions.DependencyInjection.AzureIdentityServiceCollectionExtensions.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString*) | Uses `AzureServiceTokenProvider`-style connection strings to determine which kind of [`TokenCredential`](xref:Azure.Core.TokenCredential) to use |
| [`AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential`](xref:Microsoft.Extensions.DependencyInjection.AzureIdentityServiceCollectionExtensions.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential*) | Uses the [`TokenCredential`](xref:Azure.Core.TokenCredential) you provide |



### Corvus.Identity.MicrosoftRest

This defines two specialized abstractions intended for use by components that work with [`Microsoft.Rest`](xref:Microsoft.Rest)-based libraries, such as "old"-style Azure SDK client libraries, or Autorest-based clients, and it provides an implementation that is an adapter on top of [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource).

| Interface | Purpose |
| --- | --- |
| [`IMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IMicrosoftRestTokenProviderSource) | Provides the ability to obtain a [`Microsoft.Rest`](xref:Microsoft.Rest) style [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) |
| [`IServiceIdentityMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IServiceIdentityMicrosoftRestTokenProviderSource) | A specialized [`IMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IMicrosoftRestTokenProviderSource) suppling token providers that represent the identity of the running service |

If you are writing a component that needs to authenticate as the service identity, and to do so through the [`Microsoft.Rest`](xref:Microsoft.Rest) family of libraries' [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) (e.g., to use an old-style Azure SDK client library), take a dependency on [`IServiceIdentityMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IServiceIdentityMicrosoftRestTokenProviderSource).

This library also provides a single extension method for [`IServiceCollection`](xref:Microsoft.Extensions.DependencyInjection.IServiceCollection), making an implementation of [`IServiceIdentityMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IServiceIdentityMicrosoftRestTokenProviderSource) available through DI: [`AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource`](xref:Microsoft.Extensions.DependencyInjection.MicrosoftRestIdentityServiceCollectionExtensions.AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource*). Note that this requires an implementation of [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource) to provide the actual tokens, because this library only provides adapters from that general-purpose form to the more specialized form required by [`Microsoft.Rest`](xref:Microsoft.Rest)-based code. So you would typically have a pair of calls in your DI startup:

```cs
// Enable use of AzureServiceTokenProvider-style connection strings in the
// AzureServicesAuthConnectionString configuration setting.
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    config.Get<LegacyAzureServiceTokenProviderOptions>());
// Provide tokens in a form that old Microsoft.Rest-style libraries can use.
services.AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource();
```

With this in your startup, you will be able to use both old- and new-style Azure client libraries. You can take a dependency on [`IServiceIdentityMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IServiceIdentityMicrosoftRestTokenProviderSource) to obtain tokens for old-style libraries, and on [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource) to obtain tokens for new-style libraries. The same underlying [`TokenCredential`](xref:Azure.Core.TokenCredential) will be used in each case.

### Corvus.Identity.ManagedServiceIdentity.ClientAuthentication

This library is the oldest in `Corvus.Identity`, and it dates back to before the new-style Azure SDK. You should not use this in new code.

The one benefit this library offers is that if you are working purely in a [`Microsoft.Rest`](xref:Microsoft.Rest) world, you can use this without taking any dependency on [`Azure.Core`](xref:Azure.Core) or [`Azure.Identity`](xref:Azure.Identity). (If you use the startup code shown in the preceding section for the `Corvus.Identity.MicrosoftRest` library, you will get access to credentials in a form suitable for old-style SDKs, but they will be implemented as wrappers on top of the newer mechanisms, meaning you will end up with dependencies on [`Azure.Core`](xref:Azure.Core) and [`Azure.Identity`](xref:Azure.Identity) even if you're not using any new-style SDK libraries.) This library depends only on [`Microsoft.Azure.Services.AppAuthentication`](https://www.nuget.org/packages/Microsoft.Azure.Services.AppAuthentication/) (the older wrapper for Azure Managed Identities), [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/), and [`Microsoft.Extensions.DependencyInjection.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/).

This library defines a single interface:

| Interface | Purpose |
| --- | --- |
| [`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource) | Provides the ability to obtain access tokens that represent the identity of the running service |

If that sounds very similar to the [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource) defined by [`Corvus.Identity.Abstractions`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/), that's because it is. On the face of it, we didn't really need to add the new [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource). However, there are two problems with this older [`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource) interface:

* it is defined in this [`Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) package, meaning you end up with dependencies on the old [`Microsoft.Azure.Services.AppAuthentication`](https://www.nuget.org/packages/Microsoft.Azure.Services.AppAuthentication/) and [`Microsoft.Rest.ClientRuntime`](https://www.nuget.org/packages/Microsoft.Rest.ClientRuntime/)
* it has idiosyncracies that made sense when it was written but which are now unhelpful

The idiosyncracies arise from the fact that this interface was specialized for use with either [`Microsoft.Rest`](xref:Microsoft.Rest)-based libraries, or with the old Azure Key Vault client library which, for some reason, used a slightly different approach to authentication for some years.

This library also supplied an adapter, `ServiceIdentityTokenProvider` that provided an implementation of [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider) (which is how [`Microsoft.Rest`](xref:Microsoft.Rest)-based libraries obtain credentials) on top of [`IServiceIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.IServiceIdentityTokenSource). New code should use either the equivalent [`ServiceIdentityMicrosoftRestTokenProvider`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.ServiceIdentityMicrosoftRestTokenProvider) in the [`Corvus.Identity.MicrosoftRest`](https://www.nuget.org/packages/Corvus.Identity.MicrosoftRest/) library (which wraps any [`IServiceIdentityAccessTokenSource`](xref:Corvus.Identity.ClientAuthentication.IServiceIdentityAccessTokenSource) as an [`ITokenProvider`](xref:Microsoft.Rest.ITokenProvider)), or should work with the [`IServiceIdentityMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IServiceIdentityMicrosoftRestTokenProviderSource) interface defined by that library in cases where old-style [`Microsoft.Rest`](xref:Microsoft.Rest)-based libraries are still in use. And new code that uses new-style libraries should use [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource).

The only reason to continue to use this [`Corvus.Identity.ManagedServiceIdentity.ClientAuthentication`](https:///www.nuget.org/packages/Corvus.Identity.ManagedServiceIdentity.ClientAuthentication/) library would be that you have existing code that uses it, and you don't want to rewrite it. But if you are either writing a new component, or you begin to need to use new-style client libraries, you should use the other components offered by `Corvus.Identity` instead of this one. For that reason, this library's public types and methods are marked as `Obsolete` as of v2.0.