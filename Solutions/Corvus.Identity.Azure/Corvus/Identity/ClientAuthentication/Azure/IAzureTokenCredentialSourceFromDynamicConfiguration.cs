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

        /// <summary>
        /// Removes any cached tokens for the specified identity. Called when an application has
        /// reason to believe that credentials are out of date, and may need underlying secrets to
        /// be reloaded.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="ClientIdentityConfiguration"/> describing the identity for which cached
        /// credentials no longer seem to be working..
        /// </param>
        void InvalidateFailedAccessToken(
            ClientIdentityConfiguration configuration);
    }
}