# Getting started with `Corvus.Identity` when using new-style Azure SDK libraries

New-style libraries are those whose names begin with `Azure.` and which use the [`Azure.Core`](xref:Azure.Core) library's [`TokenCredential`](xref:Azure.Core.TokenCredential) type to manage authentication. (See the [Azure SDK changes](old-vs-new-azure-sdk.md) article for details.) If all of the client libraries you need to use are of this kind, you can follow these instructions when writing code that needs to authenticate using the hosting service's identity.

Add a reference to the [`Corvus.Identity.MicrosoftRest`](https://www.nuget.org/packages/Corvus.Identity.MicrosoftRest/) NuGet package.

This package is designed to be used through dependency injection. In your application's startup code, add the following where you initialize the [`IServiceCollection`](xref:Microsoft.Extensions.DependencyInjection.IServiceCollection) (where `config` refers to your application's [`IConfiguration`](xref:Microsoft.Extensions.Configuration.IConfiguration)):

```cs
// Makes IServiceIdentityAccessTokenSource and IServiceIdentityAzureTokenCredentialSource
// available through DI, with the `AzureServicesAuthConnectionString` configuration choosing
// which credentials mechanism to use.
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    config.Get<LegacyAzureServiceTokenProviderOptions>());
```

With this in place, you can then write classes that take a dependency on [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource), which provides a straightforward way to obtain credentials in the form required by modern Azure SDK client libraries. All it requires is a single call to the [`GetAccessTokenAsync`](xref:Corvus.Identity.ClientAuthentication.Azure.IAzureTokenCredentialSource.GetAccessTokenAsync) method, which you can see at the start of the `GetSecretAsync` method in the following example:

```cs
public class UseAzureKeyVaultWithNewSdk
{
    private readonly IServiceIdentityAzureTokenCredentialSource tokenCredentialSource;

    public UseAzureKeyVaultWithNewSdk(
        IServiceIdentityAzureTokenCredentialSource tokenCredentialSource)
    {
        this.tokenCredentialSource = tokenCredentialSource
            ?? throw new ArgumentNullException(nameof(tokenCredentialSource));
    }

    public async Task<string> GetSecretAsync(
        Uri keyVaultUri, string secretName)
    {
        TokenCredential credential = await this.tokenCredentialSource.GetAccessTokenAsync()
            .ConfigureAwait(false);
        var client = new SecretClient(keyVaultUri, credential);

        Response<KeyVaultSecret> secret = await client.GetSecretAsync(secretName)
            .ConfigureAwait(false);

        return secret.Value.Value;
    }
}
```

This is using the Azure Key Vault client library's [`SecretClient`](xref:Azure.Security.KeyVault.Secrets.SecretClient) class. This code initializes it with a [`TokenCredential`](xref:Azure.Core.TokenCredential) obtained from the [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource) it acquired through dependency injection.

This same basic pattern works for any client library that uses the [`TokenCredential`](xref:Azure.Core.TokenCredential) mechanism defined by [`Azure.Core`](xref:Azure.Core).

## Alternatives

The code shown above allows application configuration to use old-style connection strings of the kind supported by the old `AzureServiceTokenProvider` type. (Microsoft chose to drop support for this style of configuration in their SDK redesign. `Corvus.Identity` enables you to continue to use it.)

If you want to support this use of connection strings but for some reason you don't want to use Microsoft's [`IConfiguration`](xref:Microsoft.Extensions.Configuration.IConfiguration) mechanism, you can either construct a [`LegacyAzureServiceTokenProviderOptions`](xref:Corvus.Identity.ClientAuthentication.Azure.LegacyAzureServiceTokenProviderOptions) directly, putting the connection string into its [`AzureServicesAuthConnectionString` property](xref:Corvus.Identity.ClientAuthentication.Azure.LegacyAzureServiceTokenProviderOptions.AzureServicesAuthConnectionString), or you can just use the overload the takes a plain string:

```cs
// Makes IServiceIdentityAccessTokenSource and IServiceIdentityAzureTokenCredentialSource
// available through DI, using connection-string-based configuration, but without having
// to use IConfiguration.
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    myAuthenticationConnectionString);
```

This connection string system is helpful because it enabled you to ensure that when code was deployed to production, it used the appropriate mechanisms (typically a Managed Identity) but enabled you to configure a service principle manually for local debugging. The new-style SDK provides no configuration-driven mechanism for switching between these two systems.

If, however, you do not want support for these kinds of connection strings, you can instead use this alternative registration call:

```cs
// Use this if you don't want connection-string-driven authentication configuration.

// Makes IServiceIdentityAccessTokenSource and IServiceIdentityAzureTokenCredentialSource
// available through DI, with a hard-coded token acquisition mechanism.
services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(
    new DefaultAzureCredential());
```

This uses [`DefaultAzureCredential`](xref:Azure.Identity.DefaultAzureCredential), but you can supply any type derived from [`TokenCredential`](xref:Azure.Core.TokenCredential) if you want other behaviours.
