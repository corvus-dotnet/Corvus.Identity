// <copyright file="AzureTokenCredentialSourceFromConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Identity.Certificates;
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
        private readonly ICertificateFromConfiguration certificateSource;

        /// <summary>
        /// Creates an <see cref="AzureTokenCredentialSourceFromConfiguration"/>.
        /// </summary>
        /// <param name="secretClientFactory">
        /// Creates new <see cref="SecretClient"/> instances.
        /// </param>
        /// <param name="secretCache">
        /// Provides caching to avoid repeated lookups in key vault.
        /// </param>
        /// <param name="certificateSource">
        /// Retrieves certificates.
        /// </param>
        public AzureTokenCredentialSourceFromConfiguration(
            IKeyVaultSecretClientFactory secretClientFactory,
            IKeyVaultSecretCache secretCache,
            ICertificateFromConfiguration certificateSource)
        {
            this.secretClientFactory = secretClientFactory ?? throw new ArgumentNullException(nameof(secretClientFactory));
            this.secretCache = secretCache;
            this.certificateSource = certificateSource;
        }

        /// <inheritdoc/>
        public async ValueTask<IAzureTokenCredentialSource> CredentialSourceForConfigurationAsync(
            ClientIdentityConfiguration configuration,
            CancellationToken cancellationToken)
        {
            string? validationMessage = ClientIdentityConfigurationValidation.Validate(
                configuration,
                out ClientIdentitySourceTypes identitySourceType);

            if (validationMessage is not null)
            {
                throw new ArgumentException(
                    "Invalid ClientIdentityConfiguration: " + validationMessage,
                    nameof(configuration));
            }

            if (identitySourceType == ClientIdentitySourceTypes.ClientIdAndSecret)
            {
                return await this.GetTokenCredentialSourceForAdAppWithClientSecret(configuration, cancellationToken).ConfigureAwait(false);
            }

            if (identitySourceType == ClientIdentitySourceTypes.ClientIdAndCertificate)
            {
                return await this.GetTokenCredentialSourceForAdAppWithClientCertificate(configuration, cancellationToken).ConfigureAwait(false);
            }

            TokenCredential tokenCredential = identitySourceType switch
            {
                ClientIdentitySourceTypes.SystemAssignedManaged => new ManagedIdentityCredential(),
                ClientIdentitySourceTypes.UserAssignedManaged => new ManagedIdentityCredential(configuration.ManagedIdentityClientId),
                ClientIdentitySourceTypes.AzureIdentityDefaultAzureCredential => new DefaultAzureCredential(),
                ClientIdentitySourceTypes.AzureCli => new AzureCliCredential(),
                ClientIdentitySourceTypes.VisualStudio => new VisualStudioCredential(),
                ClientIdentitySourceTypes.VisualStudioCode => new VisualStudioCodeCredential(),

                _ => throw new ArgumentException(
                    $"Unsupported IdentitySourceType: {identitySourceType}",
                    nameof(configuration)),
            };
            return new AzureTokenCredentialSource(tokenCredential, null);
        }

        /// <inheritdoc/>
        public void InvalidateFailedAccessToken(ClientIdentityConfiguration configuration)
        {
            if (configuration.AzureAdAppClientSecretInKeyVault is KeyVaultSecretConfiguration keyVaultConfig)
            {
                this.InvalidateKeyVaultSecrets(keyVaultConfig);
            }
        }

        private async Task<IAzureTokenCredentialSource> GetTokenCredentialSourceForAdAppWithClientCertificate(ClientIdentityConfiguration configuration, CancellationToken cancellationToken)
        {
            X509Certificate2 certificate = await this.certificateSource.CertificateForConfigurationAsync(configuration.AzureAdAppClientCertificate!).ConfigureAwait(false);

            // TO DO: Implement renewal callback.
            return new AzureTokenCredentialSource(new TestableClientCertificateCredential(configuration.AzureAdAppTenantId!, configuration.AzureAdAppClientId!, certificate), null);
        }

        private async ValueTask<IAzureTokenCredentialSource> GetTokenCredentialSourceForAdAppWithClientSecret(
            ClientIdentityConfiguration configuration, CancellationToken cancellationToken)
        {
            string secret;
            if (configuration.AzureAdAppClientSecretInKeyVault is KeyVaultSecretConfiguration keyVaultConfig)
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
                        this.InvalidateKeyVaultSecrets(keyVaultConfig);
                        IAzureTokenCredentialSource result = await this.CredentialSourceForConfigurationAsync(
                            configuration, cancellationToken).ConfigureAwait(false);
                        return await result.GetTokenCredentialAsync(cancellationToken).ConfigureAwait(false);
                    });
            }
            else
            {
                secret = configuration.AzureAdAppClientSecretPlainText!;
            }

            return new AzureTokenCredentialSource(
                new TestableClientSecretCredential(
                    configuration.AzureAdAppTenantId!,
                    configuration.AzureAdAppClientId!,
                    secret),
                null);
        }

        private void InvalidateKeyVaultSecrets(
            KeyVaultSecretConfiguration keyVaultSecretConfiguration)
        {
            this.secretCache.InvalidateSecret(
                keyVaultSecretConfiguration.VaultName,
                keyVaultSecretConfiguration.SecretName,
                keyVaultSecretConfiguration.VaultClientIdentity);
            if (keyVaultSecretConfiguration.VaultClientIdentity?.AzureAdAppClientSecretInKeyVault is KeyVaultSecretConfiguration childSecretConfiguration)
            {
                this.InvalidateKeyVaultSecrets(childSecretConfiguration);
            }
        }
    }
}