Feature: ServiceIdentityTokenProvider
    In order to be able to use any Corvus IServiceIdentityTokenSource with any Microsoft.Rest-based client (e.g., most Azure client libraries ca 2020/01)
    As a developer
    I want to be able to adapt any Corvus IServiceIdentityTokenSource implementation to the Microsoft.Rest.ITokenProvider interface

Scenario: Start token acquisition
    Given I created a ServiceIdentityTokenProvider for the resource 'MyResource'
    When I invoke ITokenProvider.GetAuthenticationHeaderAsync
    Then the resource passed to the wrapped IServiceIdentityTokenSource implementation's GetAccessToken should be 'MyResource'

Scenario: Successful token acquisition
    Given I created a ServiceIdentityTokenProvider for the resource 'MyResource'
    And I have invoked ITokenProvider.GetAuthenticationHeaderAsync
    When the task returned by the wrapped IServiceIdentityTokenSource implementation's GetAccessToken method returns 'MyToken'
    Then the task returned from ITokenProvider.GetAuthenticationHeaderAsync should complete successfully
    And the AuthenticationHeaderValue produced by ITokenProvider.GetAuthenticationHeaderAsync should have a Scheme of 'Bearer'
    And the AuthenticationHeaderValue produced by ITokenProvider.GetAuthenticationHeaderAsync should have a Parameter of 'MyToken'

Scenario: Unsuccessful token acquisition
    Given I created a ServiceIdentityTokenProvider for the resource 'MyResource'
    And I have invoked ITokenProvider.GetAuthenticationHeaderAsync
    When the task returned by the wrapped IServiceIdentityTokenSource implementation's GetAccessToken method returns null
    Then the task returned from ITokenProvider.GetAuthenticationHeaderAsync should complete successfully
    And the result produced by ITokenProvider.GetAuthenticationHeaderAsync should be null