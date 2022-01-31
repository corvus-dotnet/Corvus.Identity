// <copyright file="KeyVaultSecretCache.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json;

    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Caches secrets to avoid repeated lookups in Azure Key Vault.
    /// </summary>
    internal class KeyVaultSecretCache : IKeyVaultSecretCache
    {
        private static readonly MemoryCacheEntryOptions EntryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20),
        };

        private readonly MemoryCache cache = new(
            new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));

        /// <inheritdoc/>
        public void AddSecret(string vaultName, string secretName, ClientIdentityConfiguration? clientIdentity, string secret)
        {
            string key = GetKey(vaultName, secretName, clientIdentity);
            this.cache.Set(key, secret, EntryOptions);
        }

        /// <inheritdoc/>
        public void InvalidateSecret(string vaultName, string secretName, ClientIdentityConfiguration? clientIdentity)
        {
            string key = GetKey(vaultName, secretName, clientIdentity);
            this.cache.Remove(key);
        }

        /// <inheritdoc/>
        public bool TryGetSecret(
            string vaultName,
            string secretName,
            ClientIdentityConfiguration? clientIdentity,
            [NotNullWhen(true)] out string? secret)
        {
            string key = GetKey(vaultName, secretName, clientIdentity);
            return this.cache.TryGetValue(key, out secret);
        }

        private static string GetKey(
            string vaultName,
            string secretName,
            ClientIdentityConfiguration? clientIdentity)
        {
            // TODO: this is a placeholder. It is not efficient.
            string clientIdText = clientIdentity is null
                ? "service"
                : JsonSerializer.Serialize(clientIdentity);
            return $"V:{vaultName},S:{secretName},I{clientIdText}";
        }
    }
}