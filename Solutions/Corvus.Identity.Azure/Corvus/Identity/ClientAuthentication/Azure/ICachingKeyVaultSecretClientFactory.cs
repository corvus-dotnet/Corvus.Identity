// <copyright file="ICachingKeyVaultSecretClientFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Security.KeyVault.Secrets;

    /// <summary>
    /// Provides <see cref="SecretClient"/> instances configured to communicate with Azure Key
    /// Vault using a particular <see cref="ClientIdentityConfiguration"/>, and which cache results
    /// to avoid reading the same secrets over and over again from Key Vault.
    /// </summary>
    public interface ICachingKeyVaultSecretClientFactory
    {
        /// <summary>
        /// Gets a <see cref="SecretClient"/> for the specified Azure Key Vault.
        /// </summary>
        /// <param name="keyVaultName">
        /// The name of the Azure Key Vault. (Not the fully-qualified domain name.)
        /// </param>
        /// <param name="clientIdentity">
        /// The identity to use when communicating with the Key Vault.
        /// </param>
        /// <param name="cancellationToken">
        /// Enables cancellation.
        /// </param>
        /// <returns>A <see cref="SecretClient"/>.</returns>
        ValueTask<SecretClient> GetSecretClientForAsync(
            string keyVaultName,
            ClientIdentityConfiguration clientIdentity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tells the cache that a secret is no longer valid, and should not be returned from
        /// the cache again, with a fresh looking being performed if it is asked for again.
        /// </summary>
        /// <param name="keyVaultName">The name of they key vault.</param>
        /// <param name="mySecret">The secret name.</param>
        void InvalidateSecret(string keyVaultName, string mySecret);
    }
}