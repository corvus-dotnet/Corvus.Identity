// <copyright file="MicrosoftRestTokenProviderSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest.Internal
{
    using Microsoft.Rest;

    /// <summary>
    /// A source of <see cref="ITokenProvider"/>s based on an <see cref="IAccessTokenSource"/>.
    /// </summary>
    internal class MicrosoftRestTokenProviderSource : IMicrosoftRestTokenProviderSource
    {
        private readonly IAccessTokenSource tokenSource;

        /// <summary>
        /// Creates a <see cref="MicrosoftRestTokenProviderSource"/>.
        /// </summary>
        /// <param name="serviceIdentityTokenSource">
        /// The source from which to obtain access tokens.
        /// </param>
        public MicrosoftRestTokenProviderSource(
            IAccessTokenSource serviceIdentityTokenSource)
        {
            this.tokenSource = serviceIdentityTokenSource;
        }

        /// <inheritdoc/>
        public ITokenProvider GetTokenProvider(string[] scopes)
            => new MicrosoftRestTokenProvider(this.tokenSource, scopes);
    }
}