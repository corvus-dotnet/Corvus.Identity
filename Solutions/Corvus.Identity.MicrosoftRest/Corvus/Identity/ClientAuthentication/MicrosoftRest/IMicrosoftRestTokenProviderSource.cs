// <copyright file="IMicrosoftRestTokenProviderSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest
{
    using Microsoft.Rest;

    /// <summary>
    /// A source of <see cref="ITokenProvider"/> objects, enabling authentication to Azure
    /// services, or any other APIs that use <c>Microsoft.Rest</c>.
    /// </summary>
    public interface IMicrosoftRestTokenProviderSource
    {
        /// <summary>
        /// Gets a <see cref="ITokenProvider"/>.
        /// </summary>
        /// <param name="scopes">
        /// The scopes for which the token is required.
        /// </param>
        /// <returns>
        /// A <see cref="ITokenProvider"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Whereas the other source types in <c>Corvus.Identity</c> for working directly with
        /// access tokens (<c>IAccessTokenSource</c>) or <c>Azure.Core</c>-style credentials
        /// (<c>IAzureTokenCredentialSource</c>) both use async in their equivalents of this
        /// method, this does not for a couple of reasons. First, <see cref="ITokenProvider"/>
        /// is inherently asynchronous anyway, so in practice, if failures are going to occur,
        /// they will happen at the point at which the token provider is first used, and not
        /// when it is obtained, so making this async would provide a misleading impression of
        /// when work is actually being done. Secondly, it's common to want to use this from
        /// process startup code, notably DI initialization, in which async is often not an
        /// option, so it would be actively unhelpful to make this async.
        /// </para>
        /// </remarks>
        ITokenProvider GetTokenProvider(string[] scopes);

        /// <summary>
        /// Gets a new <see cref="ITokenProvider"/> to replace one that seems to have stopped
        /// working.
        /// </summary>
        /// <param name="scopes">
        /// The scopes for which the token is required.
        /// </param>
        /// <returns>
        /// A <see cref="ITokenProvider"/> to use from now on.
        /// </returns>
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
        /// <see cref="GetTokenProvider"/>, making those wait until refreshed credentials have
        /// become available.
        /// </para>
        /// </remarks>
        ITokenProvider GetReplacementForFailedTokenProvider(string[] scopes);
    }
}