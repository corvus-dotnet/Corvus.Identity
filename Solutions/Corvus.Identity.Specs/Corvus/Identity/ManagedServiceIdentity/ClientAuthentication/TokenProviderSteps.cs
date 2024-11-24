// <copyright file="TokenProviderSteps.cs" company="Endjin Limited">
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
    using NUnit.Framework;
    using Reqnroll;

    [Binding]
    public class TokenProviderSteps
    {
        public ITokenProvider? Provider { get; set; }

        public Task<AuthenticationHeaderValue?>? Result { get; set; }

        [Given(@"I have invoked ITokenProvider\.GetAuthenticationHeaderAsync")]
        [When(@"I invoke ITokenProvider\.GetAuthenticationHeaderAsync")]
        public void GivenIHaveInvokedITokenProvider_GetAuthenticationHeader()
        {
            if (this.Provider == null)
            {
                throw new Exception("No provider has been set up.");
            }

            this.Result = this.Provider.GetAuthenticationHeaderAsync(CancellationToken.None);
        }

        [Then(@"the task returned from ITokenProvider\.GetAuthenticationHeaderAsync should complete successfully")]
        public async Task ThenTheTaskReturnedFromITokenProvider_GetAuthenticationHeaderAsyncShouldCompleteSuccessfully()
        {
            await this.Result!.WithTimeout().ConfigureAwait(false);
        }

        [Then(@"the AuthenticationHeaderValue produced by ITokenProvider\.GetAuthenticationHeaderAsync should have a Scheme of '(.*)'")]
        public async Task ThenTheAuthenticationHeaderValueProducedByITokenProvider_GetAuthenticationHeaderAsyncShouldHaveASchemeOf(string scheme)
        {
            AuthenticationHeaderValue? header = await this.Result!.WithTimeout().ConfigureAwait(false);
            Assert.That(header?.Scheme, Is.EqualTo(scheme));
        }

        [Then(@"the AuthenticationHeaderValue produced by ITokenProvider\.GetAuthenticationHeaderAsync should have a Parameter of '(.*)'")]
        public async Task ThenTheAuthenticationHeaderValueProducedByITokenProvider_GetAuthenticationHeaderAsyncShouldHaveAParameterOfAsync(string parameter)
        {
            AuthenticationHeaderValue? header = await this.Result!.WithTimeout().ConfigureAwait(false);
            Assert.That(header?.Parameter, Is.EqualTo(parameter));
        }
    }
}