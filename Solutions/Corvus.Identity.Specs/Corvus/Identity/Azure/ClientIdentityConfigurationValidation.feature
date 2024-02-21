Feature: ClientIdentityConfigurationValidation
    As a developer or administrator configuring an application
    I want to receive useful errors when an ClientIdentityConfiguration is incorrect
    So that I can understand and resolve the situation quickly

Scenario Outline: Simple valid configuration
    Given configuration of
        """
        {
            "ClientIdentity": { "IdentitySourceType": "<IdentitySourceType>" }
        }
        """
    When I validate the configuration
    Then the validation should pass
    And the validated type should be '<IdentitySourceType>'

    Examples:
    | IdentitySourceType                  |
    | Managed                             |
    | AzureCli                            |
    | VisualStudio                        |
    | AzureIdentityDefaultAzureCredential |

Scenario: Null configuration
    Given configuration of
        """
        {
          "ClientIdentity": null
        }
        """
    When I validate the configuration
    Then the validation should fail with 'must not be null'
    
Scenario: No apparent source type
    Given configuration of
        """
        {
          "ClientIdentity": { "IdentitySourceType": null }
        }
        """
    When I validate the configuration
    Then the validation should fail with 'unable to determine identity type because no suitable properties have been set'

Scenario Outline: Unknown identity source type
    Given a ClientIdAndSecret configuration with '', '<AzureAppTenantId>', '<AzureAdAppClientId>', ''
    When I validate the configuration
    Then the validation should fail with 'unable to determine identity type because no suitable properties have been set'

    Examples:
    | AzureAppTenantId                     | AzureAdAppClientId                   |
    | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      |
    |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 |
    | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 |

Scenario Outline: Good ClientIdAndSecret
    Given a ClientIdAndSecret configuration with '<IdentitySourceType>', '<AzureAppTenantId>', '<AzureAdAppClientId>', '<AzureAdAppClientSecretPlainText>'
    When I validate the configuration
    Then the validation should pass
    And the validated type should be 'ClientIdAndSecret'

    Examples:
    | IdentitySourceType | AzureAppTenantId                     | AzureAdAppClientId                   | AzureAdAppClientSecretPlainText |
    |                    | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | s3cret!                         |
    | ClientIdAndSecret  | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | s3cret!                         |

Scenario Outline: Bad ClientIdAndSecret plaintext
    Given a ClientIdAndSecret configuration with '<IdentitySourceType>', '<AzureAppTenantId>', '<AzureAdAppClientId>', '<AzureAdAppClientSecretPlainText>'
    When I validate the configuration
    Then the validation should fail with 'ClientIdAndSecret configuration must provide AzureAppTenantId, AzureAdAppClientId, and either AzureAppClientSecretPlainText or AzureAdAppClientSecretInKeyVault'

    Examples:
    | IdentitySourceType | AzureAppTenantId                     | AzureAdAppClientId                   | AzureAdAppClientSecretPlainText |
    |                    |                                      |                                      | s3cret!                         |
    |                    | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      | s3cret!                         |
    |                    |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 | s3cret!                         |
    | ClientIdAndSecret  |                                      |                                      |                                 |
    | ClientIdAndSecret  | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      |                                 |
    | ClientIdAndSecret  |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 |                                 |
    | ClientIdAndSecret  |                                      |                                      | s3cret!                         |
    | ClientIdAndSecret  | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 |                                 |
    | ClientIdAndSecret  | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      | s3cret!                         |
    | ClientIdAndSecret  |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 | s3cret!                         |

Scenario Outline: Bad ClientIdAndSecret with secret in key vault
    Given a ClientIdAndSecret configuration with '<IdentitySourceType>', '<AzureAppTenantId>', '<AzureAdAppClientId>', '<AzureAdAppClientSecretPlainText>' and a secret in key vault
    When I validate the configuration
    Then the validation should fail with 'ClientIdAndSecret configuration must provide AzureAppTenantId, AzureAdAppClientId, and either AzureAppClientSecretPlainText or AzureAdAppClientSecretInKeyVault'

    Examples:
    | IdentitySourceType | AzureAppTenantId                     | AzureAdAppClientId                   | AzureAdAppClientSecretPlainText |
    |                    | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      |                                 |
    |                    |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 |                                 |
    |                    | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | s3cret!                         |
    | ClientIdAndSecret  |                                      |                                      |                                 |
    | ClientIdAndSecret  | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      |                                 |
    | ClientIdAndSecret  |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 |                                 |
    | ClientIdAndSecret  | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | s3cret!                         |

Scenario Outline: Good ClientIdAndCertificate
    Given a ClientIdAndCertificate configuration with '<IdentitySourceType>', '<AzureAppTenantId>', '<AzureAdAppClientId>', '<StoreLocation>', '<StoreName>', '<SubjectName>'
    When I validate the configuration
    Then the validation should pass
    And the validated type should be 'ClientIdAndCertificate'

    Examples:
    | IdentitySourceType     | AzureAppTenantId                     | AzureAdAppClientId                   | StoreLocation | StoreName | SubjectName |
    |                        | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   | My        | CN=MyCert   |
    | ClientIdAndCertificate | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   | My        | CN=MyCert   |

Scenario Outline: Bad ClientIdAndCertificate
    Given a ClientIdAndCertificate configuration with '<IdentitySourceType>', '<AzureAppTenantId>', '<AzureAdAppClientId>', '<StoreLocation>', '<StoreName>', '<SubjectName>'
    When I validate the configuration
    Then the validation should fail with 'ClientIdAndCertificate configuration must provide AzureAppTenantId, AzureAdAppClientId, and an AzureAppClientCertificate with a StoreLocation, StoreName, and SubjectName'

    Examples:
    | IdentitySourceType     | AzureAppTenantId                     | AzureAdAppClientId                   | StoreLocation | StoreName | SubjectName |
    |                        |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   | My        | CN=MyCert   |
    |                        | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      | CurrentUser   | My        | CN=MyCert   |
    |                        | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 |               | My        | CN=MyCert   |
    |                        | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   |           | CN=MyCert   |
    |                        | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   | My        |             |
    | ClientIdAndCertificate |                                      | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   | My        | CN=MyCert   |
    | ClientIdAndCertificate | b39db674-9ba1-4343-8d4e-004675b5d7a8 |                                      | CurrentUser   | My        | CN=MyCert   |
    | ClientIdAndCertificate | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 |               | My        | CN=MyCert   |
    | ClientIdAndCertificate | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   |           | CN=MyCert   |
    | ClientIdAndCertificate | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 | CurrentUser   | My        |             |
    | ClientIdAndCertificate | b39db674-9ba1-4343-8d4e-004675b5d7a8 | 831c7dcb-516a-4e6b-9b74-347264c67397 |               |           |             |

Scenario: Bad ClientIdAndCertificate with null AzureAdAppClientCertificate
    Given configuration of
        """
        {
          "ClientIdentity": { "IdentitySourceType": "ClientIdAndCertificate" }
        }
        """
    When I validate the configuration
    Then the validation should fail with 'ClientIdAndCertificate configuration must provide AzureAppTenantId, AzureAdAppClientId, and an AzureAppClientCertificate with a StoreLocation, StoreName, and SubjectName'

Scenario: Both ClientIdAndSecret with AzureAdAppClientSecretPlainText and ClientIdAndCertificate properties present
    Given a ClientIdAndCertificate configuration with '', 'b39db674-9ba1-4343-8d4e-004675b5d7a8', '831c7dcb-516a-4e6b-9b74-347264c67397', 'CurrentUser', 'My', 'CN=MyCert'
    And the AzureAdAppClientSecretPlainText is 'secret'
    When I validate the configuration
    Then the validation should fail with 'identity type is ambiguous because the properties set are for ClientIdAndSecret, ClientIdAndCertificate'

Scenario: Both ClientIdAndSecret with AzureAdAppClientSecretInKeyVault and ClientIdAndCertificate properties present
    Given a ClientIdAndCertificate configuration with '', 'b39db674-9ba1-4343-8d4e-004675b5d7a8', '831c7dcb-516a-4e6b-9b74-347264c67397', 'CurrentUser', 'My', 'CN=MyCert'
    And the AzureAdAppClientSecretInKeyVault is set
    When I validate the configuration
    Then the validation should fail with 'identity type is ambiguous because the properties set are for ClientIdAndSecret, ClientIdAndCertificate'

Scenario: Both ClientIdAndSecret with AzureAdAppClientSecretPlainText and AzureAdAppClientSecretInKeyVault and ClientIdAndCertificate properties present
    Given a ClientIdAndCertificate configuration with '', 'b39db674-9ba1-4343-8d4e-004675b5d7a8', '831c7dcb-516a-4e6b-9b74-347264c67397', 'CurrentUser', 'My', 'CN=MyCert'
    And the AzureAdAppClientSecretPlainText is 'secret'
    And the AzureAdAppClientSecretInKeyVault is set
    When I validate the configuration
    Then the validation should fail with 'identity type is ambiguous because the properties set are for ClientIdAndSecret, ClientIdAndCertificate'

Scenario Outline: IdentitySourceType conflicts apparent ClientIdAndSecret with plaintext secret
    Given configuration of
        """
        {
          "ClientIdentity": {
            "IdentitySourceType": "<IdentitySourceType>",
            "AzureAppTenantId": "b39db674-9ba1-4343-8d4e-004675b5d7a8",
            "AzureAdAppClientId": "831c7dcb-516a-4e6b-9b74-347264c67397",
            "AzureAdAppClientSecretPlainText": "s3cret!"
          }
        }
        """
    When I validate the configuration
    Then the validation should fail with 'identity type is ambiguous because the IdentitySourceType is <IdentitySourceType> but the properties set are for ClientIdAndSecret'

    Examples:
    | IdentitySourceType                  |
    | SystemAssignedManaged               |
    | UserAssignedManaged                 |
    | AzureCli                            |
    | VisualStudio                        |
    | AzureIdentityDefaultAzureCredential |
    | ClientIdAndCertificate              |

Scenario Outline: IdentitySourceType conflicts apparent ClientIdAndSecret with secret in key vault
    Given configuration of
        """
        {
          "ClientIdentity": {
            "IdentitySourceType": "<IdentitySourceType>",
            "AzureAppTenantId": "b39db674-9ba1-4343-8d4e-004675b5d7a8",
            "AzureAdAppClientId": "831c7dcb-516a-4e6b-9b74-347264c67397",
            "AzureAdAppClientSecretInKeyVault": {
              "VaultName": "myvault",
              "SecretName": "MyAzureAdAppClientSecret" 
            }
          }
        }
        """
    When I validate the configuration
    Then the validation should fail with 'identity type is ambiguous because the IdentitySourceType is <IdentitySourceType> but the properties set are for ClientIdAndSecret'

    Examples:
    | IdentitySourceType                  |
    | SystemAssignedManaged               |
    | UserAssignedManaged                 |
    | AzureCli                            |
    | VisualStudio                        |
    | AzureIdentityDefaultAzureCredential |
    | ClientIdAndCertificate              |

Scenario Outline: IdentitySourceType conflicts apparent ClientIdAndCertificate
    Given a ClientIdAndCertificate configuration with '<IdentitySourceType>', 'b39db674-9ba1-4343-8d4e-004675b5d7a8', '831c7dcb-516a-4e6b-9b74-347264c67397', 'CurrentUser', 'My', 'CN=MyCert'
    When I validate the configuration
    Then the validation should fail with 'identity type is ambiguous because the IdentitySourceType is <IdentitySourceType> but the properties set are for ClientIdAndCertificate'

    Examples:
    | IdentitySourceType                  |
    | SystemAssignedManaged               |
    | UserAssignedManaged                 |
    | AzureCli                            |
    | VisualStudio                        |
    | AzureIdentityDefaultAzureCredential |
    | ClientIdAndSecret                   |

Scenario Outline: Good SystemAssignedManaged
    Given configuration of
        """
        {
          "ClientIdentity": { "IdentitySourceType": "<IdentitySourceType>" }
        }
        """
    When I validate the configuration
    Then the validation should pass
    And the validated type should be 'SystemAssignedManaged'

    Examples:
    | IdentitySourceType    |
    | SystemAssignedManaged |
    # Legacy, but still supported
    | Managed               |

Scenario: Good UserAssignedManaged
    Given configuration of
        """
        {
          "ClientIdentity": {
              "IdentitySourceType": "UserAssignedManaged",
              "ManagedIdentityClientId": "baadf00d-0123-4567-89ab-abbadabbad00"
          }
        }
        """
    When I validate the configuration
    Then the validation should pass
    And the validated type should be 'UserAssignedManaged'

Scenario: Good implicit UserAssignedManaged
    Given configuration of
        """
        {
          "ClientIdentity": {
              "ManagedIdentityClientId": "baadf00d-0123-4567-89ab-abbadabbad00"
          }
        }
        """
    When I validate the configuration
    Then the validation should pass
    And the validated type should be 'UserAssignedManaged'

Scenario: UserAssignedManaged with missing ManagedIdentityClientId
    Given configuration of
        """
        {
          "ClientIdentity": {
              "IdentitySourceType": "UserAssignedManaged"
          }
        }
        """
    When I validate the configuration
    Then the validation should fail with 'UserAssignedManaged configuration must provide ManagedIdentityClientId'
