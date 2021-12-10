// <copyright file="IAzureTokenCredentialSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;

    /// <summary>
    /// A source of <see cref="TokenCredential"/> objects, enabling authentication to Azure
    /// services, or any other APIs that use <c>Azure.Core</c>.
    /// </summary>
    public interface IAzureTokenCredentialSource
    {
        /// <summary>
        /// Gets a <see cref="TokenCredential"/>.
        /// </summary>
        /// <returns>
        /// A task that produces a <see cref="TokenCredential"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method was erroneously given the same name as
        /// <see cref="IAccessTokenSource.GetAccessTokenAsync(AccessTokenRequest, CancellationToken)"/>.
        /// It should have been named <see cref="GetTokenCredentialAsync"/>, and new code should use that instead.
        /// </para>
        /// </remarks>
        [Obsolete("This method was misnamed in v2 of this library. Use the functionally identical GetTokenCredentialAsync instead.")]
        ValueTask<TokenCredential> GetAccessTokenAsync();

        /// <summary>
        /// Gets a <see cref="TokenCredential"/>.
        /// </summary>
        /// <param name="cancellationToken">
        /// May enable the request to be cancelled.
        /// </param>
        /// <returns>
        /// A task that produces a <see cref="TokenCredential"/>.
        /// </returns>
        ValueTask<TokenCredential> GetTokenCredentialAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a new <see cref="TokenCredential"/> to replace one that seems to have stopped
        /// working.
        /// </summary>
        /// <param name="cancellationToken">
        /// May enable the request to be cancelled.
        /// </param>
        /// <returns>A a token credential to use from now on.</returns>
        /// <remarks>
        /// <para>
        /// Some kinds of token credential will become invalid under certain circumstances. For
        /// example, when using ClientID/ClientSecret credentials to authenticate as a service
        /// principle, the secret will expire at some point. With short key rotation cycles, this
        /// can happen fairly frequently, but in any case it will always happen eventually.
        /// </para>
        /// <para>
        /// This method enables the application to obtain updated credentials. It also enables the
        /// <see cref="IAzureTokenCredentialSource"/> implementation to know that the credentials
        /// in question are no longer valid. Implementations that cache credentials can choose to
        /// stop handing out the now-failed cached credentials to any futher calls to
        /// <see cref="GetAccessTokenAsync"/>, making those wait until refreshed credentials have
        /// become available.
        /// </para>
        /// </remarks>
        ValueTask<TokenCredential> GetReplacementForFailedTokenCredentialAsync(
            CancellationToken cancellationToken = default);
    }
}