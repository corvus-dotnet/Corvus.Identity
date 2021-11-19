// <copyright file="UseAzureKeyVaultAsServiceIdentityWithNewSdk.cs" company="Endjin Limited">
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
    /// A service that retrieves a secret from Azure Key Vault, using the service's own identity.
    /// </summary>
    public class UseAzureKeyVaultAsServiceIdentityWithNewSdk
    {
        private readonly IServiceIdentityAzureTokenCredentialSource tokenCredentialSource;

        /// <summary>
        /// Creates a <see cref="UseAzureKeyVaultAsServiceIdentityWithNewSdk"/>.
        /// </summary>
        /// <param name="tokenCredentialSource">
        /// The source from which to obtain <see cref="TokenCredential"/> instances.
        /// </param>
        public UseAzureKeyVaultAsServiceIdentityWithNewSdk(
            IServiceIdentityAzureTokenCredentialSource tokenCredentialSource)
        {
            this.tokenCredentialSource = tokenCredentialSource
                ?? throw new ArgumentNullException(nameof(tokenCredentialSource));
        }

        /// <summary>
        /// Gets a secret from a vault.
        /// </summary>
        /// <param name="keyVaultUri">
        /// The URI of the Azure Key Vault from which to read the secret.
        /// </param>
        /// <param name="secretName">
        /// The name of the secret to retrieve.
        /// </param>
        /// <returns>
        /// A task that produces the retrieved secret's value.
        /// </returns>
        public async Task<string> GetSecretAsync(Uri keyVaultUri, string secretName)
        {
            TokenCredential credential = await this.tokenCredentialSource.GetTokenCredentialAsync()
                .ConfigureAwait(false);
            var client = new SecretClient(keyVaultUri, credential);

            Response<KeyVaultSecret> vaultResponse = await client.GetSecretAsync(secretName).ConfigureAwait(false);

            return vaultResponse.Value.Value;
        }
    }
}