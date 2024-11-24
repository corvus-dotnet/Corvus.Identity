// <copyright file="KeyVaultBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure.Internal;
    using global::Azure;
    using global::Azure.Core;
    using global::Azure.Security.KeyVault.Secrets;

    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Reqnroll;

    [Binding]
    public class KeyVaultBindings
    {
        private readonly FakeKeyVaultSecretClientFactory secretClientFactory;
        private readonly TokenCredentialBindings tokenCredentials;

        public KeyVaultBindings(
            TokenCredentialBindings tokenCredentials)
        {
            List<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs = new();
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

        [When("in this test we identify the token credential passed when creating the key vault '(.*)' as '(.*)'")]
        public void WhenInThisTestWeIdentifyTheTokenCredentialPassedWhenCreatingTheKeyVaultAs(
            string keyVaultName,
            string credentialName)
        {
            TokenCredential keyVaultCredentials = this.VaultCredentialPairs
                .Single(x => x.KeyVaultName == keyVaultName)
                .Credential;
            this.tokenCredentials.SetNamedCredential(credentialName, keyVaultCredentials);
        }

        [Then(@"the key vault client should have been used (\d+) times")]
        public void ThenTheKeyVaultClientShouldNotHaveBeenUsed(int times)
        {
            Assert.That(this.VaultCredentialPairs.Count, Is.EqualTo(times));
        }

        private class FakeKeyVaultSecretClientFactory : IKeyVaultSecretClientFactory
        {
            private readonly IList<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs;
            private readonly Dictionary<(string VaultName, string SecretName), string> secrets = new();

            public FakeKeyVaultSecretClientFactory(
                IList<(string KeyVaultName, TokenCredential Credential)> vaultCredentialPairs)
            {
                this.vaultCredentialPairs = vaultCredentialPairs;
            }

            public SecretClient GetSecretClientFor(string keyVaultName, TokenCredential credential)
            {
                this.vaultCredentialPairs.Add((keyVaultName, credential));
                return new FakeSecretClient(this, keyVaultName);
            }

            internal void AddSecret(string keyVault, string secret, string secretName)
            {
                this.secrets.Add((keyVault, secretName), secret);
            }

            private class FakeSecretClient : SecretClient
            {
                private readonly FakeKeyVaultSecretClientFactory fakeKeyVaultSecretClientFactory;
                private readonly string keyVaultName;

                public FakeSecretClient(FakeKeyVaultSecretClientFactory fakeKeyVaultSecretClientFactory, string keyVaultName)
                {
                    this.fakeKeyVaultSecretClientFactory = fakeKeyVaultSecretClientFactory;
                    this.keyVaultName = keyVaultName;
                }

                public override Task<Response<KeyVaultSecret>> GetSecretAsync(
                    string name,
                    string version,
                    CancellationToken cancellationToken)
                {
                    if (this.fakeKeyVaultSecretClientFactory.secrets.TryGetValue((this.keyVaultName, name), out string? secretValue))
                    {
                        KeyVaultSecret secret = SecretModelFactory.KeyVaultSecret(
                            SecretModelFactory.SecretProperties(),
                            value: secretValue);

                        return Task.FromResult(Response.FromValue(secret, new Mock<Response>().Object));
                    }

                    throw new InvalidOperationException($"No secret {name} exists for vault {this.keyVaultName}");
                }
            }
        }
    }
}