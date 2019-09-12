// <copyright file="ServiceIdentityServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System.Linq;
    using Corvus.Identity.ManagedServiceIdentity.ClientAuthentication;

    /// <summary>
    /// DI initialization for services using a Managed Service Identity (MSI).
    /// </summary>
    public static class ServiceIdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Workflow Engine client to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddAzureMsiBasedTokenSource(
            this IServiceCollection services)
        {
            if (services.Any(s => s.ImplementationType == typeof(AzureMsiTokenSource)))
            {
                return services;
            }

            return services.AddSingleton<IServiceIdentityTokenSource, AzureMsiTokenSource>();
        }
    }
}
