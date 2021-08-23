# Getting started with `Corvus.Identity` when using a mixture of new- and old-style Azure SDK libraries

New-style libraries are those whose names begin with `Azure.` and which use the `Azure.Core` library's `TokenCredential` type to manage authentication. Old-style Azure client libraries typically have names beginning with `Microsoft.` and use the `Microsoft.Rest.ClientRuntime` library's `ITokenProvider` type to manage authentication. (See the [Azure SDK changes](old-vs-new-azure-sdk.md) article for details.) If you need to use a mixture of each type of client library, you can follow these instructions when writing code that needs to authenticate using the hosting service's identity.

Add references to both the `Corvus.Identity.MicrosoftRest` and `Corvus.Identity.Azure` NuGet packages.

These packages are designed to be used through dependency injection. In your application's startup code, add the following where you initialize the `IServiceCollection` (where `config` refers to your application's `IConfiguration`):

```cs
// Makes IServiceIdentityMicrosoftRestTokenProviderSource, IServiceIdentityAccessTokenSource,
// and IServiceIdentityAzureTokenCredentialSource available through DI. We're using the form
// that supports an `AzureServicesAuthConnectionString` configuration setting, choosing which
// credentials mechanism to use.
services.AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource();
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    config.Get<LegacyAzureServiceTokenProviderOptions>());
```

With this in place, you can then write classes that take a dependency either (or both) of two interfaces for obtaining credentials.

When working with a new-style client library, you can write classes that take a dependency on [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource), which provides a straightforward way to obtain credentials in the form required by modern Azure SDK client libraries:

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

This is using the Azure Key Vault client library's `SecretClient` class. This code initializes it with a `TokenCredential` obtained from the [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource) it acquired through dependency injection.

This same basic pattern works for any client library that uses the `TokenCredential` mechanism defined by `Azure.Core`.

When working with an old-style client library, you can write classes that take a dependency on [`IServiceIdentityMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IServiceIdentityMicrosoftRestTokenProviderSource), which provides a straightforward way to obtain credentials in the form required by old-style `Microsoft.Rest`-based client libraries:

```cs
public class UseWebAppManagementWithOldSdk
{
    private readonly IServiceIdentityMicrosoftRestTokenProviderSource tokenProviderSource;

    public UseWebAppManagementWithOldSdk(
        IServiceIdentityMicrosoftRestTokenProviderSource tokenProviderSource)
    {
        this.tokenProviderSource = tokenProviderSource
            ?? throw new ArgumentNullException(nameof(tokenProviderSource));
    }

    public async Task<List<string>> GetWebAppsAsync(string subscriptionId)
    {
        ITokenProvider tokenProvider = await this.tokenProviderSource.GetTokenProviderAsync(
            "https://management.azure.com//.default")
            .ConfigureAwait(false);
        var credentials = new TokenCredentials(tokenProvider);
        var client = new WebSiteManagementClient(credentials)
        {
            SubscriptionId = subscriptionId,
        };

        IPage<Site> sitesPage = await client.WebApps.ListAsync().ConfigureAwait(false);
        var result = new List<string>();
        while (true)
        {
            result.AddRange(sitesPage.Select(s => s.Id));
            if (sitesPage.NextPageLink == null)
            {
                break;
            }

            sitesPage = await client.WebApps.ListNextAsync(sitesPage.NextPageLink).ConfigureAwait(false);
        }

        return result;
    }
```

This is using the `Microsoft.Azure.Management.Websites` client library's `WebSiteManagementClient` class. This code initializes it with a `TokenCredentials` initialized with an `ITokenProvider` obtained from the [`IServiceIdentityMicrosoftRestTokenProviderSource`](xref:Corvus.Identity.ClientAuthentication.MicrosoftRest.IServiceIdentityMicrosoftRestTokenProviderSource) it acquired through dependency injection.

This same basic pattern works for any client library that uses the `ITokenProvider` mechanism defined by the `Microsoft.Rest` libraries.

## Alternatives

The code shown above allows application configuration to use old-style connection strings of the kind supported by the old `AzureServiceTokenProvider` type. (Microsoft chose to drop support for this style of configuration in their SDK redesign. `Corvus.Identity` enables you to continue to use it.)

If you want to support this use of connection strings but for some reason you don't want to use Microsoft's `IConfiguration` mechanism, you can either construct a `LegacyAzureServiceTokenProviderOptions` directly, putting the connection string into its [`AzureServicesAuthConnectionString` property](xref:Corvus.Identity.ClientAuthentication.Azure.LegacyAzureServiceTokenProviderOptions.AzureServicesAuthConnectionString), or you can just use the overload the takes a plain string:

```cs
// Makes IServiceIdentityMicrosoftRestTokenProviderSource, IServiceIdentityAccessTokenSource,
// and IServiceIdentityAzureTokenCredentialSource available through DI. We're using the form
// that supports an `AzureServicesAuthConnectionString` configuration setting, choosing which
// credentials mechanism to use, but without having to use IConfiguration.
services.AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource();
services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
    myAuthenticationConnectionString);
```

This connection string system is helpful because it enabled you to ensure that when code was deployed to production, it used the appropriate mechanisms (typically a Managed Identity) but enabled you to configure a service principle manually for local debugging. The new-style SDK provides no configuration-driven mechanism for switching between these two systems.

If, however, you do not want support for these kinds of connection strings, you can instead use this alternative registration call:

```cs
// Use this if you don't want connection-string-driven authentication configuration.

// Makes IServiceIdentityMicrosoftRestTokenProviderSource, IServiceIdentityAccessTokenSource,
// and IServiceIdentityAzureTokenCredentialSource available through DI.
services.AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource();
services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(
    new DefaultAzureCredential());
```
