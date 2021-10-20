// <copyright file="ServiceIdentityAccessTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;

    /// <summary>
    /// Wraps an <see cref="IAccessTokenSource"/> as an
    /// <see cref="IServiceIdentityAccessTokenSource"/>.
    /// Wrapper for a <see cref="TokenCredential"/> that implements
    /// <see cref="IServiceIdentityAccessTokenSource"/>.
    /// </summary>
    internal class ServiceIdentityAccessTokenSource : IServiceIdentityAccessTokenSource
    {
        private readonly IAccessTokenSource tokenCredentialSource;

        /// <summary>
        /// Creates a <see cref="AzureTokenCredentialAccessTokenSource"/>.
        /// </summary>
        /// <param name="tokenCredentialSource">The source of Azure token credentials to wrap.</param>
        public ServiceIdentityAccessTokenSource(
            IAccessTokenSource tokenCredentialSource)
        {
            this.tokenCredentialSource = tokenCredentialSource ?? throw new ArgumentNullException(nameof(tokenCredentialSource));
        }

        /// <inheritdoc/>
        public ValueTask<AccessTokenDetail> GetAccessTokenAsync(
            AccessTokenRequest requiredTokenCharacteristics,
            CancellationToken cancellationToken)
            => this.tokenCredentialSource.GetAccessTokenAsync(requiredTokenCharacteristics, cancellationToken);

        /// <inheritdoc/>
        public ValueTask<AccessTokenDetail> GetReplacementForFailedAccessTokenAsync(
            AccessTokenDetail failedToken,
            CancellationToken cancellationToken)
            => this.tokenCredentialSource.GetReplacementForFailedAccessTokenAsync(failedToken, cancellationToken);
    }
}