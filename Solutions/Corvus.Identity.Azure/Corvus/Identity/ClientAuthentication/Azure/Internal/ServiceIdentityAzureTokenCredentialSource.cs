// <copyright file="ServiceIdentityAzureTokenCredentialSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;

    /// <summary>
    /// Wraps an <see cref="IAzureTokenCredentialSource"/> as an
    /// <see cref="IServiceIdentityAzureTokenCredentialSource"/>.
    /// </summary>
    internal class ServiceIdentityAzureTokenCredentialSource : IServiceIdentityAzureTokenCredentialSource
    {
        private readonly IAzureTokenCredentialSource underlyingSource;

        /// <summary>
        /// Creates a <see cref="ServiceIdentityAzureTokenCredentialSource"/>.
        /// </summary>
        /// <param name="underlyingSource">
        /// The <see cref="IAzureTokenCredentialSource"/> to wrap as an
        /// <see cref="IServiceIdentityAzureTokenCredentialSource"/>.
        /// </param>
        public ServiceIdentityAzureTokenCredentialSource(
            IAzureTokenCredentialSource underlyingSource)
        {
            this.underlyingSource = underlyingSource;
        }

        /// <inheritdoc/>
        public ValueTask<TokenCredential> GetAccessTokenAsync() => this.GetTokenCredentialAsync(CancellationToken.None);

        /// <inheritdoc/>
        public ValueTask<TokenCredential> GetTokenCredentialAsync(CancellationToken cancellationToken)
            => this.underlyingSource.GetTokenCredentialAsync(cancellationToken);

        /// <inheritdoc/>
        public ValueTask<TokenCredential> GetReplacementForFailedTokenCredentialAsync(
            CancellationToken cancellationToken)
            => this.underlyingSource.GetReplacementForFailedTokenCredentialAsync(cancellationToken);
    }
}