# Configuration and Azure AD Client Identities

Application code often needs to determine how to authenticate when using services that are secured by Azure AD.

* Service identity
* Service-specific logins
* Associated key vault secrets

## Multi-tenant scenarios with multiple Azure AD tenants

For some multi-tenanted scenarios, customers may want require that certain services (e.g. storage accounts) be entirely under their control. For example, imagine an application that can perform analysis over data in an Azure Data Lake. We might have a customer who wants to use this application, and to have it operate directly on data that is already in a Data Lake in their own Azure Subscription, and they do not want to copy it into some other storage account to be able to use the services our application provides.

So there will be two mostly-separate worlds here: a customer Azure Subscription, and our application's Azure Subscription; a customer Azure AD tenant, and our application's Azure AD tenant. (For brevity, we'll refer to the customer subscription, customer tenant, application subscription, and application tenant.) Our application will run in compute resources associated with the application subscription, and if we enable a Managed Identity, that identity will exist in our application tenant. But the Data Lake our customer wants us to use is in a storage account in the customer subscription, and for authentication and access control purposes, it will only recognize identities known to the customer tenant.

In this scenario, the customer is not going to want to supply us with the relevant storage account's access keys. (That might be the simplest technical solution, but unless the storage account in question is being used only for the purposes of integrating with our application, it will be unacceptable from a security perspective. In any case, coordinating key rotation would be problematic.) Instead, they are likely to want to create a service principle in their own Azure AD tenant and have our application authenticate with that identity when accessing their Data Lake. That way they can control the exact level of access our application has to their data. The account with the necessary access is defined in the customer tenant, meaning they have complete control over it, and can revoke it at any time.

The question then becomes: how is our application going to authenticate as the customer-defined service principle in the customer tenant?

One possible answer to this is to use a multi-tenant Azure AD application. (**Note**: multi-tenanting of Azure AD applications is a distinct technical mechanism from the broader idea of a multi-tenanted service. Unfortunately these two similar but different concepts have the same name.) If we define such an application in the application tenant, it is possible to create a service principle associated with that application in the customer tenant. (This is essentially the service principle equivalent of adding a user from an external domain as a guest.) The customer can choose to recognize a multi-tenanted AD application, at which point a new service principle gets created in the customer tenant, but it is associated with the Azure AD application in the application tenant. A significant advantage of this is that the credentials for the application belong to the application tenant, but the customer gets to decide what privileges the application has within the customer tenant, and they are free to revoke the application's membership of the customer tenant at any time. In this model, we retain full ownership of the application credentials (meaning that we do not need to coordinate with the customer in order to determine the mechanism used for authentication—e.g. client ID and password vs certificates—nor to rotate keys or otherwise refresh credentials), but the customer remains in full control of what our application is able to do with their resources. (Typically, they would grant the application no capabilities beyond access to the relevant storage account.)

There are two downsides to multi-tenanted Azure AD applications. The first is that Managed Identities do not (as of September 2021) support multi-tenanting. The second is that some customers will simply refuse to use them. It is therefore necessary to be able to authenticate as a service principle defined in a customer tenant.

## Configuration examples


### Managed Identity

```json
"MyService": {
  "ClientIdentity": { "IdentitySourceType": "Managed" }
}
```


### Managed Identity if available, falling back to local dev options

```json
"MyService": {
  "ClientIdentity": { "IdentitySourceType": "AzureIdentityDefaultAzureCredential" }
}
```


### Service principle client/secret credentials in configuration

```json
"MyService": {
  "ClientIdentity": {
    "AzureAdAppTenantId": "<tenantid>",
    "AzureAdAppClientId": "<appid>",
    "AzureAdAppClientSecretPlainText": "<clientsecret>"
  }
}
```

### Service principle client/secret credentials, with secret in in a Key Vault accessible to service's Managed Identity


```json
"MyService": {
  "ClientIdentity": {
    "AzureAdAppTenantId": "<tenantid>",
    "AzureAdAppClientId": "<appid>",
    "AzureAdAppClientSecretInKeyVault" {
      "VaultName": "myvault",
      "SecretName": "MyAzureAdAppClientSecret" 
    }
  }
}
```

### Service principle client/secret credentials, with secret in a customer-controller Key Vault, accessed with a separate service principle with client/secret credentials in a Key Vault accessible to service's Managed Identity

```json
"MyService": {
  "ClientIdentity": {
    "AzureAdAppClientId": "<appid>",
    "AzureAdAppTenantId": "<tenantid>",
    "AzureAdAppClientSecretInKeyVault" {
      "VaultName": "someoneelsesvault",
      "SecretName": "CustomerAzureAdAppClientSecret",
      "VaultClientIdentity": {
        "AzureAdAppTenantId": "<tenantid>",
        "AzureAdAppClientId": "<appIdWithWhichWeAccessClientKeyVault>",
        "AzureAdAppClientSecretInKeyVault" {
          "VaultName": "myvault",
          "SecretName": "ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault",
          "VaultClientIdentity": { "IdentitySourceType": "Managed" }
        }
      }
    }
  }
}
```
