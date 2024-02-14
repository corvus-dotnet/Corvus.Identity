// <copyright file="TokenCredentialSourceFromDynamicConfigurationSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure.TokenCredentialSourceFromDynamicConfiguration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;

    using global::Azure.Core;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using NUnit.Framework;

    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Assist;

    [Binding]
    public sealed class TokenCredentialSourceFromDynamicConfigurationSteps : IDisposable
    {
        private readonly TokenCredentialBindings tokenCredentials;
        private readonly KeyVaultBindings keyVault;
        private readonly TestCache secretCache = new();
        private IAzureTokenCredentialSourceFromDynamicConfiguration? credsFromConfig;
        private TestConfiguration? configuration;
        private ServiceProvider? serviceProvider;
        private string? validationResult;
        private ClientIdentitySourceTypes validatedType;

        public TokenCredentialSourceFromDynamicConfigurationSteps(
            TokenCredentialBindings tokenCredentials,
            KeyVaultBindings keyVault)
        {
            this.tokenCredentials = tokenCredentials;
            this.keyVault = keyVault;
        }

        [Given("configuration of")]
        public void GivenConfigurationOf(string configurationJson)
        {
            this.configuration = ConfigLoading.LoadJsonConfiguration<TestConfiguration>(configurationJson);
            ServiceCollection services = new();
            services.AddAzureTokenCredentialSourceFromDynamicConfiguration();

            services.RemoveAll(typeof(IKeyVaultSecretCache));
            services.AddSingleton<IKeyVaultSecretCache>(this.secretCache);

            this.keyVault.AddKeyVaultFactoryForTests(services);
            this.serviceProvider = services.BuildServiceProvider();

            this.credsFromConfig = this.serviceProvider.GetRequiredService<IAzureTokenCredentialSourceFromDynamicConfiguration>();
        }

        [Given("a ClientIdAndSecret configuration with '([^']*)', '([^']*)', '([^']*)', '([^']*)'")]
        public void GivenAClientIdAndSecretConfigurationWith(
            string identitySourceType,
            string azureAppTenantId,
            string azureAdAppClientId,
            string azureAppClientSecretPlainText)
        {
            var id = new ClientIdentityConfiguration
            {
                IdentitySourceType = string.IsNullOrWhiteSpace(identitySourceType)
                    ? null
                    : Enum.Parse<ClientIdentitySourceTypes>(identitySourceType),
                AzureAdAppTenantId = azureAppTenantId,
                AzureAdAppClientId = azureAdAppClientId,
                AzureAdAppClientSecretPlainText = azureAppClientSecretPlainText,
            };

            this.configuration = new TestConfiguration { ClientIdentity = id };
        }

        [Given("a ClientIdAndSecret configuration with '([^']*)', '([^']*)', '([^']*)', '([^']*)' and a secret in key vault")]
        public void GivenAClientIdAndSecretConfigurationWithAndASecretInKeyVault(
            string identitySourceType,
            string azureAppTenantId,
            string azureAdAppClientId,
            string azureAppClientSecretPlainText)
        {
            this.GivenAClientIdAndSecretConfigurationWith(identitySourceType, azureAppTenantId, azureAdAppClientId, azureAppClientSecretPlainText);
            this.configuration!.ClientIdentity!.AzureAdAppClientSecretInKeyVault = new()
            {
                VaultName = "somevault",
                SecretName = "SomeSecret",
            };
        }

        [Given(@"a ClientIdAndCertificate configuration with '([^']*)', '([^']*)', '([^']*)', '([^']*)', '([^']*)', '([^']*)'")]
        public void GivenAClientIdAndCertificateConfigurationWith(
            string identitySourceType,
            string azureAppTenantId,
            string azureAdAppClientId,
            StoreLocation storeLocation,
            string storeName,
            string subjectName)
        {
            var id = new ClientIdentityConfiguration
            {
                IdentitySourceType = string.IsNullOrWhiteSpace(identitySourceType)
                    ? null
                    : Enum.Parse<ClientIdentitySourceTypes>(identitySourceType),
                AzureAdAppTenantId = azureAppTenantId,
                AzureAdAppClientId = azureAdAppClientId,
                AzureAdAppClientCertificate = new Certificates.ClientCertificateConfiguration
                {
                    StoreLocation = storeLocation,
                    StoreName = storeName,
                    SubjectName = subjectName,
                },
            };

            this.configuration = new TestConfiguration { ClientIdentity = id };
        }

        [When("I validate the configuration")]
        public void WhenIValidateTheConfiguration()
        {
            this.validationResult = ClientIdentityConfigurationValidation.Validate(
                this.configuration!.ClientIdentity!,
                out this.validatedType);
        }

        [Then("the validation should pass")]
        public void ThenTheValidationShouldPass()
        {
            Assert.IsNull(this.validationResult);
        }

        [Then("the validated type should be '([^']*)'")]
        public void ThenTheValidatedTypeShouldBe(ClientIdentitySourceTypes expectedValidatedType)
        {
            Assert.AreEqual(expectedValidatedType, this.validatedType);
        }

        [Then("the validation should fail with '([^']*)'")]
        public void ThenTheValidationShouldFailWith(string message)
        {
            Assert.AreEqual(message, this.validationResult);
        }

        [When("a TokenCredential is fetched for this configuration")]
        public async Task WhenATokenCredentialIsFetchedForThisConfiguration()
        {
            await this.WhenATokenCredentialIsFetchedForThisConfigurationAsCredential(null).ConfigureAwait(false);
        }

        [Given("a TokenCredential is fetched for this configuration as credential '(.*)'")]
        [When("a TokenCredential is fetched for this configuration as credential '(.*)'")]
        public async Task WhenATokenCredentialIsFetchedForThisConfigurationAsCredential(string? credentialName)
        {
            IAzureTokenCredentialSource source =
                await this.credsFromConfig!.CredentialSourceForConfigurationAsync(this.configuration!.ClientIdentity!).ConfigureAwait(false);
            TokenCredential credential = await source.GetTokenCredentialAsync().ConfigureAwait(false);

            this.tokenCredentials.SetNamedCredential(credentialName, credential);
        }

        [When("this ClientIdentityConfiguration is invalidated via '([^']*)'")]
        public async Task WhenThisClientIdentityConfigurationIsInvalidatedVia(string invalidationMechanism)
        {
            if (invalidationMechanism == "IAzureTokenCredentialSource")
            {
                IAzureTokenCredentialSource source =
                    await this.credsFromConfig!.CredentialSourceForConfigurationAsync(this.configuration!.ClientIdentity!).ConfigureAwait(false);
                await source.GetReplacementForFailedTokenCredentialAsync().ConfigureAwait(false);
            }
            else if (invalidationMechanism == "IAzureTokenCredentialSourceFromDynamicConfiguration")
            {
                this.credsFromConfig!.InvalidateFailedAccessToken(this.configuration!.ClientIdentity!);
            }
            else
            {
                Assert.Fail("Unrecognized invalidation mechanism: " + invalidationMechanism);
            }
        }

        [Given("the secret cache returns '([^']*)' for the secret named '([^']*)' in '([^']*)'")]
        public void GivenTheSecretCacheReturnsForTheSecretNamedIn(
            string secretValue, string secretName, string vaultName)
        {
            this.secretCache.ReturnThisSecretInThisTest(vaultName, secretName, secretValue);
        }

        [Then("the secret cache should have seen these requests")]
        public void ThenTheSecretCacheShouldHaveSeenTheseRequests(Table table)
        {
            this.CheckCacheOperations(table, this.secretCache.TryGets, "gets");
        }

        [Then("the secret cache should have seen these credentials invalidated")]
        public void ThenTheSecretCacheShouldHaveSeenTheseCredentialsInvalidated(Table table)
        {
            this.CheckCacheOperations(table, this.secretCache.Invalidations, "invalidations");
        }

        public void Dispose()
        {
            this.serviceProvider?.Dispose();
        }

        private void CheckCacheOperations(Table table, List<SecretCacheRow> actualRows, string operationName)
        {
            var rows = table.CreateSet<SecretCacheRow>().ToList();
            Assert.AreEqual(rows.Count, actualRows.Count, $"Number of {operationName}");
            foreach ((SecretCacheRow expected, SecretCacheRow actual) in rows.Zip(actualRows))
            {
                Assert.AreEqual(expected.VaultName, actual.VaultName);
                Assert.AreEqual(expected.SecretName, actual.SecretName);

                ClientIdentityConfiguration? actualCredential = this.secretCache.Identities[actual.Credential];
                ClientIdentityConfiguration expectedCredential;
                switch (expected.Credential)
                {
                    case "null":
                        Assert.IsNull(actualCredential);
                        break;

                    case "AzureAdAppClientSecretInKeyVault":
                        expectedCredential = this.configuration!.ClientIdentity!.AzureAdAppClientSecretInKeyVault!.VaultClientIdentity!;
                        Assert.AreEqual(JsonSerializer.Serialize(expectedCredential), JsonSerializer.Serialize(actualCredential));
                        break;

                    case "AzureAdAppClientSecretInKeyVault.AzureAdAppClientSecretInKeyVault":
                        expectedCredential = this.configuration!.ClientIdentity!.AzureAdAppClientSecretInKeyVault!.VaultClientIdentity!.AzureAdAppClientSecretInKeyVault!.VaultClientIdentity!;
                        Assert.AreEqual(JsonSerializer.Serialize(expectedCredential), JsonSerializer.Serialize(actualCredential));
                        break;
                }
            }
        }

        public class TestConfiguration
        {
            public ClientIdentityConfiguration? ClientIdentity { get; set; }
        }

        private class TestCache : IKeyVaultSecretCache
        {
            private readonly Dictionary<(string VaultName, string SecretName), string> secretsToReturn = new();

            public List<SecretCacheRow> TryGets { get; } = new();

            public List<SecretCacheRow> Invalidations { get; } = new();

            public Dictionary<string, ClientIdentityConfiguration?> Identities { get; } = new();

            public void AddSecret(string vaultName, string secretName, ClientIdentityConfiguration? clientIdentity, string secret)
            {
            }

            public void InvalidateSecret(string vaultName, string secretName, ClientIdentityConfiguration? clientIdentity)
            {
                string identityName = $"id{this.Identities.Count}";
                this.Identities.Add(identityName, clientIdentity);
                this.Invalidations.Add(new SecretCacheRow(vaultName, secretName, identityName));
            }

            public bool TryGetSecret(string vaultName, string secretName, ClientIdentityConfiguration? clientIdentity, [NotNullWhen(true)] out string? secret)
            {
                string identityName = $"id{this.Identities.Count}";
                this.Identities.Add(identityName, clientIdentity);
                this.TryGets.Add(new SecretCacheRow(vaultName, secretName, identityName));

                return this.secretsToReturn.TryGetValue((vaultName, secretName), out secret);
            }

            internal void ReturnThisSecretInThisTest(string vaultName, string secretName, string secretValue)
            {
                this.secretsToReturn.Add((vaultName, secretName), secretValue);
            }
        }

        private record SecretCacheRow(string VaultName, string SecretName, string Credential);
    }
}