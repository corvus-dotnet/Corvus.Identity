// <copyright file="ServiceIdentityMicrosoftRestTokenProviderSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication;
    using Corvus.Identity.MicrosoftRest;

    using Idg.AsyncTest.TaskExtensions;

    using Moq;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class ServiceIdentityMicrosoftRestTokenProviderSteps
    {
        private readonly FakeTokenSource source;
        private readonly ServiceIdentityTokenProviderCommonSteps common;
        private Exception? exceptionFromUnderlyingSource;

        public ServiceIdentityMicrosoftRestTokenProviderSteps(
            ServiceIdentityTokenProviderCommonSteps common)
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
            Assert.AreEqual(scope, this.source.TokenRequest.Scopes.Single());
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
            await this.common.Result.WhenCompleteIgnoringErrors().WithTimeout().ConfigureAwait(false);
            Assert.AreEqual(TaskStatus.Faulted, this.common.Result!.Status);
            Assert.AreSame(this.exceptionFromUnderlyingSource, this.common.Result.Exception!.InnerException);
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
        }
    }
}