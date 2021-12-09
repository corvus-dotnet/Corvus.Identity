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
        /// <summary>
        /// Provides access to the default options that will be used by <see cref="GetSecretClientFor"/>
        /// if no value is provided for the <c>options</c> parameter.
        /// </summary>
        /// <returns>The <see cref="SecretClientOptions"/>.</returns>
        public static SecretClientOptions GetDefaultSecretClientOptions()
        {
            var options = new SecretClientOptions();
            options.AddPolicy(new KeyVaultProxy(), HttpPipelinePosition.PerCall);
            return options;
        }

        /// <inheritdoc/>
        public SecretClient GetSecretClientFor(
            string keyVaultName,
            TokenCredential credential,
            SecretClientOptions? options)
        {
            return new SecretClient(
                new Uri($"https://{keyVaultName}.vault.azure.net/"),
                credential,
                options ?? GetDefaultSecretClientOptions());
        }
    }
}
