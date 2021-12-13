// <copyright file="CachingKeyVaultClientSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure;

    using global::Azure;
    using global::Azure.Security.KeyVault.Secrets;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class CachingKeyVaultClientSteps : IDisposable
    {
        private readonly KeyVaultBindings keyVault;
        private readonly Dictionary<string, ClientIdentityConfiguration> identityConfigurations = new ();
        private readonly Dictionary<string, SecretClient> secretClients = new ();
        private readonly Dictionary<string, string> results = new ();
        private readonly ServiceProvider serviceProvider;
        private readonly ICachingKeyVaultSecretClientFactory secretClientFactory;

        public CachingKeyVaultClientSteps(
            KeyVaultBindings keyVault)
        {
            this.keyVault = keyVault;

            ServiceCollection services = new ();
            services.AddAzureTokenCredentialSourceFromDynamicConfiguration();
            this.keyVault.AddKeyVaultFactoryForTests(services);

            this.serviceProvider = services.BuildServiceProvider();
            this.secretClientFactory = this.serviceProvider.GetRequiredService<ICachingKeyVaultSecretClientFactory>();
        }

        public void Dispose()
        {
            this.serviceProvider.Dispose();
        }

        [Given("the following ClientIdentityConfigurations")]
        public void GivenCachingSecretClient(string configurationJson)
        {
            using MemoryStream configurationStream = new (Encoding.UTF8.GetBytes(configurationJson));
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .AddJsonStream(configurationStream)
                .Build();

            foreach (IConfigurationSection section in configRoot.GetChildren())
            {
                this.identityConfigurations.Add(section.Key, section.Get<ClientIdentityConfiguration>());
            }
        }

        [Given("a caching SecretClient '([^']*)' for the key vault '([^']*)' and the identity configuration '([^']*)'")]
        public async Task GivenCachingSecretClient(string label, string vaultName, string identityConfigurationKey)
        {
            SecretClient client = await this.secretClientFactory.GetSecretClientForAsync(
                vaultName,
                this.identityConfigurations[identityConfigurationKey])
                .ConfigureAwait(false);
            this.secretClients.Add(label, client);
        }

        [When("a secret named '([^']*)' is fetched through the caching SecretClient '([^']*)' and then labelled '([^']*)'")]
        public async Task ASecretIsFetchedAsync(string secretName, string clientLabel, string resultLabel)
        {
            SecretClient client = this.secretClients[clientLabel];
            Response<KeyVaultSecret> result = await client.GetSecretAsync(secretName);
            this.results.Add(resultLabel, result.Value.Value);
        }

        [When("the secret named '([^']*)' in key vault '([^']*)' is invalidated")]
        public void WhenTheSecretNamedInKeyVaultIsInvalidated(string mySecret, string myvault)
        {
            this.secretClientFactory.InvalidateSecret(myvault, mySecret);
        }

        [Then("the returned secret '([^']*)' has the value '([^']*)'")]
        public void ThenTheReturnedSecretHasTheValue(string resultLabel, string expectedSecretValue)
        {
            Assert.AreEqual(expectedSecretValue, this.results[resultLabel]);
        }
    }
}