// <copyright file="AzureTokenCredentialSourceFromConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure;
    using global::Azure.Core;
    using global::Azure.Identity;
    using global::Azure.Security.KeyVault.Secrets;

    /// <summary>
    /// Implementation of <see cref="IAzureTokenCredentialSourceFromDynamicConfiguration"/>.
    /// </summary>
    internal class AzureTokenCredentialSourceFromConfiguration : IAzureTokenCredentialSourceFromDynamicConfiguration
    {
        private readonly IKeyVaultSecretClientFactory secretClientFactory;
        private readonly IKeyVaultSecretCache secretCache;

        /// <summary>
        /// Creates an <see cref="AzureTokenCredentialSourceFromConfiguration"/>.
        /// </summary>
        /// <param name="secretClientFactory">
        /// Creates new <see cref="SecretClient"/> instances.
        /// </param>
        /// <param name="secretCache">
        /// Provides caching to avoid repeated lookups in key vault.
        /// </param>
        public AzureTokenCredentialSourceFromConfiguration(
            IKeyVaultSecretClientFactory secretClientFactory,
            IKeyVaultSecretCache secretCache)
        {
            this.secretClientFactory = secretClientFactory ?? throw new ArgumentNullException(nameof(secretClientFactory));
            this.secretCache = secretCache;
        }

        /// <inheritdoc/>
        public async ValueTask<IAzureTokenCredentialSource> CredentialSourceForConfigurationAsync(
            ClientIdentityConfiguration configuration,
            CancellationToken cancellationToken)
        {
            ClientIdentitySourceTypes? identitySourceType = configuration.IdentitySourceType;

            if (identitySourceType is null)
            {
                if (!string.IsNullOrWhiteSpace(configuration.AzureAdAppClientId))
                {
                    // TODO: there are other possibilities, and we should also detect conflicting config,
                    identitySourceType = ClientIdentitySourceTypes.ClientIdAndSecret;
                }
            }

            if (identitySourceType == ClientIdentitySourceTypes.ClientIdAndSecret)
            {
                string secret;
                if (!string.IsNullOrWhiteSpace(configuration.AzureAdAppClientSecretPlainText))
                {
                    secret = configuration.AzureAdAppClientSecretPlainText!;
                }
                else if (configuration.AzureAdAppClientSecretInKeyVault is KeyVaultSecretConfiguration keyVaultConfig)
                {
                    if (this.secretCache.TryGetSecret(
                        keyVaultConfig.VaultName,
                        keyVaultConfig.SecretName,
                        keyVaultConfig.VaultClientIdentity,
                        out string? secretIfAvailable))
                    {
                        secret = secretIfAvailable;
                    }
                    else
                    {
                        TokenCredential keyVaultCredential = keyVaultConfig.VaultClientIdentity is not null
                            ? await (await this.CredentialSourceForConfigurationAsync(keyVaultConfig.VaultClientIdentity, cancellationToken).ConfigureAwait(false)).GetTokenCredentialAsync(cancellationToken).ConfigureAwait(false)
                            : new ManagedIdentityCredential();
                        SecretClient keyVaultSecretClient = this.secretClientFactory.GetSecretClientFor(
                            keyVaultConfig.VaultName, keyVaultCredential);
                        Response<KeyVaultSecret> secretResponse = await keyVaultSecretClient.GetSecretAsync(keyVaultConfig.SecretName, cancellationToken: cancellationToken).ConfigureAwait(false);

                        secret = secretResponse.Value.Value;
                        this.secretCache.AddSecret(
                            keyVaultConfig.VaultName,
                            keyVaultConfig.SecretName,
                            keyVaultConfig.VaultClientIdentity,
                            secret);
                    }

                    return new AzureTokenCredentialSource(
                        new TestableClientSecretCredential(
                            configuration.AzureAdAppTenantId!,
                            configuration.AzureAdAppClientId!,
                            secret),
                        async cancellationToken =>
                        {
                            // This gets called if the client application believes that the
                            // TokenCredential we gave it before is no longer valid. That can
                            // happen in key rotation situations. So we flush cached copies of
                            // the secret for these credentials, and also any nested credentials
                            // that were used in the population of these ones, and then try again.
                            RecursivelyFlushCacheEntries(keyVaultConfig);
                            IAzureTokenCredentialSource result = await this.CredentialSourceForConfigurationAsync(
                                configuration, cancellationToken).ConfigureAwait(false);
                            return await result.GetTokenCredentialAsync(cancellationToken).ConfigureAwait(false);

                            void RecursivelyFlushCacheEntries(KeyVaultSecretConfiguration keyVaultSecretConfiguration)
                            {
                                this.secretCache.InvalidateSecret(
                                    keyVaultSecretConfiguration.VaultName,
                                    keyVaultSecretConfiguration.SecretName,
                                    keyVaultSecretConfiguration.VaultClientIdentity);
                                if (keyVaultSecretConfiguration.VaultClientIdentity?.AzureAdAppClientSecretInKeyVault is KeyVaultSecretConfiguration childSecretConfiguration)
                                {
                                    RecursivelyFlushCacheEntries(childSecretConfiguration);
                                }
                            }
                        });
                }
                else
                {
                    throw new ArgumentException(
                        "Configuration seems to want Azure AD Client ID and Secret, but not enough information provided",
                        nameof(configuration));
                }

                return new AzureTokenCredentialSource(
                    new TestableClientSecretCredential(
                        configuration.AzureAdAppTenantId!,
                        configuration.AzureAdAppClientId!,
                        secret),
                    null);
            }

            TokenCredential tokenCredential = identitySourceType switch
            {
                ClientIdentitySourceTypes.Managed => new ManagedIdentityCredential(),
                ClientIdentitySourceTypes.AzureIdentityDefaultAzureCredential => new DefaultAzureCredential(),
                ClientIdentitySourceTypes.ClientIdAndSecret => new TestableClientSecretCredential(
                    configuration.AzureAdAppTenantId!,
                    configuration.AzureAdAppClientId!,
                    configuration.AzureAdAppClientSecretPlainText!),

                _ => throw new ArgumentException(
                    $"Unsupported IdentitySourceType: ${identitySourceType}",
                    nameof(configuration)),
            };
            return new AzureTokenCredentialSource(tokenCredential, null);
        }
    }
}
