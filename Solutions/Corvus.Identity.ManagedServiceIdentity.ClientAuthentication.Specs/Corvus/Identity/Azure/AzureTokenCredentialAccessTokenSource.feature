﻿Feature: AzureTokenCredentialAccessTokenSource
    As an application developer
    Using a TokenCredential (as defined in Azure.Core) to obtain tokens
    I want to be able to use IAccessTokenSource to obtain a raw access token

Scenario: Successful token acquisition with just scopes
    Given the AccessTokenRequest scope is 'https://management.core.windows.net/.default'
    When IAccessTokenSource.GetAccessTokenAsync is called
    And the underlying TokenCredential returns a successful result
    Then the scope should have been passed on to TokenCredential.GetTokenAsync
    And the Claims passed to TokenCredential.GetTokenAsync should be null
    And the TenantId passed to TokenCredential.GetTokenAsync should be null
    And the ParentRequestId passed to TokenCredential.GetTokenAsync should be null
    And the AccessToken returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync
    And the ExpiresOn returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync

Scenario: Successful token acquisition with scopes and claims
    Given the AccessTokenRequest scope is 'https://management.core.windows.net/.default'
    And the AccessTokenRequest has additional claims of 'claim1 claim2'
    When IAccessTokenSource.GetAccessTokenAsync is called
    And the underlying TokenCredential returns a successful result
    Then the scope should have been passed on to TokenCredential.GetTokenAsync
    And the Claims should have been passed on to TokenCredential.GetTokenAsync
    And the TenantId passed to TokenCredential.GetTokenAsync should be null
    And the ParentRequestId passed to TokenCredential.GetTokenAsync should be null
    And the AccessToken returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync
    And the ExpiresOn returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync

Scenario: Successful token acquisition with scopes and authority id
    Given the AccessTokenRequest scope is 'https://management.core.windows.net/.default'
    And the AccessTokenRequest has an authority id 'mytenant'
    When IAccessTokenSource.GetAccessTokenAsync is called
    And the underlying TokenCredential returns a successful result
    Then the scope should have been passed on to TokenCredential.GetTokenAsync
    And the Claims passed to TokenCredential.GetTokenAsync should be null
    And the AuthorityId should have been passed on to TokenCredential.GetTokenAsync as the TenantId
    And the ParentRequestId passed to TokenCredential.GetTokenAsync should be null
    And the AccessToken returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync
    And the ExpiresOn returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync

Scenario: Successful token acquisition with scopes, claims, and authority id
    Given the AccessTokenRequest scope is 'https://management.core.windows.net/.default'
    And the AccessTokenRequest has additional claims of 'claim1 claim2'
    And the AccessTokenRequest has an authority id 'mytenant'
    When IAccessTokenSource.GetAccessTokenAsync is called
    And the underlying TokenCredential returns a successful result
    Then the scope should have been passed on to TokenCredential.GetTokenAsync
    And the Claims should have been passed on to TokenCredential.GetTokenAsync
    And the AuthorityId should have been passed on to TokenCredential.GetTokenAsync as the TenantId
    And the ParentRequestId passed to TokenCredential.GetTokenAsync should be null
    And the AccessToken returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync
    And the ExpiresOn returned by IAccessTokenSource.GetAccessTokenAsync should be the same as was returned by TokenCredential.GetTokenAsync


# The various implementations of TokenCredential supplied by Azure.Identity throw either a
# CredentialUnavailableException (mostly the AzureCliCredential and VisualStudioCredential,
# and the ManagedIdentityCredential if the managed identity endpoint is not available) or
# AuthenticationFailedException (mostly ClientSecretCredential, but also some other
# scenarios in cases where you're just trying to use something you're not allowed to use).

Scenario: Token acquisition fails with CredentialUnavailableException
    Given the AccessTokenRequest scope is 'https://management.core.windows.net/.default'
    When IAccessTokenSource.GetAccessTokenAsync is called
    And the underlying TokenCredential throws a 'CredentialUnavailableException'
    Then IAccessTokenSource.GetAccessTokenAsync should have thrown an AccessTokenNotIssuedException
    And the AccessTokenNotIssuedException.InnerException should be the exception thrown by the underlying TokenCredential

Scenario: Token acquisition fails with AuthenticationFailedException
    Given the AccessTokenRequest scope is 'https://management.core.windows.net/.default'
    When IAccessTokenSource.GetAccessTokenAsync is called
    And the underlying TokenCredential throws a 'AuthenticationFailedException'
    Then IAccessTokenSource.GetAccessTokenAsync should have thrown an AccessTokenNotIssuedException
    And the AccessTokenNotIssuedException.InnerException should be the exception thrown by the underlying TokenCredential