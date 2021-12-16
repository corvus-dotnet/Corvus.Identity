// <copyright file="MicrosoftRestTokenProviderSourceFromDynamicConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest.Internal
{
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.Azure;

    /// <summary>
    /// Implementation of <see cref="IMicrosoftRestTokenProviderSourceFromDynamicConfiguration"/>.
    /// </summary>
    internal class MicrosoftRestTokenProviderSourceFromDynamicConfiguration : IMicrosoftRestTokenProviderSourceFromDynamicConfiguration
    {
        private readonly IAccessTokenSourceFromDynamicConfiguration tokenSourceFromDynamicConfiguration;

        /// <summary>
        /// Creates a <see cref="MicrosoftRestTokenProviderSourceFromDynamicConfiguration"/>.
        /// </summary>
        /// <param name="tokenSourceFromDynamicConfiguration">
        /// The source from which to obtain access tokens.
        /// </param>
        public MicrosoftRestTokenProviderSourceFromDynamicConfiguration(
            IAccessTokenSourceFromDynamicConfiguration tokenSourceFromDynamicConfiguration)
        {
            this.tokenSourceFromDynamicConfiguration = tokenSourceFromDynamicConfiguration;
        }

        /// <inheritdoc/>
        public void InvalidateFailedTokenProviderSource(ClientIdentityConfiguration configuration)
        {
            this.tokenSourceFromDynamicConfiguration.InvalidateFailedAccessToken(configuration);
        }

        /// <inheritdoc/>
        public async ValueTask<IMicrosoftRestTokenProviderSource> TokenProviderSourceForConfigurationAsync(
            ClientIdentityConfiguration configuration, CancellationToken cancellationToken)
        {
            IAccessTokenSource tokenSource = await this.tokenSourceFromDynamicConfiguration.AccessTokenSourceForConfigurationAsync(
                configuration, cancellationToken)
                .ConfigureAwait(false);
            return new MicrosoftRestTokenProviderSource(
                tokenSource,
                () => this.InvalidateFailedTokenProviderSource(configuration));
        }
    }
}