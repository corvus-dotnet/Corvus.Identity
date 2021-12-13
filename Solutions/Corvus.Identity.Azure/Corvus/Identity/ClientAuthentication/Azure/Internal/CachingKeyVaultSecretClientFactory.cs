// <copyright file="CachingKeyVaultSecretClientFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using global::Azure.Security.KeyVault.Secrets;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// A source of <see cref="SecretClient"/> instances which are set up to cache responses
    /// where appropriate.
    /// </summary>
    internal class CachingKeyVaultSecretClientFactory : ICachingKeyVaultSecretClientFactory
    {
        private readonly object sync = new ();
        private readonly IServiceProvider serviceProvider;
        private readonly IKeyVaultSecretClientFactory underlyingSecretClientFactory;
        private readonly Dictionary<(string VaultName, string ClientIdentity), (SecretClient Client, KeyVaultProxy Proxy)> secretClientsByVaultNameAndClientIdentity = new ();
        private IAzureTokenCredentialSourceFromDynamicConfiguration? tokenCredentialSource;

        /// <summary>
        /// Creates a <see cref="CachingKeyVaultSecretClientFactory"/>.
        /// </summary>
        /// <param name="serviceProvider">
        /// DI service provider, enabling us to defer the fetching of the
        /// IAzureTokenCredentialSourceFromDynamicConfiguration service. It depends right back on
        /// us, creating a circular dependency. That's OK because we don't need access to it until
        /// after DI has completed, but it does mean we need to resolve the service later.
        /// </param>
        /// <param name="underlyingSecretClientFactory">
        /// Creates the real (non-caching) <see cref="SecretClient"/> instances that will be used
        /// when secrets cannot be retrieved from the cache.
        /// </param>
        public CachingKeyVaultSecretClientFactory(
            IServiceProvider serviceProvider,
            IKeyVaultSecretClientFactory underlyingSecretClientFactory)
        {
            this.serviceProvider = serviceProvider;
            this.underlyingSecretClientFactory = underlyingSecretClientFactory;
        }

        private IAzureTokenCredentialSourceFromDynamicConfiguration TokenCredentialSource
            => this.tokenCredentialSource ??= this.serviceProvider.GetRequiredService<IAzureTokenCredentialSourceFromDynamicConfiguration>();

        /// <inheritdoc/>
        public async ValueTask<SecretClient> GetSecretClientForAsync(
            string keyVaultName,
            ClientIdentityConfiguration clientIdentity,
            CancellationToken cancellationToken)
        {
            string clientIdKey = JsonSerializer.Serialize(clientIdentity);  // TODO: we need better key-ification
            (string VaultName, string ClientIdentity) vaultAndClientKey = (keyVaultName, clientIdKey);

            // We're not using ConcurrentDictionary because creating the entry when it's missing
            // requires async operations. ConcurrentDictionary doesn't really support this (which
            // is because it's typically a bad idea to be holding a lock over an await) so we lock
            // once briefly to see if the entry is there, and if not, let go, do the slow work,
            // then reacquire the lock to check nobody else got in there in the meantime. (So this
            // is an optimistic locking strategy, which ConcurrentDictionary isn't built for.)
            lock (this.sync)
            {
                if (this.secretClientsByVaultNameAndClientIdentity.TryGetValue(vaultAndClientKey, out (SecretClient Client, KeyVaultProxy Proxy) result))
                {
                    return result.Client;
                }
            }

            IAzureTokenCredentialSource credentialSource = await this.TokenCredentialSource
                .CredentialSourceForConfigurationAsync(clientIdentity, cancellationToken)
                .ConfigureAwait(false);
            TokenCredential credential = await credentialSource
                .GetTokenCredentialAsync(cancellationToken)
                .ConfigureAwait(false);

            lock (this.sync)
            {
                if (this.secretClientsByVaultNameAndClientIdentity.TryGetValue(vaultAndClientKey, out (SecretClient Client, KeyVaultProxy Proxy) clientAndProxy))
                {
                    // In the time it took us to retrieve the token credential, it looks like some
                    // other thread got in there before us, so we discard the credentials we just
                    // created, and return the SecretClient that 'won'.
                    return clientAndProxy.Client;
                }
                else
                {
                    (SecretClientOptions Options, KeyVaultProxy Proxy) optionsAndProxy = MakeSecretClientOptions();
                    SecretClient secretClient = this.underlyingSecretClientFactory.GetSecretClientFor(
                        keyVaultName,
                        credential,
                        optionsAndProxy.Options);
                    this.secretClientsByVaultNameAndClientIdentity.Add(vaultAndClientKey, (secretClient, optionsAndProxy.Proxy));
                    return secretClient;
                }
            }
        }

        /// <inheritdoc/>
        public void InvalidateSecret(string keyVaultName, string mySecret)
        {
            lock (this.sync)
            {
                foreach (((string VaultName, string ClientIdentity) key, (SecretClient Client, KeyVaultProxy Proxy) entry) in this.secretClientsByVaultNameAndClientIdentity)
                {
                    if (key.VaultName == keyVaultName)
                    {
                        entry.Proxy.InvalidateSecret(mySecret);
                    }
                }
            }
        }

        private static (SecretClientOptions Options, KeyVaultProxy Proxy) MakeSecretClientOptions()
        {
            var options = new SecretClientOptions();
            var proxy = new KeyVaultProxy();
            options.AddPolicy(proxy, HttpPipelinePosition.PerCall);
            return (options, proxy);
        }
    }
}