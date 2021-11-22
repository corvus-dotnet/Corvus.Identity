// <copyright file="IMicrosoftRestTokenProviderSourceFromDynamicConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest
{
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure;

    /// <summary>
    /// Provides <see cref="IMicrosoftRestTokenProviderSource"/> instances from
    /// <see cref="ClientIdentityConfiguration"/> supplied at runtime.
    /// </summary>
    public interface IMicrosoftRestTokenProviderSourceFromDynamicConfiguration
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
        ValueTask<IMicrosoftRestTokenProviderSource> TokenProviderSourceForConfigurationAsync(
            ClientIdentityConfiguration configuration,
            CancellationToken cancellationToken = default);
    }
}
