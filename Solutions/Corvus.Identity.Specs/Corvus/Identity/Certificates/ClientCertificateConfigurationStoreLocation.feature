Feature: ClientCertificateConfigurationStoreLocation
    As a developer or administrator configuring an application
    I want to receive useful errors when a ClientCertificateConfiguration is incorrect
    So that I can understand and resolve the situation quickly

Scenario Outline: Store location
    When client certificate configuration is
        """
        {
            "ClientCertificate": { "StoreLocation": "<StoreLocation>" }
        }
        """
    Then the certificate store location is '<StoreLocation>'

    Examples:
    | StoreLocation |
    | CurrentUser   |
    | LocalMachine  |

# Subject name identifies a certificate we do not have.

Scenario: Certificate with subject name not found
    When client certificate configuration is
        """
        {
            "ClientCertificate": { "StoreLocation": "CurrentUser", "SubjectName": "NoCertificateWithThisSubjectName" }
        }
        """
    And we attempt to get the configured certificate



# Subject name identifies a certificate we do have.
