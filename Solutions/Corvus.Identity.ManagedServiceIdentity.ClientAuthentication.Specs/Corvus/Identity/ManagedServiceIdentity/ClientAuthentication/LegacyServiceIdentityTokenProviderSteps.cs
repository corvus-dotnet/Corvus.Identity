// <copyright file="LegacyServiceIdentityTokenProviderSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System.Threading.Tasks;

    using Moq;

    using TechTalk.SpecFlow;

    [Binding]
    public class LegacyServiceIdentityTokenProviderSteps
    {
        private readonly Mock<IServiceIdentityTokenSource> underlyingTokenSource = new ();
        private readonly TaskCompletionSource<string?> underlyingGetAccessTokenResultSource = new ();
        private readonly ServiceIdentityTokenProviderCommonSteps common;

        public LegacyServiceIdentityTokenProviderSteps(
            ServiceIdentityTokenProviderCommonSteps common)
        {
            this.underlyingTokenSource
                .Setup(s => s.GetAccessToken(It.IsAny<string>()))
                .Returns(this.underlyingGetAccessTokenResultSource.Task);
            this.common = common;
        }

        [Given("I created a ServiceIdentityTokenProvider for the resource '(.*)'")]
        public void GivenICreatedAServiceIdentityTokenProviderForTheResource(string resource)
        {
            this.common.Provider = new ServiceIdentityTokenProvider(this.underlyingTokenSource.Object, resource);
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
    }
}
