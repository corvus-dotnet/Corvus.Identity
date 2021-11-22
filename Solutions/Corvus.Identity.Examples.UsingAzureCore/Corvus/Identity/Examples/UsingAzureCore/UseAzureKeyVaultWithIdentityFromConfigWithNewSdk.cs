// <copyright file="UseAzureKeyVaultWithIdentityFromConfigWithNewSdk.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Examples.UsingAzureCore
{
    using System;
    using System.Threading.Tasks;

    using Azure;
    using Azure.Core;
    using Azure.Security.KeyVault.Secrets;

    using Corvus.Identity.ClientAuthentication.Azure;

    /// <summary>
    /// A service that retrieves a secret from Azure Key Vault, using an identity specified with
    /// configuration.
    /// </summary>
    public class UseAzureKeyVaultWithIdentityFromConfigWithNewSdk
    {
        private readonly IAzureTokenCredentialSourceFromDynamicConfiguration credentialSourceFromConfiguration;

        /// <summary>
        /// Creates a <see cref="UseAzureKeyVaultWithIdentityFromConfigWithNewSdk"/>.
        /// </summary>
        /// <param name="credentialSourceFromConfiguration">
        /// Provides the ability to get token sources based on configuration settings.
        /// </param>
        public UseAzureKeyVaultWithIdentityFromConfigWithNewSdk(
            IAzureTokenCredentialSourceFromDynamicConfiguration credentialSourceFromConfiguration)
        {
            this.credentialSourceFromConfiguration = credentialSourceFromConfiguration;
        }

        /// <summary>
        /// Gets a secret from a vault.
        /// </summary>
        /// <param name="identity">
        /// Configuration describing the identity with which to connect to Azure Key Vault.
        /// </param>
        /// <param name="keyVaultUri">
        /// The URI of the Azure Key Vault from which to read the secret.
        /// </param>
        /// <param name="secretName">
        /// The name of the secret to retrieve.
        /// </param>
        /// <returns>
        /// A task that produces the retrieved secret's value.
        /// </returns>
        public async Task<string> GetSecretAsync(
            ClientIdentityConfiguration identity,
            Uri keyVaultUri,
            string secretName)
        {
            IAzureTokenCredentialSource tokenCredentialSource =
                await this.credentialSourceFromConfiguration.CredentialSourceForConfigurationAsync(identity)
                .ConfigureAwait(false);
            TokenCredential credential = await tokenCredentialSource.GetTokenCredentialAsync()
                .ConfigureAwait(false);
            var client = new SecretClient(keyVaultUri, credential);

            Response<KeyVaultSecret> vaultResponse = await client.GetSecretAsync(secretName).ConfigureAwait(false);

            return vaultResponse.Value.Value;
        }
    }
}