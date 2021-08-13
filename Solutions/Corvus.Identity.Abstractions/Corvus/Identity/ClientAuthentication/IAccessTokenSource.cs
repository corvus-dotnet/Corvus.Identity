// <copyright file="IAccessTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A source of access tokens.
    /// </summary>
    /// <para>
    /// These tokens are just some text, and this interface makes no assumptions about these
    /// tokens' form or origin. (In practice, they are likely to have been obtained from Azure AD,
    /// but nothing in this interface requires that.)
    /// </para>
    public interface IAccessTokenSource
    {
        /// <summary>
        /// Gets an access token.
        /// </summary>
        /// <param name="requiredTokenCharacteristics">
        /// Describes the scope (and optionally, other characteristics) required for the token.
        /// </param>
        /// <param name="cancellationToken">
        /// May enable the request to be cancelled.
        /// </param>
        /// <returns>
        /// A value task that produces an <see cref="AccessTokenDetail"/> containing the token
        /// value and the time at which it expires.
        /// </returns>
        /// <exception cref="AccessTokenNotIssuedException">
        /// Thrown if a token cannot be acquired for any reason.
        /// </exception>
        ValueTask<AccessTokenDetail> GetAccessTokenAsync(
            AccessTokenRequest requiredTokenCharacteristics,
            CancellationToken cancellationToken);
    }
}