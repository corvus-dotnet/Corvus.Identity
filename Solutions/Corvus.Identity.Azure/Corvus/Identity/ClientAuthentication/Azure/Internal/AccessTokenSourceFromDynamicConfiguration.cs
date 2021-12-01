// <copyright file="AccessTokenSourceFromDynamicConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication;

    /// <summary>
    /// Wraps a <see cref="IAzureTokenCredentialSourceFromDynamicConfiguration"/> as an
    /// <see cref="IAccessTokenSourceFromDynamicConfiguration"/>.
    /// </summary>
    internal class AccessTokenSourceFromDynamicConfiguration : IAccessTokenSourceFromDynamicConfiguration
    {
        private readonly IAzureTokenCredentialSourceFromDynamicConfiguration tokenCredentialSource;

        /// <summary>
        /// Creates a <see cref="AccessTokenSourceFromDynamicConfiguration"/>.
        /// </summary>
        /// <param name="tokenCredentialSource">
        /// The <see cref="IAzureTokenCredentialSource"/> to wrap as an
        /// <see cref="IAccessTokenSourceFromDynamicConfiguration"/>.
        /// </param>
        public AccessTokenSourceFromDynamicConfiguration(
            IAzureTokenCredentialSourceFromDynamicConfiguration tokenCredentialSource)
        {
            this.tokenCredentialSource = tokenCredentialSource ?? throw new ArgumentNullException(nameof(tokenCredentialSource));
        }

        /// <inheritdoc/>
        public ValueTask<IAccessTokenSource> AccessTokenSourceForConfigurationAsync(
            ClientIdentityConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<IAccessTokenSource>(
                new AzureTokenCredentialAccessTokenSource(
                    new AzureTokenCredentialSourceForSpecificConfiguration(
                        configuration,
                        this.tokenCredentialSource)));
        }
    }
}
