// <copyright file="UseAzureKeyVaultWithNewSdk.cs" company="Endjin Limited">
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
    /// A service that retrieves a secret from Azure Key Vault.
    /// </summary>
    public class UseAzureKeyVaultWithNewSdk
    {
        private readonly IServiceIdentityAzureTokenCredentialSource tokenCredentialSource;

        /// <summary>
        /// Creates a <see cref="UseAzureKeyVaultWithNewSdk"/>.
        /// </summary>
        /// <param name="tokenCredentialSource">
        /// The source from which to obtain <see cref="TokenCredential"/> instances.
        /// </param>
        public UseAzureKeyVaultWithNewSdk(
            IServiceIdentityAzureTokenCredentialSource tokenCredentialSource)
        {
            this.tokenCredentialSource = tokenCredentialSource;
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
            TokenCredential credential = await this.tokenCredentialSource.GetAccessTokenAsync().ConfigureAwait(false);
            var client = new SecretClient(keyVaultUri, credential);

            Response<KeyVaultSecret> secret = await client.GetSecretAsync(secretName).ConfigureAwait(false);

            return secret.Value.Value;
        }
    }
}