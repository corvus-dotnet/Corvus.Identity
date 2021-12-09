// <copyright file="IKeyVaultSecretClientFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using global::Azure.Core;

    using global::Azure.Security.KeyVault.Secrets;

    /// <summary>
    /// Source of <see cref="SecretClient"/> instances. Enables tests to plug in fakes.
    /// </summary>
    internal interface IKeyVaultSecretClientFactory
    {
        /// <summary>
        /// Gets a <see cref="SecretClient"/> for the specified Azure Key Vault.
        /// </summary>
        /// <param name="keyVaultName">
        /// The name of the Azure Key Vault. (Not the fully-qualified domain name.)
        /// </param>
        /// <param name="credential">
        /// The credentials with which to initialize the <see cref="SecretClient"/>.
        /// </param>
        /// <param name="options">
        /// The secret client options to specify extra behaviour on the <see cref="SecretClient"/>.
        /// </param>
        /// <returns>A <see cref="SecretClient"/>.</returns>
        SecretClient GetSecretClientFor(
            string keyVaultName,
            TokenCredential credential,
            SecretClientOptions? options = default);
    }
}
