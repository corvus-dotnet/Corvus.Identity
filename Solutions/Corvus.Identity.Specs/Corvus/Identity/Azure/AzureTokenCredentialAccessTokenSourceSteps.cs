namespace Corvus.Identity.Azure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;

    using global::Azure.Core;
    using global::Azure.Identity;

    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class AzureTokenCredentialAccessTokenSourceSteps
    {
        private readonly TaskCompletionSource<AccessToken> taskForResultFromUnderlyingCredential = new ();
        private readonly IAccessTokenSource source;
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
            var services = new ServiceCollection();
            services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(new TestTokenCredential(this));
            ServiceProvider sp = services.BuildServiceProvider();
            this.source = sp.GetRequiredService<IServiceIdentityAccessTokenSource>();
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

        [When(@"IAccessTokenSource\.GetAccessTokenAsync is called")]
        public void WhenIAccessTokenSource_GetAccessTokenAsyncIsCalled()
        {
            this.accessTokenDetailReturnedTask = this.source.GetAccessTokenAsync(
                new AccessTokenRequest(this.scopes, this.claims, this.authorityId),
                CancellationToken.None).AsTask();
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

                    _ => new InvalidOperationException($"Bad exceptionType in test: {exceptionType}")
                });
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
            Assert.AreSame(this.resultFromUnderlyingCredential.Token, (await this.accessTokenDetailReturnedTask.ConfigureAwait(false)).AccessToken);
        }

        [Then(@"the ExpiresOn returned by IAccessTokenSource\.GetAccessTokenAsync should be the same as was returned by TokenCredential\.GetTokenAsync")]
        public async Task ThenTheExpiresOnReturnedByIAccessTokenSource_GetAccessTokenAsyncShouldBeTheSameAsWasReturnedByTokenCredential_GetTokenAsync()
        {
            Assert.AreEqual(this.resultFromUnderlyingCredential.ExpiresOn, (await this.accessTokenDetailReturnedTask.ConfigureAwait(false)).ExpiresOn);
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