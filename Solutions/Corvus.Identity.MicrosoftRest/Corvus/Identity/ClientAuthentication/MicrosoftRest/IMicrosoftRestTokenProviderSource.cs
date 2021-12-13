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
    }
}