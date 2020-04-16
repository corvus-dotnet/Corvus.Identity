// <copyright file="ServiceIdentityServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System.Linq;
    using Corvus.Identity.ManagedServiceIdentity.ClientAuthentication;

    /// <summary>
    /// DI initialization for services using a Managed Identity.
    /// </summary>
    public static class ServiceIdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <c>AzureServiceTokenProvider</c>-based <see cref="IServiceIdentityTokenSource"/>
        /// to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">Configuration for the token source.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddAzureManagedIdentityBasedTokenSource(
            this IServiceCollection services,
            AzureManagedIdentityTokenSourceOptions? options = null)
        {
            if (services.Any(s => s.ImplementationType == typeof(AzureManagedIdentityTokenSource)))
            {
                return services;
            }

            return services.AddSingleton<IServiceIdentityTokenSource>(
                _ => new AzureManagedIdentityTokenSource(options?.AzureServicesAuthConnectionString));
        }
    }
}
