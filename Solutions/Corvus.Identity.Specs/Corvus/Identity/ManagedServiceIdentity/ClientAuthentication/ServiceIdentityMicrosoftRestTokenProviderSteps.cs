// <copyright file="ServiceIdentityMicrosoftRestTokenProviderSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication;
    using Corvus.Identity.ClientAuthentication.MicrosoftRest;

    using Idg.AsyncTest.TaskExtensions;

    using NUnit.Framework;

    using Reqnroll;

    [Binding]
    public class ServiceIdentityMicrosoftRestTokenProviderSteps
    {
        private readonly FakeTokenSource source;
        private readonly TokenProviderSteps common;
        private Exception? exceptionFromUnderlyingSource;

        public ServiceIdentityMicrosoftRestTokenProviderSteps(TokenProviderSteps common)
        {
            this.source = new FakeTokenSource();
            this.common = common;
        }

        [Given("I created a ServiceIdentityMicrosoftRestTokenProvider for the scope '(.*)'")]
        public void GivenICreatedAServiceIdentityMicrosoftRestTokenProviderForTheResource(string scope)
        {
            this.common.Provider = new ServiceIdentityMicrosoftRestTokenProvider(this.source, scope);
        }

        [Then("the scope passed to the wrapped IServiceIdentityAccessTokenSource implementation's GetAccessToken should be '(.*)'")]
        public void ThenTheScopePassedToTheWrappedIServiceIdentityAccessTokenSourceImplementationApiWhatever_Default(string scope)
        {
            Assert.That(this.source.TokenRequest.Scopes.Single(), Is.EqualTo(scope));
        }

        [When("the task returned by the wrapped IServiceIdentityAccessTokenSource implementation's GetAccessToken method returns '(.*)'")]
        public void WhenTheTaskReturnedByTheWrappedIServiceIdentityAccessTokenSourceImplementationMyToken(string token)
        {
            this.source.UnderlyingGetAccessTokenResultSource.SetResult(new AccessTokenDetail(token, DateTimeOffset.UtcNow.AddHours(1)));
        }

        [When("the task returned by the wrapped IServiceIdentityAccessTokenSource implementation's GetAccessToken method fails")]
        public void WhenTheTaskReturnedByTheWrappedIServiceIdentityAccessTokenSourceImplementationFails()
        {
            this.exceptionFromUnderlyingSource = new AccessTokenNotIssuedException(new Exception());
            this.source.UnderlyingGetAccessTokenResultSource.SetException(this.exceptionFromUnderlyingSource);
        }

        [Then(@"the task returned from ITokenProvider\.GetAuthenticationHeaderAsync should fail with the exception produced by IServiceIdentityAccessTokenSource\.GetAccessToken")]
        public async Task ThenTheTaskReturnedFromITokenProvider_GetAuthenticationHeaderAsyncShouldFailWithTheExceptionProducedByIServiceIdentityAccessTokenSource_GetAccessToken()
        {
            await this.common.Result!.WhenCompleteIgnoringErrors().WithTimeout().ConfigureAwait(false);
            Assert.That(this.common.Result!.Status, Is.EqualTo(TaskStatus.Faulted));
            Assert.That(this.common.Result.Exception!.InnerException, Is.EqualTo(this.exceptionFromUnderlyingSource));
        }

        // Not using MoQ due to ValueTask funkiness
        private class FakeTokenSource : IServiceIdentityAccessTokenSource
        {
            public TaskCompletionSource<AccessTokenDetail> UnderlyingGetAccessTokenResultSource { get; }
                = new TaskCompletionSource<AccessTokenDetail>();

            public AccessTokenRequest TokenRequest { get; private set; }

            public async ValueTask<AccessTokenDetail> GetAccessTokenAsync(
                AccessTokenRequest requiredTokenCharacteristics,
                CancellationToken cancellationToken)
            {
                this.TokenRequest = requiredTokenCharacteristics;
                return await this.UnderlyingGetAccessTokenResultSource.Task.ConfigureAwait(false);
            }

            public ValueTask<AccessTokenDetail> GetReplacementForFailedAccessTokenAsync(
                AccessTokenRequest requiredTokenCharacteristics,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }
    }
}