namespace Corvus.Identity.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication;
    using Corvus.Identity.ClientAuthentication.Azure;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;

    using global::Azure.Core;
    using global::Azure.Identity;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class AzureTokenCredentialAccessTokenSourceSteps
    {
        private readonly TaskCompletionSource<AccessToken> taskForResultFromUnderlyingCredential = new ();
        private readonly List<TestTokenCredential> replacementCredentials = new ();
        private readonly List<ClientIdentityConfiguration> invalidatedConfigurations = new ();
        private readonly ClientIdentityConfiguration configuration;
        private readonly IAccessTokenSourceFromDynamicConfiguration sourceFromDynamicConfiguration;
        private readonly Task<IAccessTokenSource> accessTokenSource;
#nullable disable annotations
        private string[] scopes;
        private string claims;
        private string authorityId;
        private Task<AccessTokenDetail> accessTokenDetailReturnedTask;
#nullable restore annotations
        private TokenRequestContext requestContextPassedToUnderlyingCredential;
        private AccessToken resultFromUnderlyingCredential;

        public AzureTokenCredentialAccessTokenSourceSteps()
        {
            AzureTokenCredentialSource tokenCredentialSource = new (
                new TestTokenCredential(this),
                _ => this.ReplacementTokenRequested());
            this.sourceFromDynamicConfiguration = new AccessTokenSourceFromDynamicConfiguration(
                new TestTokenCredentialSourceFromConfig(this, tokenCredentialSource));

            this.configuration = new ClientIdentityConfiguration
            {
                IdentitySourceType = ClientIdentitySourceTypes.Managed,
            };
            this.accessTokenSource = this.sourceFromDynamicConfiguration
                .AccessTokenSourceForConfigurationAsync(this.configuration)
                .AsTask();
        }

        [Given("the AccessTokenRequest scope is '(.*)'")]
        public void GivenTheAccessTokenRequestScopeIs(string scope)
        {
            this.scopes = new[] { scope };
        }

        [Given("the AccessTokenRequest has additional claims of '(.*)'")]
        public void GivenTheAccessTokenRequestHasAdditionalClaimsOf(string claims)
        {
            this.claims = claims;
        }

        [Given("the AccessTokenRequest has an authority id '(.*)'")]
        public void GivenTheAccessTokenRequestHasAnAuthorityId(string authorityId)
        {
            this.authorityId = authorityId;
        }

        [Given(@"IAccessTokenSource\.GetAccessTokenAsync is called")]
        [When(@"IAccessTokenSource\.GetAccessTokenAsync is called")]
        public async Task WhenIAccessTokenSource_GetAccessTokenAsyncIsCalledAsync()
        {
            this.accessTokenDetailReturnedTask = (await this.accessTokenSource.ConfigureAwait(false)).GetAccessTokenAsync(
                new AccessTokenRequest(this.scopes, this.claims, this.authorityId),
                CancellationToken.None).AsTask();
        }

        [When(@"IAccessTokenSource\.GetReplacementForFailedAccessTokenAsync is called")]
        public async Task WhenIAccessTokenSource_GetReplacementForFailedAccessTokenAsyncIsCalledAsync()
        {
            this.accessTokenDetailReturnedTask = (await this.accessTokenSource.ConfigureAwait(false)).GetReplacementForFailedAccessTokenAsync(
                new AccessTokenRequest(this.scopes, this.claims, this.authorityId),
                CancellationToken.None).AsTask();
        }

        [When(@"IAccessTokenSourceFromDynamicConfiguration\.InvalidateFailedAccessToken is called")]
        public void WhenIAccessTokenSourceFromDynamicConfiguration_InvalidateFailedAccessTokenIsCalled()
        {
            this.sourceFromDynamicConfiguration.InvalidateFailedAccessToken(this.configuration);
        }

        [When("the underlying TokenCredential returns a successful result")]
        public void WhenTheUnderlyingTokenCredentialReturnsASuccessfulResult()
        {
            byte[] r = new byte[10];
            new Random().NextBytes(r);
            this.resultFromUnderlyingCredential = new AccessToken(
                Convert.ToBase64String(r),
                DateTimeOffset.UtcNow.AddSeconds(20));
            this.taskForResultFromUnderlyingCredential.SetResult(this.resultFromUnderlyingCredential);
        }

        [When("the underlying TokenCredential throws a '(.*)'")]
        public void WhenTheUnderlyingTokenCredentialThrowsA(string exceptionType)
        {
            this.taskForResultFromUnderlyingCredential.SetException(
                exceptionType switch
                {
                    "CredentialUnavailableException" => new CredentialUnavailableException("That's not there"),
                    "AuthenticationFailedException" => new AuthenticationFailedException("That didn't work"),

                    _ => new InvalidOperationException($"Bad exceptionType in test: {exceptionType}"),
                });
        }

        [Then("the IAzureTokenCredentialSource should have been asked to replace the credential")]
        public void ThenTheIAzureTokenCredentialSourceShouldHaveBeenAskedToReplaceTheCredential()
        {
            Assert.AreEqual(1, this.replacementCredentials.Count);
        }

        [Then("the IAzureTokenCredentialSourceFromDynamicConfiguration should have been asked to invalidate the credential")]
        public void ThenTheIAzureTokenCredentialSourceFromDynamicConfigurationShouldHaveBeenAskedToInvalidateTheCredential()
        {
            Assert.AreSame(this.configuration, this.invalidatedConfigurations.Single());
        }

        [Then(@"the scope should have been passed on to TokenCredential\.GetTokenAsync")]
        public void ThenTheScopeShouldHaveBeenPassedOnToTokenCredential_GetToken()
        {
            Assert.AreSame(this.scopes, this.requestContextPassedToUnderlyingCredential.Scopes);
        }

        [Then(@"the Claims passed to TokenCredential\.GetTokenAsync should be null")]
        public void ThenTheClaimsPassedToTokenCredential_GetTokenAsyncShouldBeNull()
        {
            Assert.IsNull(this.requestContextPassedToUnderlyingCredential.Claims);
        }

        [Then(@"the TenantId passed to TokenCredential\.GetTokenAsync should be null")]
        public void ThenTheTenantIdPassedToTokenCredential_GetTokenAsyncShouldBeNull()
        {
            Assert.IsNull(this.requestContextPassedToUnderlyingCredential.TenantId);
        }

        [Then(@"the ParentRequestId passed to TokenCredential\.GetTokenAsync should be null")]
        public void ThenTheParentRequestIdPassedToTokenCredential_GetTokenAsyncShouldBeNull()
        {
            Assert.IsNull(this.requestContextPassedToUnderlyingCredential.ParentRequestId);
        }

        [Then(@"the AccessToken returned by IAccessTokenSource\.GetAccessTokenAsync should be the same as was returned by TokenCredential\.GetTokenAsync")]
        public async Task ThenTheAccessTokenReturnedByIAccessTokenSource_GetAccessTokenAsyncShouldBeTheSameAsWasReturnedByTokenCredential_GetTokenAsync()
        {
            Assert.AreEqual(this.resultFromUnderlyingCredential.Token, (await this.accessTokenDetailReturnedTask.ConfigureAwait(false)).AccessToken);
        }

        [Then(@"the ExpiresOn returned by IAccessTokenSource\.GetAccessTokenAsync should about three minutes into the future")]
        public async Task ThenTheExpiresOnReturnedByIAccessTokenSource_GetAccessTokenAsyncShouldBeTheSameAsWasReturnedByTokenCredential_GetTokenAsync()
        {
            // Because of the limited access to the Azure.Core token caching functionality, it's
            // not possible for us to determine the real token expiration time. (We'd have to
            // write our own cache logic to do that.) We know that the token cache we are
            // (indirectly) using refreshes tokens 5 minutes before they expire, so we just
            // report a fixed expiration time a little into the future.
            DateTimeOffset reportedExpiration = (await this.accessTokenDetailReturnedTask.ConfigureAwait(false)).ExpiresOn;
            TimeSpan diff = DateTimeOffset.UtcNow.AddMinutes(3) - reportedExpiration;
            Assert.IsTrue(Math.Abs(diff.TotalSeconds) < 30);
        }

        [Then(@"the Claims should have been passed on to TokenCredential\.GetTokenAsync")]
        public void ThenTheClaimsShouldHaveBeenPassedOnToTokenCredential_GetToken()
        {
            Assert.AreSame(this.claims, this.requestContextPassedToUnderlyingCredential.Claims);
        }

        [Then(@"the AuthorityId should have been passed on to TokenCredential\.GetTokenAsync as the TenantId")]
        public void ThenTheAuthorityIdShouldHaveBeenPassedOnToTokenCredential_GetTokenAsyncAsTheTenantId()
        {
            Assert.AreSame(this.authorityId, this.requestContextPassedToUnderlyingCredential.TenantId);
        }

        [Then(@"IAccessTokenSource\.GetAccessTokenAsync should have thrown an AccessTokenNotIssuedException")]
        public async Task ThenIAccessTokenSource_GetAccessTokenAsyncShouldHaveThrownAnAccessTokenNotIssuedExceptionAsync()
        {
            try
            {
                await this.accessTokenDetailReturnedTask.ConfigureAwait(false);
                Assert.Fail("Exception should have been thrown");
            }
            catch (Exception x)
            {
                Assert.IsInstanceOf<AccessTokenNotIssuedException>(x);
            }
        }

        [Then(@"the AccessTokenNotIssuedException\.InnerException should be the exception thrown by the underlying TokenCredential")]
        public async Task ThenTheAccessTokenNotIssuedException_InnerExceptionShouldBeTheExceptionThrownByTheUnderlyingTokenCredentialAsync()
        {
            try
            {
                await this.accessTokenDetailReturnedTask.ConfigureAwait(false);
                Assert.Fail("Exception should have been thrown");
            }
            catch (Exception x)
            {
                Assert.AreSame(this.taskForResultFromUnderlyingCredential.Task.Exception!.InnerException, x.InnerException);
            }
        }

        private ValueTask<TokenCredential> ReplacementTokenRequested()
        {
            TestTokenCredential replacement = new (this);
            this.replacementCredentials.Add(replacement);
            return new ValueTask<TokenCredential>(replacement);
        }

        private class TestTokenCredentialSourceFromConfig : IAzureTokenCredentialSourceFromDynamicConfiguration
        {
            private readonly AzureTokenCredentialAccessTokenSourceSteps azureTokenCredentialAccessTokenSourceSteps;
            private readonly AzureTokenCredentialSource azureTokenCredentialSource;

            public TestTokenCredentialSourceFromConfig(
                AzureTokenCredentialAccessTokenSourceSteps azureTokenCredentialAccessTokenSourceSteps,
                AzureTokenCredentialSource azureTokenCredentialSource)
            {
                this.azureTokenCredentialAccessTokenSourceSteps = azureTokenCredentialAccessTokenSourceSteps;
                this.azureTokenCredentialSource = azureTokenCredentialSource;
            }

            public ValueTask<IAzureTokenCredentialSource> CredentialSourceForConfigurationAsync(
                ClientIdentityConfiguration configuration, CancellationToken cancellationToken = default)
            {
                return new ValueTask<IAzureTokenCredentialSource>(this.azureTokenCredentialSource);
            }

            public void InvalidateFailedAccessToken(ClientIdentityConfiguration configuration)
            {
                this.azureTokenCredentialAccessTokenSourceSteps.invalidatedConfigurations.Add(configuration);
            }
        }

        private class TestTokenCredential : TokenCredential
        {
            private readonly AzureTokenCredentialAccessTokenSourceSteps parent;

            public TestTokenCredential(AzureTokenCredentialAccessTokenSourceSteps azureTokenCredentialAccessTokenSourceSteps)
            {
                this.parent = azureTokenCredentialAccessTokenSourceSteps;
            }

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                throw new NotSupportedException($"The {nameof(AzureTokenCredentialAccessTokenSource)} should not be calling this.");
            }

            public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                this.parent.requestContextPassedToUnderlyingCredential = requestContext;
                return await this.parent.taskForResultFromUnderlyingCredential.Task.ConfigureAwait(false);
            }
        }
    }
}