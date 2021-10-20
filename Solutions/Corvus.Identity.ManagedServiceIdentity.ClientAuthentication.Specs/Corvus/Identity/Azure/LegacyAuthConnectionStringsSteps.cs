// <copyright file="LegacyAuthConnectionStringsSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure
{
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure;

    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework.Internal;

    using TechTalk.SpecFlow;

    [Binding]
    public class LegacyAuthConnectionStringsSteps
    {
        private readonly LegacyAuthConnectionStringsFeature.Modes mode;
        private readonly TokenCredentialBindings tokenCredentials;

        public LegacyAuthConnectionStringsSteps(
            TokenCredentialBindings tokenCredentials)
        {
            var feature = (LegacyAuthConnectionStringsFeature)TestExecutionContext.CurrentContext.TestObject;
            this.mode = feature.Mode;
            this.tokenCredentials = tokenCredentials;
        }

        [When("I create a TokenCredential with the connection string '(.*)'")]
        public async Task WhenICreateATokenCredentialWithTheConnectionString(string connectionString)
        {
            if (this.mode == LegacyAuthConnectionStringsFeature.Modes.Direct)
            {
                this.tokenCredentials.Credential = LegacyAzureServiceTokenProviderConnectionString.ToTokenCredential(connectionString);
            }
            else
            {
                var services = new ServiceCollection();
                services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(connectionString);
                ServiceProvider sp = services.BuildServiceProvider();
                IServiceIdentityAzureTokenCredentialSource source = sp.GetRequiredService<IServiceIdentityAzureTokenCredentialSource>();
                this.tokenCredentials.Credential = await source.GetTokenCredentialAsync().ConfigureAwait(false);
            }
        }
    }
}