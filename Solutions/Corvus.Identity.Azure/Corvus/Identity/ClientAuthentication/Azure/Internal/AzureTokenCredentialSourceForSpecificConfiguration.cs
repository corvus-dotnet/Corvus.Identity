// <copyright file="AzureTokenCredentialSourceForSpecificConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;

    /// <summary>
    /// Implements <see cref="IAzureTokenCredentialSource"/> for a particular
    /// <see cref="ClientIdentityConfiguration"/>.
    /// </summary>
    internal class AzureTokenCredentialSourceForSpecificConfiguration : IAzureTokenCredentialSource
    {
        private readonly ClientIdentityConfiguration configuration;
        private readonly IAzureTokenCredentialSourceFromDynamicConfiguration sourceSource;
        private IAzureTokenCredentialSource? source;

        /// <summary>
        /// Creates a <see cref="AzureTokenCredentialSourceForSpecificConfiguration"/>.
        /// </summary>
        /// <param name="configuration">
        /// Configuration to present.
        /// </param>
        /// <param name="source">
        /// Provides <see cref="IAzureTokenCredentialSource"/> instances.
        /// </param>
        public AzureTokenCredentialSourceForSpecificConfiguration(
            ClientIdentityConfiguration configuration,
            IAzureTokenCredentialSourceFromDynamicConfiguration source)
        {
            this.configuration = configuration;
            this.sourceSource = source;
        }

        /// <inheritdoc/>
        public ValueTask<TokenCredential> GetAccessTokenAsync() => this.GetTokenCredentialAsync(CancellationToken.None);

        /// <inheritdoc/>
        public async ValueTask<TokenCredential> GetTokenCredentialAsync(CancellationToken cancellationToken)
        {
            IAzureTokenCredentialSource source = await this.EnsureSource(cancellationToken).ConfigureAwait(false);
            return await source.GetTokenCredentialAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<TokenCredential> GetReplacementForFailedTokenCredentialAsync(
            TokenCredential failedTokenCredential,
            CancellationToken cancellationToken)
        {
            IAzureTokenCredentialSource source = await this.EnsureSource(cancellationToken).ConfigureAwait(false);
            return await source.GetReplacementForFailedTokenCredentialAsync(failedTokenCredential, cancellationToken)
                .ConfigureAwait(false);
        }

        private async ValueTask<IAzureTokenCredentialSource> EnsureSource(CancellationToken cancellationToken)
        {
            if (this.source is null)
            {
                this.source = await this.sourceSource
                .CredentialSourceForConfigurationAsync(this.configuration, cancellationToken)
                .ConfigureAwait(false);
            }

            return this.source;
        }
    }
}