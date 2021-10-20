// <copyright file="IAccessTokenSourceFromDynamicConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides <see cref="IAccessTokenSource"/> instances from
    /// <see cref="ClientIdentityConfiguration"/> supplied at runtime.
    /// </summary>
    /// <para>
    /// These tokens are just some text, and this interface makes no assumptions about these
    /// tokens' form or origin. (In practice, they are likely to have been obtained from Azure AD,
    /// but nothing in this interface requires that.)
    /// </para>
    public interface IAccessTokenSourceFromDynamicConfiguration
    {
        /// <summary>
        /// Returns an <see cref="IAzureTokenCredentialSource"/> as described by a
        /// <see cref="ClientIdentityConfiguration"/>.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="ClientIdentityConfiguration"/> describing the identity to use.
        /// </param>
        /// <param name="cancellationToken">
        /// May enable the request to be cancelled.
        /// </param>
        /// <returns>An <see cref="IAzureTokenCredentialSource"/>.</returns>
        ValueTask<IAccessTokenSource> AccessTokenSourceForConfigurationAsync(
            ClientIdentityConfiguration configuration,
            CancellationToken cancellationToken = default);
    }
}