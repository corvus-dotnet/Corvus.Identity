Feature: TokenCredentialSourceFromDynamicConfiguration AD App with client certificate
    As the person responsible for deploying and configuring an application that uses services secured by Azure Active Directory
    I need to be able to use a locally installed client certificate
    So that I can demonstrate to Azure AD that I am entitled to act as this service identity


Scenario: Service principle client ID in configuration and locally installed client certificate
    Given configuration of
        """
        {
          "ClientIdentity": {
            "AzureAdAppTenantId": "b39db674-9ba1-4343-8d4e-004675b5d7a8",
            "AzureAdAppClientId": "831c7dcb-516a-4e6b-9b74-347264c67397",
            "AzureAdAppClientCertificate": {
              "StoreLocation": "CurrentUser",
              "StoreName": "My",
              "SubjectName": "CorvusIdentityTestShouldBeDeleted" 
            }
          }
        }
        """
    And the 'My' store contains a trusted certificate with the subject name of 'CN=CorvusIdentityTestShouldBeDeleted'
    When a TokenCredential is fetched for this configuration as credential 'TargetCredentials'
    Then the TokenCredential 'TargetCredentials' should be of type 'ClientCertificateCredential'
	And the ClientCertificateCredential 'TargetCredentials' tenantId should be 'b39db674-9ba1-4343-8d4e-004675b5d7a8'
	And the ClientCertificateCredential 'TargetCredentials' appId should be '831c7dcb-516a-4e6b-9b74-347264c67397'
	And the ClientCertificateCredential 'TargetCredentials' should be using the certificate from the 'My' store with the subject name of 'CorvusIdentityTestShouldBeDeleted'
