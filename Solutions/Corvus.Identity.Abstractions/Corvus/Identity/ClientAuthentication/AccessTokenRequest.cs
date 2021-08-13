// <copyright file="AccessTokenRequest.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication
{
    /// <summary>
    /// Describes the characteristics required when requesting an access token.
    /// </summary>
    public struct AccessTokenRequest
    {
        /// <summary>
        /// Creates an <see cref="AccessTokenRequest"/>.
        /// </summary>
        /// <param name="scopes">The <see cref="Scopes"/>.</param>
        /// <param name="claims">The <see cref="Claims"/>.</param>
        /// <param name="authorityId">The <see cref="AuthorityId"/>.</param>
        public AccessTokenRequest(
            string[] scopes,
            string? claims = null,
            string? authorityId = null)
        {
            this.Scopes = scopes;
            this.Claims = claims;
            this.AuthorityId = authorityId;
        }

        /// <summary>
        /// Gets the scopes that determine what the token can be used for.
        /// </summary>
        public string[] Scopes { get; }

        /// <summary>
        /// Gets any additional claims that the application needs in the token, if any.
        /// </summary>
        public string? Claims { get; }

        /// <summary>
        /// Gets the authority identifier (e.g., Azure AD tenant id) that should issue the token
        /// or <c>null</c> to use the default.
        /// </summary>
        public string? AuthorityId { get; }
    }
}