// <copyright file="IKeyVaultSecretCache.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Caches secrets to avoid repeated lookups in Azure Key Vault.
    /// </summary>
    internal interface IKeyVaultSecretCache
    {
        /// <summary>
        /// Retrieve a cached secret it one is available for the specified combination of vault
        /// name, secret name, and client identity.
        /// </summary>
        /// <param name="vaultName">The name of the vault containing the secret.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="clientIdentity">
        /// The identity that will be used to access the key vault if the secret is not cached.
        /// This can be null because <see cref="KeyVaultSecretConfiguration.VaultClientIdentity"/>
        /// is allowed to be null, and that's typically where this comes from. A null value here
        /// signifies that we want to use the service identity.
        /// </param>
        /// <param name="secret">The secret, or null if the secret is not available.</param>
        /// <returns>
        /// True if the cache contains this secret.
        /// </returns>
        bool TryGetSecret(
            string vaultName,
            string secretName,
            ClientIdentityConfiguration? clientIdentity,
            [NotNullWhen(true)] out string? secret);

        /// <summary>
        /// Adds a secret to the cache.
        /// </summary>
        /// <param name="vaultName">The name of the vault containing the secret.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="clientIdentity">
        /// The identity that was used to access the key vault, or null if the service identity was
        /// used.
        /// </param>
        /// <param name="secret">The secret.</param>
        void AddSecret(
            string vaultName,
            string secretName,
            ClientIdentityConfiguration? clientIdentity,
            string secret);

        /// <summary>
        /// Removes a secret from the cache, if it is present.
        /// </summary>
        /// <param name="vaultName">The name of the vault containing the secret.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="clientIdentity">
        /// The identity that was used to access the key vault.
        /// </param>
        void InvalidateSecret(
            string vaultName,
            string secretName,
            ClientIdentityConfiguration? clientIdentity);
    }
}