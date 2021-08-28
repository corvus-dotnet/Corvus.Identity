// <copyright file="MicrosoftRestIdentityServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Identity.ClientAuthentication;
    using Corvus.Identity.ClientAuthentication.MicrosoftRest;
    using Corvus.Identity.ClientAuthentication.MicrosoftRest.Internal;

    /// <summary>
    /// DI initialization for services using Corvus.Identity.ClientAuthentication.MicrosoftRest.
    /// </summary>
    public static class MicrosoftRestIdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IServiceIdentityMicrosoftRestTokenProviderSource"/> implementation
        /// to a service collection that defers to <see cref="IServiceIdentityAccessTokenSource"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        /// <remarks>
        /// <para>
        /// This requires an implementation of <see cref="IServiceIdentityAccessTokenSource"/> to
        /// be available. This library does not know how to obtain tokens: it is an adapter that
        /// obtainstokens from the general-purpose <see cref="IServiceIdentityAccessTokenSource"/>
        /// mechanism, and wraps them as an <see cref="Microsoft.Rest.ITokenProvider"/>, so
        /// something else needs to provide the basic ability to provide the tokens being wrapped.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource(
            this IServiceCollection services)
        {
            return services.AddSingleton<IServiceIdentityMicrosoftRestTokenProviderSource, MicrosoftRestTokenProviderSource>();
        }
    }
}