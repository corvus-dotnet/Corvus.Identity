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
        public static IServiceCollection AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource(
            this IServiceCollection services)
        {
            return services.AddSingleton<IServiceIdentityMicrosoftRestTokenProviderSource, MicrosoftRestTokenProviderSource>();
        }
    }
}