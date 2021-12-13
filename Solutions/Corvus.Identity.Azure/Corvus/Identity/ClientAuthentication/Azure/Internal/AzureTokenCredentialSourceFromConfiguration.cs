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
        private static readonly ClientIdentityConfiguration DefaultClientIdentity =
            new () { IdentitySourceType = ClientIdentitySourceTypes.Managed };

        private readonly ICachingKeyVaultSecretClientFactory cachingSecretClientFactory;

        /// <summary>
        /// Creates an <see cref="AzureTokenCredentialSourceFromConfiguration"/>.
        /// </summary>
        /// <param name="secretClientFactory">
        /// Creates new <see cref="SecretClient"/> instances.
        /// </param>
        public AzureTokenCredentialSourceFromConfiguration(
            ICachingKeyVaultSecretClientFactory secretClientFactory)
        {
            this.cachingSecretClientFactory = secretClientFactory ?? throw new ArgumentNullException(nameof(secretClientFactory));
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
                    SecretClient keyVaultSecretClient = await this.cachingSecretClientFactory.GetSecretClientForAsync(
                        keyVaultConfig.VaultName,
                        keyVaultConfig.VaultClientIdentity ?? DefaultClientIdentity,
                        cancellationToken)
                        .ConfigureAwait(false);
                    Response<KeyVaultSecret> secretResponse = await keyVaultSecretClient.GetSecretAsync(
                        keyVaultConfig.SecretName,
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    secret = secretResponse.Value.Value;
                }
                else
                {
                    throw new ArgumentException(
                        "Configuration seems to want Azure AD Client ID and Secret, but not enough information provided",
                        nameof(configuration));
                }

                return new AzureTokenCredentialSource(new TestableClientSecretCredential(
                    configuration.AzureAdAppTenantId!,
                    configuration.AzureAdAppClientId!,
                    secret));
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
            return new AzureTokenCredentialSource(tokenCredential);
        }
    }
}