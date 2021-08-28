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
        // IServiceIdentityTokenSource and ServiceIdentityTokenProvider are obsolete, but we still
        // need to test them for as long as we ship them.
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly Mock<IServiceIdentityTokenSource> underlyingTokenSource = new ();
#pragma warning restore CS0618 // Type or member is obsolete
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
            // For as long as we continue to ship the obsolete ServiceIdentityTokenProvider, we
            // need to test it, so we need to supress these compiler warnings here.
#pragma warning disable CS0618 // Type or member is obsolete
            this.common.Provider = new ServiceIdentityTokenProvider(this.underlyingTokenSource.Object, resource);
#pragma warning restore CS0618 // Type or member is obsolete
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
