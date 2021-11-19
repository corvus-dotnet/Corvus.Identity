# Configuration and Azure AD Client Identities

Application code often needs to determine how to authenticate when using services that are secured by Azure AD. In some cases, there will be an ambient service identity (e.g., an Azure Managed Identity). But in some cases, it might be necessary to use some other identity, in which case suitable credentials will need to be obtained. These might need to be retrieve from an Azure Key Vault.

`Corvus.Identity` supports these scenarios through the [`ClientIdentityConfiguration`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration) type. In cases where a single service identity will suffice for the entire process, you can pass one of these to [`AzureIdentityServiceCollectionExtensions.AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration`](xref:Microsoft.Extensions.DependencyInjection.AzureIdentityServiceCollectionExtensions.AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration) to make the service identity available through DI. Or, if you need to use different identities in different scenarios, you can pass a [`ClientIdentityConfiguration`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration) to [`IAzureTokenCredentialSourceFromDynamicConfiguration.CredentialSourceForConfigurationAsync`](`Corvus.Identity.ClientAuthentication.Azure.IAzureTokenCredentialSourceFromDynamicConfiguration.CredentialSourceForConfigurationAsync`)

## Configuration examples

The following sections show examples of how to define a [`ClientIdentityConfiguration`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration) for various common scenarios. (These examples are all in JSON, because this is a common choice for storing configuration. But although `ClientIdentityConfiguration` is designed to work well with JSON mechanisms such as [`Microsoft.Extensions.Configuration.Json`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.json))

### Managed Identity

If your code will be running in an environment that provides an Azure Managed Identity, you just need to set the [`IdentitySourceType`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.IdentitySourceType) to [`Managed`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentitySourceTypes.Managed).

```json
"MyService": {
  "ClientIdentity": { "IdentitySourceType": "Managed" }
}
```

### Managed Identity if available, falling back to local dev options

The old `AzureServiceTokenProvider` that underpins the now-obsolete [`AzureManagedIdentityTokenSource`](xref:Corvus.Identity.ManagedServiceIdentity.ClientAuthentication.AzureManagedIdentityTokenSource) offered a mode in which it would look for an Azure Managed Identity, but if it failed to find one, it would fall back to alternative sources such as Visual Studio, or the Azure CLI. This was convenient for development purposes: it meant that if you published code to Azure it would use the managed identity but it would use your personal credentials when debugging locally. The new `Azure.Identity` libraries provide something similar, which you can opt for by setting [`IdentitySourceType`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.IdentitySourceType) to [`AzureIdentityDefaultAzureCredential`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentitySourceTypes.AzureIdentityDefaultAzureCredential)

```json
"MyService": {
  "ClientIdentity": { "IdentitySourceType": "AzureIdentityDefaultAzureCredential" }
}
```

Be aware that this has some limitations when it falls back to using either Visual Studio or Azure CLI to obtain local credentials: you can only obtain tokens for certain services. So this works fine for common Azure services like Key Vault or ARM, but Azure AD will refuse to issue tokens for any custom services of your own for example (because the Azure AD application registrations for Visual Studio and Azure CLI will not have listed your custom services in the set of applications for which delegated permissions can be obtained). This restriction doesn't apply when this mode is able to use an Azure Managed Identity, so this means that there's a significant difference in behaviour when debugging locally in this mode. If you need tokens for services other than those that VS and `az` are pre-registered for, you'll need to set up a service principle that you can use for local dev work, and configure your code to use that.


### Service principle client/secret credentials in configuration

One way you can set up a [`ClientIdentityConfiguration`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration) to use a particular identity is to set the [`AzureAdAppTenantId`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.AzureAdAppTenantId), [`AzureAdAppClientId`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.AzureAdAppClientId), and [`AzureAdAppClientSecretPlainText`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.AzureAdAppClientSecretPlainText) properties:

```json
"MyService": {
  "ClientIdentity": {
    "AzureAdAppTenantId": "<tenantid>",
    "AzureAdAppClientId": "<appid>",
    "AzureAdAppClientSecretPlainText": "<clientsecret>"
  }
}
```

Note that in this case, you don't need to set [`IdentitySourceType`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.IdentitySourceType), because `Corvus.Identity` will correctly infer which mode you are using from the properties you have set. However, if you prefer to be explicit, you can set the source type to [`ClientIdAndSecret`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentitySourceTypes.ClientIdAndSecret).

The configuration above might be OK for local development purposes, but you will normally want to avoid embedding credentials in configuration settings, so for production purposes, you will more likely want to use a key vault to hold the secret.

### Service principle client/secret credentials, with secret in in a Key Vault accessible to service's Managed Identity

This shows how to define a [`ClientIdentityConfiguration`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration) to use a specific service identity, authenticating with a client secret stored in Azure Key Vault. As with the preceding example, we set the [`AzureAdAppTenantId`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.AzureAdAppTenantId) and [`AzureAdAppClientId`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.AzureAdAppClientId). But the difference here is that instead of putting the secret into the configuration, we set the [`AzureAdAppClientSecretInKeyVault`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.AzureAdAppClientSecretInKeyVault) property, providing the name of the key vault and the secret.

```json
"MyService": {
  "ClientIdentity": {
    "AzureAdAppTenantId": "<tenantid>",
    "AzureAdAppClientId": "<appid>",
    "AzureAdAppClientSecretInKeyVault": {
      "VaultName": "myvault",
      "SecretName": "MyAzureAdAppClientSecret" 
    }
  }
}
```

In the absence of any other information, `Corvus.Identity` will use the service identity to connect to key vault. (So with this particular configuration, it requires an implementation of [`IServiceIdentityAzureTokenCredentialSource`](xref:Corvus.Identity.ClientAuthentication.Azure.IServiceIdentityAzureTokenCredentialSource) to be available through DI.)

### Service principle client/secret credentials, with secret in a customer-controller Key Vault, accessed with a separate service principle with client/secret credentials in a Key Vault accessible to service's Managed Identity

If, as in the preceding example, you want a [`ClientIdentityConfiguration`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration) for a specific service identity, authenticating with a client secret stored in Azure Key Vault, but you don't want to use the ambient service identity in order to talk to key vault, you can set the [`VaultClientIdentity`](xref:Corvus.Identity.ClientAuthentication.Azure.KeyVaultSecretConfiguration.VaultClientIdentity) property in the [`AzureAdAppClientSecretInKeyVault`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration.AzureAdAppClientSecretInKeyVault).

```json
"MyService": {
  "ClientIdentity": {
    "AzureAdAppTenantId": "<tenantid>",
    "AzureAdAppClientId": "<appIdForServicePrincipleWeWant>",
    "AzureAdAppClientSecretInKeyVault": {
      "VaultName": "someoneelsesvault",
      "SecretName": "CustomerAzureAdAppClientSecret",
      "VaultClientIdentity": {
        "AzureAdAppTenantId": "<tenantid>",
        "AzureAdAppClientId": "<appIdWithWhichWeAccessClientKeyVault>",
        "AzureAdAppClientSecretInKeyVault": {
          "VaultName": "myvault",
          "SecretName": "ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault",
          "VaultClientIdentity": { "IdentitySourceType": "Managed" }
        }
      }
    }
  }
}
```

The [`VaultClientIdentity`](xref:Corvus.Identity.ClientAuthentication.Azure.KeyVaultSecretConfiguration.VaultClientIdentity) is a nested [`ClientIdentityConfiguration`](xref:Corvus.Identity.ClientAuthentication.Azure.ClientIdentityConfiguration), so you can specify the identity with which to connect to the Key Vault using all the same techniques shown in the preceding sections. So in this case, we're using `AzureAdAppClientSecretInKeyVault` at two levels because we're actually using two different key vaults (via different identities).


## Multi-tenant scenarios with multiple Azure AD tenants

For some multi-tenanted scenarios, customers may require that certain services (e.g. storage accounts) be entirely under their control. For example, imagine an application that can perform analysis over data in an Azure Data Lake. We might have a customer who wants to use this application, and to have it operate directly on data that is already in a Data Lake in their own Azure Subscription, and they do not want to copy it into some other storage account to be able to use the services our application provides.

So there will be two mostly-separate worlds here: a customer Azure Subscription, and our application's Azure Subscription; a customer Azure AD tenant, and our application's Azure AD tenant. (For brevity, we'll refer to the customer subscription, customer tenant, application subscription, and application tenant.) Our application will run in compute resources associated with the application subscription, and if we enable a Managed Identity, that identity will exist in our application tenant. But the Data Lake our customer wants us to use is in a storage account in the customer subscription, and for authentication and access control purposes, it will only recognize identities known to the customer tenant.

In this scenario, the customer will not want to supply us with the relevant storage account's access keys. (That might be the simplest technical solution, but unless the storage account in question is being used only for the purposes of integrating with our application, it will be unacceptable from a security perspective. In any case, coordinating key rotation would be problematic.) Instead, they are likely to want to create a service principle in their own Azure AD tenant and have our application authenticate with that identity when accessing their Data Lake. That way they can control the exact level of access our application has to their data. The account with the necessary access is defined in the customer tenant, meaning they have complete control over it, and can revoke it at any time.

The question then becomes: how is our application going to authenticate as the customer-defined service principle in the customer tenant?

One possible answer to this is to use a multi-tenant Azure AD application. (**Note**: multi-tenanting of Azure AD applications is a distinct technical mechanism from the broader idea of a multi-tenanted service. Unfortunately these two similar but different concepts have the same name.) If we define such an application in the application tenant, it is possible to create a service principle associated with that application in the customer tenant. (This is essentially the service principle equivalent of adding a user from an external domain as a guest.) The customer can choose to recognize a multi-tenanted AD application, at which point a new service principle gets created in the customer tenant, but it is associated with the Azure AD application in the application tenant. A significant advantage of this is that the credentials for the application belong to the application tenant, but the customer gets to decide what privileges the application has within the customer tenant, and they are free to revoke the application's membership of the customer tenant at any time. In this model, we retain full ownership of the application credentials (meaning that we do not need to coordinate with the customer in order to determine the mechanism used for authentication—e.g. client ID and password vs certificates—nor to rotate keys or otherwise refresh credentials), but the customer remains in full control of what our application is able to do with their resources. (Typically, they would grant the application no capabilities beyond access to the relevant storage account.)

There are two downsides to multi-tenanted Azure AD applications. The first is that Managed Identities do not (as of November 2021) support multi-tenanting. The second is that some customers will simply refuse to use them. It is therefore necessary to be able to authenticate as a service principle defined in a customer tenant.

So it will then become necessary for the application to be able to authenticate as this service principle in the customer tenant. One way to achieve this would be for the corresponding application to be configured for AppId/Client Secret authentication. The customer might insist that put the relevant client secret be put into a distinct Azure Key Vault, accessible only to a particular application service principle (defined in the application tenant) to limit visibility of the relevant credentials. So the application would first need to authenticate as that distinct service to be able to access the Key Vault containing the credentials enabling the application to authenticate as the relevant customer service principle. This is how you might end up needing the two-layer key vault configuration shown above.