Feature: TokenCredentialSourceFromDynamicConfiguration with simple ClientIdentityConfiguration
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

Scenario: Azure CLI Credential
    Given configuration of
        """
        {
          "ClientIdentity": { "IdentitySourceType": "AzureCli" }
        }
        """
    When a TokenCredential is fetched for this configuration
    Then the TokenCredential should be of type 'AzureCliCredential'

Scenario: Visual Studio Credential
    Given configuration of
        """
        {
          "ClientIdentity": { "IdentitySourceType": "VisualStudio" }
        }
        """
    When a TokenCredential is fetched for this configuration
    Then the TokenCredential should be of type 'VisualStudioCredential'

Scenario: Visual Studio Code Credential
    Given configuration of
        """
        {
          "ClientIdentity": { "IdentitySourceType": "VisualStudioCode" }
        }
        """
    When a TokenCredential is fetched for this configuration
    Then the TokenCredential should be of type 'AzureCliCredential'
