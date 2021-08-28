// <copyright file="IMicrosoftRestTokenProviderSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest
{
    using System.Threading.Tasks;

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
        /// A task that produces a <see cref="ITokenProvider"/>.
        /// </returns>
        ValueTask<ITokenProvider> GetTokenProviderAsync(string[] scopes);
    }
}