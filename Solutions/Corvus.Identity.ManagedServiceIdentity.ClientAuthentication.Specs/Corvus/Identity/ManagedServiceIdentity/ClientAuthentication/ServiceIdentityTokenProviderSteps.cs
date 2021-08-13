// <copyright file="ServiceIdentityTokenProviderSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Idg.AsyncTest.TaskExtensions;

    using Microsoft.Rest;

    using Moq;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class ServiceIdentityTokenProviderSteps
    {
        private readonly Mock<IServiceIdentityTokenSource> underlyingTokenSource = new Mock<IServiceIdentityTokenSource>();
        private readonly TaskCompletionSource<string?> underlyingGetAccessTokenResultSource = new TaskCompletionSource<string?>();
        private ITokenProvider? provider;
        private Task<AuthenticationHeaderValue>? result;

        public ServiceIdentityTokenProviderSteps()
        {
            this.underlyingTokenSource
                .Setup(s => s.GetAccessToken(It.IsAny<string>()))
                .Returns(this.underlyingGetAccessTokenResultSource.Task);
        }

        [Given("I created a ServiceIdentityTokenProvider for the resource '(.*)'")]
        public void GivenICreatedAServiceIdentityTokenProviderForTheResource(string resource)
        {
            this.provider = new ServiceIdentityTokenProvider(this.underlyingTokenSource.Object, resource);
        }

        [Given(@"I have invoked ITokenProvider\.GetAuthenticationHeaderAsync")]
        [When(@"I invoke ITokenProvider\.GetAuthenticationHeaderAsync")]
        public void GivenIHaveInvokedITokenProvider_GetAuthenticationHeader()
        {
            if (this.provider == null)
            {
                throw new Exception("No provider has been set up.");
            }

            this.result = this.provider.GetAuthenticationHeaderAsync(CancellationToken.None);
        }

        [When("the task returned by the wrapped IServiceIdentityTokenSource implementation's GetAccessToken method returns '(.*)'")]
        public void WhenTheTaskReturnedByTheWrappedIServiceIdentityTokenSourceImplementationMyToken(string token)
        {
            this.underlyingGetAccessTokenResultSource.SetResult(token);
        }

        [When("the task returned by the wrapped IServiceIdentityTokenSource implementation's GetAccessToken method returns null")]
        public void WhenTheTaskReturnedByTheWrappedIServiceIdentityTokenSourceImplementationSGetAccessTokenMethodReturnsNull()
        {
            this.underlyingGetAccessTokenResultSource.SetResult(null);
        }

        [Then("the resource passed to the wrapped IServiceIdentityTokenSource implementation's GetAccessToken should be '(.*)'")]
        public void ThenTheResourcePassedToTheWrappedIServiceIdentityTokenSourceImplementationMyResource(string resource)
        {
            this.underlyingTokenSource.Verify(s => s.GetAccessToken(resource));
        }

        [Then(@"the task returned from ITokenProvider\.GetAuthenticationHeaderAsync should complete successfully")]
        public async Task ThenTheTaskReturnedFromITokenProvider_GetAuthenticationHeaderAsyncShouldCompleteSuccessfully()
        {
            await this.result.WithTimeout().ConfigureAwait(false);
        }

        [Then(@"the AuthenticationHeaderValue produced by ITokenProvider\.GetAuthenticationHeaderAsync should have a Scheme of '(.*)'")]
        public async Task ThenTheAuthenticationHeaderValueProducedByITokenProvider_GetAuthenticationHeaderAsyncShouldHaveASchemeOf(string scheme)
        {
            AuthenticationHeaderValue header = await this.result.WithTimeout().ConfigureAwait(false);
            Assert.AreEqual(scheme, header.Scheme);
        }

        [Then(@"the AuthenticationHeaderValue produced by ITokenProvider\.GetAuthenticationHeaderAsync should have a Parameter of '(.*)'")]
        public async Task ThenTheAuthenticationHeaderValueProducedByITokenProvider_GetAuthenticationHeaderAsyncShouldHaveAParameterOfAsync(string parameter)
        {
            AuthenticationHeaderValue header = await this.result.WithTimeout().ConfigureAwait(false);
            Assert.AreEqual(parameter, header.Parameter);
        }

        [Then(@"the result produced by ITokenProvider\.GetAuthenticationHeaderAsync should be null")]
        public async Task ThenTheResultProducedByITokenProvider_GetAuthenticationHeaderAsyncShouldBeNullAsync()
        {
            AuthenticationHeaderValue header = await this.result.WithTimeout().ConfigureAwait(false);
            Assert.IsNull(header);
        }
    }
}
