// <copyright file="MicrosoftRestTokenProviderSourceExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest
{
    using System.Threading.Tasks;

    using Microsoft.Rest;

    /// <summary>
    /// Extension methods for <see cref="IMicrosoftRestTokenProviderSource"/>.
    /// </summary>
    public static class MicrosoftRestTokenProviderSourceExtensions
    {
        /// <summary>
        /// Gets a <see cref="ITokenProvider"/>.
        /// </summary>
        /// <param name="source">
        /// The source from which to obtain a token provider.
        /// </param>
        /// <param name="scope">
        /// The scope for which the token is required.
        /// </param>
        /// <returns>
        /// A task that produces a <see cref="ITokenProvider"/>.
        /// </returns>
        public static ValueTask<ITokenProvider> GetTokenProviderAsync(
            this IMicrosoftRestTokenProviderSource source,
            string scope)
            => source.GetTokenProviderAsync(new[] { scope });
    }
}