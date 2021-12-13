namespace Corvus.Identity.Azure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;
    using global::Azure;
    using global::Azure.Core;
    using global::Azure.Core.Pipeline;
    using global::Azure.Security.KeyVault.Secrets;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Assist;

    [Binding]
    public class KeyVaultBindings
    {
        private readonly List<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs = new ();
        private readonly List<SecretRow> secretsFetched = new ();
        private readonly FakeKeyVaultSecretClientFactory secretClientFactory;
        private readonly TokenCredentialBindings tokenCredentials;

        public KeyVaultBindings(
            TokenCredentialBindings tokenCredentials)
        {
            List<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs = new ();
            this.secretClientFactory = new FakeKeyVaultSecretClientFactory(this);
            this.tokenCredentials = tokenCredentials;
        }

        public IReadOnlyList<(string KeyVaultName, TokenCredential Credential)> VaultCredentialPairs => this.vaultCredentialPairs;

        public IReadOnlyList<SecretRow> SecretsFetched => this.secretsFetched;

        public void AddKeyVaultFactoryForTests(IServiceCollection services)
        {
            services.AddSingleton<IKeyVaultSecretClientFactory>(this.secretClientFactory);
        }

        [Given("the key vault '(.*)' returns '(.*)' for the secret named '(.*)'")]
        public void GivenTheKeyVaultReturnsForTheSecretNamed(string keyVault, string secret, string secretName)
        {
            this.secretClientFactory.AddSecret(keyVault, secret, secretName);
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

        [Then("the following secrets are retrieved from key vault")]
        public void TheseSecretsareRetrieve(Table table)
        {
            var rows = table.CreateSet<SecretRow>().ToList();

            Assert.AreEqual(rows.Count, this.SecretsFetched.Count);

            foreach ((SecretRow expected, SecretRow actual) in rows.Zip(this.SecretsFetched))
            {
                Assert.AreEqual(expected.VaultName, actual.VaultName);
                Assert.AreEqual(expected.SecretName, actual.SecretName);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1313:Parameter names should begin with lower-case letter",
            Justification = "StyleCop 1.1 doesn't understand that these are property names")]
        public record SecretRow(string VaultName, string SecretName, TokenCredential Credential);

        private class FakeKeyVaultSecretClientFactory : IKeyVaultSecretClientFactory
        {
            private readonly KeyVaultBindings parent;
            private readonly KeyVaultSecretClientFactory realKeyVaultSecretClientFactory;
            private readonly Dictionary<(string vaultName, string secretName), string> secrets = new ();
            private readonly Dictionary<string, FakeResponsePolicy> vaultMockResponsePolicies = new ();

            public FakeKeyVaultSecretClientFactory(
                KeyVaultBindings parent)
            {
                this.parent = parent;
                this.realKeyVaultSecretClientFactory = new KeyVaultSecretClientFactory();
            }

            public SecretClient GetSecretClientFor(
                string keyVaultName,
                TokenCredential credential,
                SecretClientOptions? options)
            {
                this.parent.vaultCredentialPairs.Add((keyVaultName, credential));

                options ??= new SecretClientOptions();

                if (!this.vaultMockResponsePolicies.TryGetValue(keyVaultName, out FakeResponsePolicy? policy))
                {
                    policy = new FakeResponsePolicy(this.parent, keyVaultName, credential, this);
                    this.vaultMockResponsePolicies.Add(keyVaultName, policy);
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
                private readonly KeyVaultBindings parent;
                private readonly string keyVaultName;
                private readonly TokenCredential credential;
                private readonly FakeKeyVaultSecretClientFactory factory;

                public FakeResponsePolicy(
                    KeyVaultBindings parent,
                    string keyVaultName,
                    TokenCredential credential,
                    FakeKeyVaultSecretClientFactory factory)
                {
                    this.parent = parent;
                    this.keyVaultName = keyVaultName;
                    this.credential = credential;
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

                    this.parent.secretsFetched.Add(new SecretRow(
                        this.keyVaultName, secretName, this.credential));

                    if (this.factory.secrets.TryGetValue((this.keyVaultName, secretName), out string? secretValue))
                    {
                        // Irritatingly, although KeyVaultSecret is perfectly capable of serializing
                        // itself to JSON, the relevant methods are all internal and do not seem to
                        // be callable directly. And simply passing the thing to JsonSerializer
                        // produces entirely the wrong results. So we need to build our JSON
                        // from scratch.
                        var responseBody = new JsonObject
                        {
                            ["value"] = secretValue,
                        };

                        var responseStream = new MemoryStream();
                        using (Utf8JsonWriter writer = new (responseStream))
                        {
                            responseBody.WriteTo(writer);
                        }

                        responseStream.Position = 0;

                        var mockedResponse = new Mock<Response>();
                        mockedResponse.Setup(x => x.Status).Returns(200);
                        mockedResponse.SetupGet(x => x.ContentStream).Returns(responseStream);

                        message.Response = mockedResponse.Object;

                        return new ValueTask(Task.CompletedTask);
                    }

                    throw new InvalidOperationException($"No secret {secretName} exists for vault {this.keyVaultName}");
                }
            }
        }
    }
}
