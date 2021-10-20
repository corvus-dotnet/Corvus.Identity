﻿// <copyright file="MicrosoftRestTokenProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest
{
    using System;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication;

    using Microsoft.Rest;

    /// <summary>
    /// A token provider for <c>Microsoft.Rest</c>-based clients that obtains its tokens from
    /// <see cref="IAccessTokenSource"/>.
    /// </summary>
    public class MicrosoftRestTokenProvider : ITokenProvider
    {
        private readonly IAccessTokenSource tokenSource;
        private readonly string[] scopes;

        /// <summary>
        /// Create a <see cref="MicrosoftRestTokenProvider"/>.
        /// </summary>
        /// <param name="tokenSource">
        /// Source of tokens representing the host service's identity.
        /// </param>
        /// <param name="scope">
        /// The scope defining the resource access for which we need the tokens (e.g., the app id of
        /// Azure App generated by Easy Auth for the target service).
        /// </param>
        public MicrosoftRestTokenProvider(
            IAccessTokenSource tokenSource,
            string scope)
            : this(tokenSource, new[] { scope })
        {
        }

        /// <summary>
        /// Create a <see cref="MicrosoftRestTokenProvider"/>.
        /// </summary>
        /// <param name="tokenSource">
        /// Source of tokens representing the host service's identity.
        /// </param>
        /// <param name="scopes">
        /// The scopes defining the resource access for which we need the tokens (e.g., the app id of
        /// Azure App generated by Easy Auth for the target service).
        /// </param>
        public MicrosoftRestTokenProvider(
            IAccessTokenSource tokenSource,
            string[] scopes)
        {
            this.tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
            this.scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        }

        /// <summary>
        /// Gets an authentication header value containing an access token.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task that produces an authentication header.</returns>
        public async Task<AuthenticationHeaderValue?> GetAuthenticationHeaderAsync(CancellationToken cancellationToken)
        {
            AccessTokenDetail token = await this.tokenSource.GetAccessTokenAsync(
                new AccessTokenRequest(this.scopes),
                cancellationToken).ConfigureAwait(false);
            return new AuthenticationHeaderValue("Bearer", token.AccessToken);
        }
    }
}