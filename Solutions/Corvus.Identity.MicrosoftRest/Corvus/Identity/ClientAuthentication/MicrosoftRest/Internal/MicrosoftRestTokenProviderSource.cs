// <copyright file="MicrosoftRestTokenProviderSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest.Internal
{
    using System;

    using Microsoft.Rest;

    /// <summary>
    /// A source of <see cref="ITokenProvider"/>s based on an <see cref="IAccessTokenSource"/>.
    /// </summary>
    internal class MicrosoftRestTokenProviderSource : IMicrosoftRestTokenProviderSource
    {
        private readonly IAccessTokenSource tokenSource;
        private readonly Action? invalidate;

        /// <summary>
        /// Creates a <see cref="MicrosoftRestTokenProviderSource"/>.
        /// </summary>
        /// <param name="serviceIdentityTokenSource">
        /// The source from which to obtain access tokens.
        /// </param>
        /// <param name="invalidate">
        /// Optional callback enabling this provider to invalidate any cached copy of the
        /// credentials it relies on.
        /// </param>
        public MicrosoftRestTokenProviderSource(
            IAccessTokenSource serviceIdentityTokenSource,
            Action? invalidate)
        {
            this.tokenSource = serviceIdentityTokenSource;
            this.invalidate = invalidate;
        }

        /// <inheritdoc/>
        public ITokenProvider GetReplacementForFailedTokenProvider(string[] scopes)
        {
            if (this.invalidate is not null)
            {
                this.invalidate();
            }

            return this.GetTokenProvider(scopes);
        }

        /// <inheritdoc/>
        public ITokenProvider GetTokenProvider(string[] scopes)
            => new MicrosoftRestTokenProvider(this.tokenSource, scopes);
    }
}