// <copyright file="ClientIdentityConfigurationSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure;

    using global::Azure.Core;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using TechTalk.SpecFlow;

    [Binding]
    public class ClientIdentityConfigurationSteps : IDisposable
    {
        private readonly TokenCredentialBindings tokenCredentials;
        private readonly KeyVaultBindings keyVault;
        private IAzureTokenCredentialSourceFromDynamicConfiguration? credsFromConfig;
        private MemoryStream? configurationJson;
        private TestConfiguration? configuration;
        private ServiceProvider? serviceProvider;

        public ClientIdentityConfigurationSteps(
            TokenCredentialBindings tokenCredentials,
            KeyVaultBindings keyVault)
        {
            this.tokenCredentials = tokenCredentials;
            this.keyVault = keyVault;
        }

        [Given("configuration of")]
        public void GivenConfigurationOf(string configurationJson)
        {
            this.configurationJson = new MemoryStream(Encoding.UTF8.GetBytes(configurationJson));
        }

        [When("a TokenCredential is fetched for this configuration")]
        public async Task WhenATokenCredentialIsFetchedForThisConfiguration()
        {
            await this.WhenATokenCredentialIsFetchedForThisConfigurationAsCredential(null).ConfigureAwait(false);
        }

        [When("a TokenCredential is fetched for this configuration as credential '(.*)'")]
        public async Task WhenATokenCredentialIsFetchedForThisConfigurationAsCredential(string? credentialName)
        {
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .AddJsonStream(this.configurationJson)
                .Build();

            this.configuration = configRoot.Get<TestConfiguration>();

            IServiceCollection services = new ServiceCollection()
                .AddAzureTokenCredentialSourceFromDynamicConfiguration();
            this.keyVault.AddKeyVaultFactoryForTests(services);
            this.serviceProvider = services.BuildServiceProvider();

            this.credsFromConfig = this.serviceProvider.GetRequiredService<IAzureTokenCredentialSourceFromDynamicConfiguration>();

            IAzureTokenCredentialSource source =
                await this.credsFromConfig.CredentialSourceForConfigurationAsync(this.configuration!.ClientIdentity!).ConfigureAwait(false);
            TokenCredential credential = await source.GetTokenCredentialAsync().ConfigureAwait(false);

            this.tokenCredentials.SetNamedCredential(credentialName, credential);
        }

        ////[Given(@"the key vault '(.*)' returns '(.*)' for the secret named '(.*)'")]
        ////public void GivenTheKeyVaultReturnsForTheSecretNamed(string vaultName, string secretValue, string secretName)
        ////{
        ////    this.keyVault.A
        ////}

        ////[Then(@"a TokenCredential of type '(.*)' is returned")]
        ////public void ThenATokenCredentialOfTypeIsReturned(string typeSimpleName)
        ////{
        ////    string fullTypeName = $"Azure.Identity.{typeSimpleName}";
        ////    Type expectedType = typeof(DefaultAzureCredential).Assembly.GetType(fullTypeName)
        ////        ?? throw new InvalidOperationException($"Test specifies {typeSimpleName}; failed to find ${fullTypeName}");
        ////    Assert.IsInstanceOf(expectedType, this.credentials);
        ////}

        public void Dispose()
        {
            this.serviceProvider?.Dispose();
        }

        public class TestConfiguration
        {
            public ClientIdentityConfiguration? ClientIdentity { get; set; }
        }
    }
}