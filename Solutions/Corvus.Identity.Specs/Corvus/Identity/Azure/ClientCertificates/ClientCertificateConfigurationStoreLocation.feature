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
