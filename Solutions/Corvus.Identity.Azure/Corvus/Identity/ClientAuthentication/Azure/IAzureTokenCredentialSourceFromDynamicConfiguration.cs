// <copyright file="IAzureTokenCredentialSourceFromDynamicConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides <see cref="IAzureTokenCredentialSource"/> instances from
    /// <see cref="ClientIdentityConfiguration"/> supplied at runtime.
    /// </summary>
    public interface IAzureTokenCredentialSourceFromDynamicConfiguration
    {
        /// <summary>
        /// Returns an <see cref="IAzureTokenCredentialSource"/> as described by a
        /// <see cref="ClientIdentityConfiguration"/>.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="ClientIdentityConfiguration"/> describing the identity to use.
        /// </param>
        /// <param name="cancellationToken">
        /// May enable the request to be cancelled.
        /// </param>
        /// <returns>An <see cref="IAzureTokenCredentialSource"/>.</returns>
        ValueTask<IAzureTokenCredentialSource> CredentialSourceForConfigurationAsync(
            ClientIdentityConfiguration configuration,
            CancellationToken cancellationToken = default);
    }
}