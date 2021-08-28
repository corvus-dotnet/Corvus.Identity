// <copyright file="ServiceIdentityServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
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
        [Obsolete("Consider using Corvus.Identity.Azure's AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString, optionally with LegacyAzureServiceTokenProviderOptions, or Corvus.Identity.MicrosoftRest's AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource instead")]
        public static IServiceCollection AddAzureManagedIdentityBasedTokenSource(
            this IServiceCollection services,
            AzureManagedIdentityTokenSourceOptions? options)
        {
            return services.AddAzureManagedIdentityBasedTokenSource(_ => options);
        }

        /// <summary>
        /// Adds an <c>AzureServiceTokenProvider</c>-based <see cref="IServiceIdentityTokenSource"/>
        /// to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getOptions">A callback method that will retrieve an <see cref="AzureManagedIdentityTokenSourceOptions" />.</param>
        /// <returns>The modified service collection.</returns>
        [Obsolete("Consider using Corvus.Identity.Azure's AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString, optionally with LegacyAzureServiceTokenProviderOptions, or Corvus.Identity.MicrosoftRest's AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource instead")]
        public static IServiceCollection AddAzureManagedIdentityBasedTokenSource(
            this IServiceCollection services,
            Func<IServiceProvider, AzureManagedIdentityTokenSourceOptions?> getOptions)
        {
            if (services.Any(s => s.ImplementationType == typeof(AzureManagedIdentityTokenSource)))
            {
                return services;
            }

            return services.AddSingleton<IServiceIdentityTokenSource>(
                sp => new AzureManagedIdentityTokenSource(getOptions(sp)?.AzureServicesAuthConnectionString));
        }
    }
}
