Feature: TokenCredentialSourceFromDynamicConfiguration Cache Invalidation
    As a developer using client identities obtained from ClientIdentityConfiguration instances
    I need to be able to remove bad credentials from the cache
    So that I can pick up updated credentials in key rotation scenarios

Scenario Outline: Service principle client ID in configuration and secret in key vault via service identity
    Given configuration of
        """
        {
          "ClientIdentity": {
            "AzureAdAppTenantId": "b39db674-9ba1-4343-8d4e-004675b5d7a8",
            "AzureAdAppClientId": "831c7dcb-516a-4e6b-9b74-347264c67397",
            "AzureAdAppClientSecretInKeyVault": {
              "VaultName": "myvault",
              "SecretName": "MyAzureAdAppClientSecret" 
            }
          }
        }
        """
    And the secret cache returns 's3cret!' for the secret named 'MyAzureAdAppClientSecret' in 'myvault'
    When this ClientIdentityConfiguration is invalidated via '<InvalidationMechanism>'
    Then the secret cache should have seen these credentials invalidated
    | VaultName | SecretName               | Credential                       |
    | myvault   | MyAzureAdAppClientSecret | AzureAdAppClientSecretInKeyVault |

    Examples:
    | InvalidationMechanism                               |
    | IAzureTokenCredentialSource                         |
    | IAzureTokenCredentialSourceFromDynamicConfiguration |

Scenario Outline: Service principle client ID in configuration and secret in key vault via different service identity with client ID in config and secret in key vault via service identity
    Given configuration of
        """
        {
          "ClientIdentity": {
            "AzureAdAppTenantId": "d0e416b5-1b5d-431e-9448-94a70453889f",
            "AzureAdAppClientId": "2c5cb5ea-e304-40cf-bdca-81a0d8cf3968",
            "AzureAdAppClientSecretInKeyVault": {
              "VaultName": "customervault",
              "SecretName": "CustomerAzureAdAppClientSecret",
              "VaultClientIdentity": {
                "AzureAdAppTenantId": "b39db674-9ba1-4343-8d4e-004675b5d7a8",
                "AzureAdAppClientId": "831c7dcb-516a-4e6b-9b74-347264c67397",
                "AzureAdAppClientSecretInKeyVault": {
                  "VaultName": "myvault",
                  "SecretName": "ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault",
                  "VaultClientIdentity": { "IdentitySourceType": "Managed" }
                }
              }
            }
          }
        }
        """
    And the key vault 'customervault' returns 'targetsecret!' for the secret named 'CustomerAzureAdAppClientSecret'
    And the key vault 'myvault' returns 'vaultSpSecret' for the secret named 'ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault'
    When this ClientIdentityConfiguration is invalidated via '<InvalidationMechanism>'
    Then the secret cache should have seen these credentials invalidated
    | VaultName | SecretName               | Credential                       |
    | customervault | CustomerAzureAdAppClientSecret                           | AzureAdAppClientSecretInKeyVault                                  |
    | myvault       | ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault | AzureAdAppClientSecretInKeyVault.AzureAdAppClientSecretInKeyVault |

    Examples:
    | InvalidationMechanism                               |
    | IAzureTokenCredentialSource                         |
    | IAzureTokenCredentialSourceFromDynamicConfiguration |
