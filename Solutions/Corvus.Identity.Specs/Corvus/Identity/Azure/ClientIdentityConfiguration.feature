Feature: ClientIdentityConfiguration
    As the person responsible for deploying and configuring an application that uses services secured by Azure Active Directory
    I need to be able to supply the necessary details and credentials in various different ways
    So that I can obtain the correct credentials while meeting the security requirements of my application

Scenario: Managed identity
    Given configuration of
        """
        {
            "ClientIdentity": { "IdentitySourceType": "Managed" }
        }
        """
    When a TokenCredential is fetched for this configuration
    Then the TokenCredential should be of type 'ManagedIdentityCredential'

# TODO:
# GetReplacementForFailedAccessTokenAsync for ManagedIdentity - not recoverable, so what do we do?

Scenario: Default Azure Credential
    Given configuration of
        """
        {
          "ClientIdentity": { "IdentitySourceType": "AzureIdentityDefaultAzureCredential" }
        }
        """
    When a TokenCredential is fetched for this configuration
    Then the TokenCredential should be of type 'DefaultAzureCredential'

Scenario: Service principle client ID and secret in configuration
    Given configuration of
        """
        {
          "ClientIdentity": {
            "AzureAdAppTenantId": "b39db674-9ba1-4343-8d4e-004675b5d7a8",
            "AzureAdAppClientId": "831c7dcb-516a-4e6b-9b74-347264c67397",
            "AzureAdAppClientSecretPlainText": "s3cret!"
          }
        }
        """
    When a TokenCredential is fetched for this configuration
    Then the TokenCredential should be of type 'ClientSecretCredential'
	And the ClientSecretCredential tenantId should be 'b39db674-9ba1-4343-8d4e-004675b5d7a8'
	And the ClientSecretCredential appId should be '831c7dcb-516a-4e6b-9b74-347264c67397'
	And the ClientSecretCredential clientSecret should be 's3cret!'

Scenario: Service principle client ID in configuration and secret in key vault via service identity
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
    And the key vault 'myvault' returns 's3cret!' for the secret named 'MyAzureAdAppClientSecret'
    When a TokenCredential is fetched for this configuration as credential 'TargetCredentials'
    And in this test we identify the token credential passed when creating the key vault 'myvault' as 'MyVault'
    Then the secret cache should have seen these requests
    | VaultName | SecretName               | Credential |
    | myvault   | MyAzureAdAppClientSecret | null       |
    And the TokenCredential 'TargetCredentials' should be of type 'ClientSecretCredential'
	And the ClientSecretCredential 'TargetCredentials' tenantId should be 'b39db674-9ba1-4343-8d4e-004675b5d7a8'
	And the ClientSecretCredential 'TargetCredentials' appId should be '831c7dcb-516a-4e6b-9b74-347264c67397'
	And the ClientSecretCredential 'TargetCredentials' clientSecret should be 's3cret!'
    And the TokenCredential 'MyVault' should be of type 'ManagedIdentityCredential'
    And the key vault client should have been used 1 times

Scenario: Service principle client ID in configuration and secret in key vault via service identity when secret is in cache
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
    When a TokenCredential is fetched for this configuration as credential 'TargetCredentials'
    Then the secret cache should have seen these requests
    | VaultName | SecretName               | Credential |
    | myvault   | MyAzureAdAppClientSecret | null       |
    And the TokenCredential 'TargetCredentials' should be of type 'ClientSecretCredential'
	And the ClientSecretCredential 'TargetCredentials' tenantId should be 'b39db674-9ba1-4343-8d4e-004675b5d7a8'
	And the ClientSecretCredential 'TargetCredentials' appId should be '831c7dcb-516a-4e6b-9b74-347264c67397'
	And the ClientSecretCredential 'TargetCredentials' clientSecret should be 's3cret!'
    And the key vault client should have been used 0 times

# Using an outline here purely so that we can use symbolic names for the various values.
Scenario Outline: Service principle client ID in configuration and secret in key vault via different service identity with client ID in config and secret in key vault via service identity
    Given configuration of
        """
        {
          "ClientIdentity": {
            "AzureAdAppTenantId": "<targetTenantId>",
            "AzureAdAppClientId": "<targetClientId>",
            "AzureAdAppClientSecretInKeyVault": {
              "VaultName": "customervault",
              "SecretName": "CustomerAzureAdAppClientSecret",
              "VaultClientIdentity": {
                "AzureAdAppTenantId": "<customerVaultTenantId>",
                "AzureAdAppClientId": "<customerVaultClientId>",
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
    And the key vault 'customervault' returns '<targetClientSecret>' for the secret named 'CustomerAzureAdAppClientSecret'
    And the key vault 'myvault' returns '<customerVaultClientSecretInOurVault>' for the secret named 'ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault'
    When a TokenCredential is fetched for this configuration as credential 'TargetCredentials'
    And in this test we identify the token credential passed when creating the key vault 'customervault' as 'CustomerCredentials'
    And in this test we identify the token credential passed when creating the key vault 'myvault' as 'OurVaultCredentials'
    Then the secret cache should have seen these requests
    | VaultName     | SecretName                                               | Credential                                                        |
    | customervault | CustomerAzureAdAppClientSecret                           | AzureAdAppClientSecretInKeyVault                                  |
    | myvault       | ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault | AzureAdAppClientSecretInKeyVault.AzureAdAppClientSecretInKeyVault |
    And the TokenCredential 'TargetCredentials' should be of type 'ClientSecretCredential'
	And the ClientSecretCredential 'TargetCredentials' tenantId should be '<targetTenantId>'
	And the ClientSecretCredential 'TargetCredentials' appId should be '<targetClientId>'
	And the ClientSecretCredential 'TargetCredentials' clientSecret should be '<targetClientSecret>'
    And the TokenCredential 'CustomerCredentials' should be of type 'ClientSecretCredential'
	And the ClientSecretCredential 'CustomerCredentials' tenantId should be '<customerVaultTenantId>'
	And the ClientSecretCredential 'CustomerCredentials' appId should be '<customerVaultClientId>'
	And the ClientSecretCredential 'CustomerCredentials' clientSecret should be '<customerVaultClientSecretInOurVault>'
    And the TokenCredential 'OurVaultCredentials' should be of type 'ManagedIdentityCredential'
    And the key vault client should have been used 2 times

    Examples:
        | targetTenantId                       | targetClientId                       | targetClientSecret | customerVaultTenantId                | customerVaultClientId                | customerVaultClientSecretInOurVault |
        | d0e416b5-1b5d-431e-9448-94a70453889f | 2c5cb5ea-e304-40cf-bdca-81a0d8cf3968 | targetsecret!      | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | vaultSpSecret                       |

Scenario Outline: Service principle client ID in configuration and secret in key vault via different service identity with client ID in config and secret in key vault via service identity when inner secret is in cache
    Given configuration of
        """
        {
          "ClientIdentity": {
            "AzureAdAppTenantId": "<targetTenantId>",
            "AzureAdAppClientId": "<targetClientId>",
            "AzureAdAppClientSecretInKeyVault": {
              "VaultName": "customervault",
              "SecretName": "CustomerAzureAdAppClientSecret",
              "VaultClientIdentity": {
                "AzureAdAppTenantId": "<customerVaultTenantId>",
                "AzureAdAppClientId": "<customerVaultClientId>",
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
    And the key vault 'customervault' returns '<targetClientSecret>' for the secret named 'CustomerAzureAdAppClientSecret'
    And the secret cache returns '<customerVaultClientSecretInOurVault>' for the secret named 'ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault' in 'myvault'
    When a TokenCredential is fetched for this configuration as credential 'TargetCredentials'
    And in this test we identify the token credential passed when creating the key vault 'customervault' as 'CustomerCredentials'
    Then the secret cache should have seen these requests
    | VaultName     | SecretName                                               | Credential                                                        |
    | customervault | CustomerAzureAdAppClientSecret                           | AzureAdAppClientSecretInKeyVault                                  |
    | myvault       | ClientSecretForAzureAdAppWithWhichWeAccessClientKeyVault | AzureAdAppClientSecretInKeyVault.AzureAdAppClientSecretInKeyVault |
    And the TokenCredential 'TargetCredentials' should be of type 'ClientSecretCredential'
	And the ClientSecretCredential 'TargetCredentials' tenantId should be '<targetTenantId>'
	And the ClientSecretCredential 'TargetCredentials' appId should be '<targetClientId>'
	And the ClientSecretCredential 'TargetCredentials' clientSecret should be '<targetClientSecret>'
    And the TokenCredential 'CustomerCredentials' should be of type 'ClientSecretCredential'
	And the ClientSecretCredential 'CustomerCredentials' tenantId should be '<customerVaultTenantId>'
	And the ClientSecretCredential 'CustomerCredentials' appId should be '<customerVaultClientId>'
	And the ClientSecretCredential 'CustomerCredentials' clientSecret should be '<customerVaultClientSecretInOurVault>'
    And the key vault client should have been used 1 times

    Examples:
        | targetTenantId                       | targetClientId                       | targetClientSecret | customerVaultTenantId                | customerVaultClientId                | customerVaultClientSecretInOurVault |
        | d0e416b5-1b5d-431e-9448-94a70453889f | 2c5cb5ea-e304-40cf-bdca-81a0d8cf3968 | targetsecret!      | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | vaultSpSecret                       |

Scenario Outline: Service principle client ID in configuration and secret in key vault via different service identity with client ID in config and secret in key vault via service identity when outer secret is in cache
    Given configuration of
        """
        {
          "ClientIdentity": {
            "AzureAdAppTenantId": "<targetTenantId>",
            "AzureAdAppClientId": "<targetClientId>",
            "AzureAdAppClientSecretInKeyVault": {
              "VaultName": "customervault",
              "SecretName": "CustomerAzureAdAppClientSecret",
              "VaultClientIdentity": {
                "AzureAdAppTenantId": "<customerVaultTenantId>",
                "AzureAdAppClientId": "<customerVaultClientId>",
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
    And the secret cache returns '<targetClientSecret>' for the secret named 'CustomerAzureAdAppClientSecret' in 'customervault'
    When a TokenCredential is fetched for this configuration as credential 'TargetCredentials'
    Then the secret cache should have seen these requests
    | VaultName     | SecretName                                               | Credential                                                        |
    | customervault | CustomerAzureAdAppClientSecret                           | AzureAdAppClientSecretInKeyVault                                  |
    And the TokenCredential 'TargetCredentials' should be of type 'ClientSecretCredential'
	And the ClientSecretCredential 'TargetCredentials' tenantId should be '<targetTenantId>'
	And the ClientSecretCredential 'TargetCredentials' appId should be '<targetClientId>'
	And the ClientSecretCredential 'TargetCredentials' clientSecret should be '<targetClientSecret>'

    Examples:
        | targetTenantId                       | targetClientId                       | targetClientSecret | customerVaultTenantId                | customerVaultClientId                | customerVaultClientSecretInOurVault |
        | d0e416b5-1b5d-431e-9448-94a70453889f | 2c5cb5ea-e304-40cf-bdca-81a0d8cf3968 | targetsecret!      | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | vaultSpSecret                       |
