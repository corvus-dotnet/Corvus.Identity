Feature: CachingKeyVaultClient
    As a user of an application that stores secrets in Azure Key Vault
    I need to avoid repeated lookups of the same secret time and time again
    So that the application response times are not unduly affected by Key Vault's relatively slow performance

Background:
    Given the following ClientIdentityConfigurations
        """
        {
          "ManagedIdentity": { "IdentitySourceType": "Managed" },
          "AdAppWithPlainTextSecret": {
            "AzureAdAppTenantId": "b39db674-9ba1-4343-8d4e-004675b5d7a8",
            "AzureAdAppClientId": "831c7dcb-516a-4e6b-9b74-347264c67397",
            "AzureAdAppClientSecretPlainText": "P@ssw@ord!"
          }
        }
        """

Scenario: First time secret requested, so we talk to key vault
    Given a caching SecretClient 'c1' for the key vault 'myvault' and the identity configuration 'ManagedIdentity'
    And the key vault 'myvault' returns 's3cret!' for the secret named 'MySecret'
    When a secret named 'MySecret' is fetched through the caching SecretClient 'c1' and then labelled 'r1'
    Then the following secrets are retrieved from key vault
    | VaultName | SecretName | WithClientIdentity |
    | myvault   | MySecret   | ManagedIdentity    |
    And the returned secret 'r1' has the value 's3cret!'

Scenario: Secret requested twice through one SecretClient, so we return the cached version the second time
    Given a caching SecretClient 'c1' for the key vault 'myvault' and the identity configuration 'ManagedIdentity'
    And the key vault 'myvault' returns 's3cret!' for the secret named 'MySecret'
    When a secret named 'MySecret' is fetched through the caching SecretClient 'c1' and then labelled 'r1'
    And a secret named 'MySecret' is fetched through the caching SecretClient 'c1' and then labelled 'r2'
    Then the following secrets are retrieved from key vault
    | VaultName | SecretName | WithClientIdentity |
    | myvault   | MySecret   | ManagedIdentity    |
    And the returned secret 'r1' has the value 's3cret!'
    And the returned secret 'r2' has the value 's3cret!'

Scenario: Secret requested twice for this identity, albeit through different SecretClients, so we return the cached version the second time
    Given a caching SecretClient 'c1' for the key vault 'myvault' and the identity configuration 'ManagedIdentity'
    And a caching SecretClient 'c2' for the key vault 'myvault' and the identity configuration 'ManagedIdentity'
    And the key vault 'myvault' returns 's3cret!' for the secret named 'MySecret'
    When a secret named 'MySecret' is fetched through the caching SecretClient 'c1' and then labelled 'r1'
    And a secret named 'MySecret' is fetched through the caching SecretClient 'c2' and then labelled 'r2'
    Then the following secrets are retrieved from key vault
    | VaultName | SecretName | WithClientIdentity |
    | myvault   | MySecret   | ManagedIdentity    |
    And the returned secret 'r1' has the value 's3cret!'
    And the returned secret 'r2' has the value 's3cret!'

Scenario: Secret requested twice through different SecretClients and on behalf of different identities, so we talk to key vault both times
    Given a caching SecretClient 'c1' for the key vault 'myvault' and the identity configuration 'ManagedIdentity'
    And a caching SecretClient 'c2' for the key vault 'myvault' and the identity configuration 'AdAppWithPlainTextSecret'
    And the key vault 'myvault' returns 's3cret!' for the secret named 'MySecret'
    When a secret named 'MySecret' is fetched through the caching SecretClient 'c1' and then labelled 'r1'
    And a secret named 'MySecret' is fetched through the caching SecretClient 'c2' and then labelled 'r2'
    Then the following secrets are retrieved from key vault
    | VaultName | SecretName | WithClientIdentity       |
    | myvault   | MySecret   | ManagedIdentity          |
    | myvault   | MySecret   | AdAppWithPlainTextSecret |
    And the returned secret 'r1' has the value 's3cret!'
    And the returned secret 'r2' has the value 's3cret!'


Scenario: Secret requested, invalidated, then requested again through one SecretClient, so we talk to key vault both times
    Given a caching SecretClient 'c1' for the key vault 'myvault' and the identity configuration 'ManagedIdentity'
    And the key vault 'myvault' returns 's3cret!' for the secret named 'MySecret'
    When a secret named 'MySecret' is fetched through the caching SecretClient 'c1' and then labelled 'r1'
    And the secret named 'MySecret' in key vault 'myvault' is invalidated
    And a secret named 'MySecret' is fetched through the caching SecretClient 'c1' and then labelled 'r2'
    Then the following secrets are retrieved from key vault
    | VaultName | SecretName | WithClientIdentity |
    | myvault   | MySecret   | ManagedIdentity    |
    | myvault   | MySecret   | ManagedIdentity    |
    And the returned secret 'r1' has the value 's3cret!'
    And the returned secret 'r2' has the value 's3cret!'


# TODO:
# Scenario: Secret is in the cache but has expired so we talk to key vault
# Scenario: Multiple concurrent requests to for the secret results in a single call to key vault
#
# Cache invalidation:
#
# Scenario: Requesting a replacement token for Service principle client ID in configuration and secret in key vault via service identity should result in a call to key vault and should replace the failed token in the cache
# Scenario: Requesting a replacement token when the existing token has been evicted and no replacent has been set and should result in a call to key vault
# Scenario: Requesting a replacement token for which an updated token exists in the cache and should not result in a call to key vault
