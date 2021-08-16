Feature: ServiceIdentityMicrosoftRestTokenProvider
    In order to be able to use any Corvus IServiceIdentityAccessTokenSource with any Microsoft.Rest-based client (e.g., most Azure SDK client libraries that haven't yet been reached by the revamp that started in 2019)
    As a developer
    I want to be able to adapt any Corvus IServiceIdentityAccessTokenSource implementation to the Microsoft.Rest.ITokenProvider interface

Scenario: Start token acquisition
    Given I created a ServiceIdentityMicrosoftRestTokenProvider for the scope 'api://whatever/.default'
    When I invoke ITokenProvider.GetAuthenticationHeaderAsync
    Then the scope passed to the wrapped IServiceIdentityAccessTokenSource implementation's GetAccessToken should be 'api://whatever/.default'

Scenario: Successful token acquisition
    Given I created a ServiceIdentityMicrosoftRestTokenProvider for the scope 'api://whatever/.default'
    And I have invoked ITokenProvider.GetAuthenticationHeaderAsync
    When the task returned by the wrapped IServiceIdentityAccessTokenSource implementation's GetAccessToken method returns 'MyToken'
    Then the task returned from ITokenProvider.GetAuthenticationHeaderAsync should complete successfully
    And the AuthenticationHeaderValue produced by ITokenProvider.GetAuthenticationHeaderAsync should have a Scheme of 'Bearer'
    And the AuthenticationHeaderValue produced by ITokenProvider.GetAuthenticationHeaderAsync should have a Parameter of 'MyToken'

Scenario: Unsuccessful token acquisition
    Given I created a ServiceIdentityMicrosoftRestTokenProvider for the scope 'api://whatever/.default'
    And I have invoked ITokenProvider.GetAuthenticationHeaderAsync
    When the task returned by the wrapped IServiceIdentityAccessTokenSource implementation's GetAccessToken method fails
    Then the task returned from ITokenProvider.GetAuthenticationHeaderAsync should fail with the exception produced by IServiceIdentityAccessTokenSource.GetAccessToken