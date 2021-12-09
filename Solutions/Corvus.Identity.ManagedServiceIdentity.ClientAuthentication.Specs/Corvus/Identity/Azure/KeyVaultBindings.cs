namespace Corvus.Identity.Azure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;
    using global::Azure;
    using global::Azure.Core;
    using global::Azure.Core.Pipeline;
    using global::Azure.Security.KeyVault.Secrets;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using TechTalk.SpecFlow;

    [Binding]
    public class KeyVaultBindings
    {
        private readonly FakeKeyVaultSecretClientFactory secretClientFactory;
        private readonly TokenCredentialBindings tokenCredentials;

        public KeyVaultBindings(
            TokenCredentialBindings tokenCredentials)
        {
            List<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs = new ();
            this.secretClientFactory = new FakeKeyVaultSecretClientFactory(vaultCredentialPairs);
            this.VaultCredentialPairs = vaultCredentialPairs;
            this.tokenCredentials = tokenCredentials;
        }

        public IReadOnlyList<(string KeyVaultName, TokenCredential Credential)> VaultCredentialPairs { get; }

        public void AddKeyVaultFactoryForTests(IServiceCollection services)
        {
            services.AddSingleton<IKeyVaultSecretClientFactory>(this.secretClientFactory);
        }

        [Given("the key vault '(.*)' returns '(.*)' for the secret named '(.*)'")]
        public void GivenTheKeyVaultReturnsForTheSecretNamed(string keyVault, string secret, string secretName)
        {
            this.secretClientFactory.AddSecret(keyVault, secret, secretName);
        }

        [Given(@"there is no cached TokenCredential for the secret named '(.*)'")]
        public void GivenThereIsNoCachedTokenCredentialForTheSecretNamed(string secretName)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ScenarioContext.Current.Pending();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Given(@"there is a cached TokenCredential for the secret named '(.*)'")]
        public void GivenThereIsACachedTokenCredentialForTheSecretNamed(string p0)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ScenarioContext.Current.Pending();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [When(@"in this test we identify the token credential passed when creating the key vault '(.*)' as '(.*)'")]
        public void WhenInThisTestWeIdentifyTheTokenCredentialPassedWhenCreatingTheKeyVaultAs(
            string keyVaultName,
            string credentialName)
        {
            TokenCredential keyVaultCredentials = this.VaultCredentialPairs
                .Single(x => x.KeyVaultName == keyVaultName)
                .Credential;
            this.tokenCredentials.SetNamedCredential(credentialName, keyVaultCredentials);
        }

        [Then(@"the secret named '(.*)' is retrieved from key vault")]
        public void ThenTheSecretNamedIsRetrievedFromKeyVault(string secretName)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ScenarioContext.Current.Pending();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Then(@"the secret named '(.*)' is not retrieved from key vault")]
        public void ThenTheSecretNamedIsNotRetrievedFromKeyVault(string p0)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ScenarioContext.Current.Pending();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Then(@"the cache should contain a secret named '(.*)'")]
        public void ThenTheCacheShouldContainASecretNamed(string p0)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ScenarioContext.Current.Pending();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private class FakeKeyVaultSecretClientFactory : IKeyVaultSecretClientFactory
        {
            private readonly IList<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs;
            private readonly Dictionary<(string vaultName, string secretName), string> secrets = new ();
            private readonly Dictionary<string, FakeResponsePolicy> vaultMockResponsePolicies = new ();

            private readonly KeyVaultSecretClientFactory realKeyVaultSecretClientFactory;

            public FakeKeyVaultSecretClientFactory(
                IList<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs)
            {
                this.vaultCredentialPairs = vaultCredentialPairs;
                this.realKeyVaultSecretClientFactory = new KeyVaultSecretClientFactory();
            }

            public SecretClient GetSecretClientFor(string keyVaultName, TokenCredential credential, SecretClientOptions? options)
            {
                this.vaultCredentialPairs.Add((keyVaultName, credential));

                options ??= KeyVaultSecretClientFactory.GetDefaultSecretClientOptions();

                if (!this.vaultMockResponsePolicies.TryGetValue(keyVaultName, out FakeResponsePolicy? policy))
                {
                    policy = new FakeResponsePolicy(keyVaultName, this);
                }

                options.AddPolicy(policy, HttpPipelinePosition.PerCall);

                return this.realKeyVaultSecretClientFactory.GetSecretClientFor(keyVaultName, credential, options);
            }

            internal void AddSecret(string keyVault, string secret, string secretName)
            {
                this.secrets.Add((keyVault, secretName), secret);
            }

            private class FakeResponsePolicy : HttpPipelinePolicy
            {
                private readonly string name;
                private readonly FakeKeyVaultSecretClientFactory factory;

                public FakeResponsePolicy(string keyVaultName, FakeKeyVaultSecretClientFactory factory)
                {
                    this.name = keyVaultName;
                    this.factory = factory;
                }

                public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
                {
                    throw new NotImplementedException();
                }

                public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
                {
                    string requestPath = message.Request.Uri.Path;

                    if (!requestPath.StartsWith("/secrets"))
                    {
                        throw new NotImplementedException("Mocked HttpPipelineTransport only supports secrets.");
                    }

                    string secretName = requestPath[9..^1];

                    if (this.factory.secrets.TryGetValue((this.name, secretName), out string? secretValue))
                    {
                        KeyVaultSecret secret = SecretModelFactory.KeyVaultSecret(
                            SecretModelFactory.SecretProperties(null, null, secretName),
                            value: secretValue);

                        var responseStream = new MemoryStream();
                        var writer = new Utf8JsonWriter(responseStream);
                        JsonSerializer.Serialize(writer, secret, default);

                        responseStream.Position = 0;

                        var mockedResponse = new Mock<Response>();
                        mockedResponse.Setup(x => x.Status).Returns(200);
                        mockedResponse.SetupGet(x => x.ContentStream).Returns(responseStream);

                        message.Response = mockedResponse.Object;

                        return new ValueTask(Task.CompletedTask);
                    }

                    throw new InvalidOperationException($"No secret {secretName} exists for vault {this.name}");
                }
            }
        }
    }
}
