// <copyright file="LegacyAuthConnectionStringsSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure
{
    using System;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure;

    using global::Azure.Core;
    using global::Azure.Identity;

    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework;
    using NUnit.Framework.Internal;

    using TechTalk.SpecFlow;

    [Binding]
    public class LegacyAuthConnectionStringsSteps
    {
        private readonly LegacyAuthConnectionStringsFeature.Modes mode;
        private TokenCredential? credential;

        public LegacyAuthConnectionStringsSteps()
        {
            var feature = (LegacyAuthConnectionStringsFeature)TestExecutionContext.CurrentContext.TestObject;
            this.mode = feature.Mode;
        }

        [When("I create a TokenCredential with the connection string '(.*)'")]
        public async Task WhenICreateATokenCredentialWithTheConnectionString(string connectionString)
        {
            if (this.mode == LegacyAuthConnectionStringsFeature.Modes.Direct)
            {
                this.credential = LegacyAzureServiceTokenProviderConnectionString.ToTokenCredential(connectionString);
            }
            else
            {
                var services = new ServiceCollection();
                services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(connectionString);
                ServiceProvider sp = services.BuildServiceProvider();
                IServiceIdentityAzureTokenCredentialSource source = sp.GetRequiredService<IServiceIdentityAzureTokenCredentialSource>();
                this.credential = await source.GetAccessTokenAsync().ConfigureAwait(false);
            }
        }

        [Then("the TokenCredential should be of type '(.*)'")]
        public void ThenTheTokenCredentialShouldBeOfType(string credentialType)
        {
            string fullCredentialTypeName = $"Azure.Identity.{credentialType}";
            Type? expectedBaseType = typeof(DefaultAzureCredential).Assembly.GetType(fullCredentialTypeName);
            if (expectedBaseType is null)
            {
                Assert.Fail($"Did not find type {fullCredentialTypeName}");
            }
            else
            {
                Assert.IsInstanceOf(expectedBaseType, this.credential);
            }
        }

        [Then("the ClientSecretCredential tenantId should be '(.*)'")]
        public void ThenTheClientSecretCredentialTenantIdShouldBe(string tenantId)
        {
            Assert.AreEqual(
                tenantId,
                ((LegacyAzureServiceTokenProviderConnectionString.TestableClientSecretCredential)this.credential!).TenantId);
        }

        [Then("the ClientSecretCredential appId should be '(.*)'")]
        public void ThenTheClientSecretCredentialAppIdShouldBe(string clientId)
        {
            Assert.AreEqual(
                clientId,
                ((LegacyAzureServiceTokenProviderConnectionString.TestableClientSecretCredential)this.credential!).ClientId);
        }

        [Then("the ClientSecretCredential clientSecret should be '(.*)'")]
        public void ThenTheClientSecretCredentialClientSecretShouldBe(string clientSecret)
        {
            Assert.AreEqual(
                clientSecret,
                ((LegacyAzureServiceTokenProviderConnectionString.TestableClientSecretCredential)this.credential!).ClientSecret);
        }
    }
}