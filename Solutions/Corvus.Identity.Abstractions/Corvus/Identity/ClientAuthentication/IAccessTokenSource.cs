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

        /// <summary>
        /// Gets a new <see cref="AccessTokenDetail"/> to replace one that seems to have stopped
        /// working.
        /// </summary>
        /// <param name="failedToken">
        /// The token that no longer works.
        /// </param>
        /// <param name="cancellationToken">
        /// May enable the request to be cancelled.
        /// </param>
        /// <returns>A a token credential to use from now on.</returns>
        /// <remarks>
        /// <para>
        /// Some sources of access tokens will become invalid under certain circumstances. For
        /// example, when using ClientID/ClientSecret credentials to authenticate as a service
        /// principle, the secret will expire at some point. With short key rotation cycles, this
        /// can happen fairly frequently, but in any case it will always happen eventually.
        /// </para>
        /// <para>
        /// This method enables the application to obtain updated credentials. It also enables the
        /// <see cref="IAccessTokenSource"/> implementation to know that the credentials
        /// in question are no longer valid. Implementations that cache credentials can choose to
        /// stop handing out the now-failed cached credentials to any futher calls to
        /// <see cref="GetAccessTokenAsync"/>, making those wait until refreshed credentials have
        /// become available.
        /// </para>
        /// </remarks>
        ValueTask<AccessTokenDetail> GetReplacementForFailedAccessTokenAsync(
            AccessTokenDetail failedToken,
            CancellationToken cancellationToken);
    }
}