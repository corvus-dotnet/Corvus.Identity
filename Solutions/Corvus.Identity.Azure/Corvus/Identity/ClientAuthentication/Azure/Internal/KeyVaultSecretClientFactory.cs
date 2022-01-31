// <copyright file="KeyVaultSecretClientFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;

    using global::Azure.Core;

    using global::Azure.Security.KeyVault.Secrets;

    /// <summary>
    /// Supplies <see cref="SecretClient"/>s in cases where tests aren't supplying fakes.
    /// </summary>
    internal class KeyVaultSecretClientFactory : IKeyVaultSecretClientFactory
    {
        /// <inheritdoc/>
        public SecretClient GetSecretClientFor(
            string keyVaultName,
            TokenCredential credential)
        {
            return new SecretClient(
                new Uri($"https://{keyVaultName}.vault.azure.net/"),
                credential);
        }
    }
}