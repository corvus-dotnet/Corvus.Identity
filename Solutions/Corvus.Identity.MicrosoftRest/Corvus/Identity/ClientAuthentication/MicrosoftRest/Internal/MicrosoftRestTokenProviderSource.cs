// <copyright file="MicrosoftRestTokenProviderSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest.Internal
{
    using System.Threading.Tasks;

    using Microsoft.Rest;

    /// <summary>
    /// A source of <see cref="ITokenProvider"/>s that represent the service's identity.
    /// </summary>
    internal class MicrosoftRestTokenProviderSource : IServiceIdentityMicrosoftRestTokenProviderSource
    {
        private readonly IServiceIdentityAccessTokenSource serviceIdentityTokenSource;

        /// <summary>
        /// Creates a <see cref="MicrosoftRestTokenProviderSource"/>.
        /// </summary>
        /// <param name="serviceIdentityTokenSource">
        /// The source from which to obtain access tokens.
        /// </param>
        public MicrosoftRestTokenProviderSource(
            IServiceIdentityAccessTokenSource serviceIdentityTokenSource)
        {
            this.serviceIdentityTokenSource = serviceIdentityTokenSource;
        }

        /// <inheritdoc/>
        public ValueTask<ITokenProvider> GetTokenProviderAsync(string[] scopes)
            => new (new ServiceIdentityMicrosoftRestTokenProvider(this.serviceIdentityTokenSource, scopes));
    }
}