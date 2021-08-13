// <copyright file="IAzureTokenCredentialSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
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
        ValueTask<TokenCredential> GetAccessTokenAsync();
    }
}