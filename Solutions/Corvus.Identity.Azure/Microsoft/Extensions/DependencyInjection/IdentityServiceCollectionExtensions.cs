// <copyright file="IdentityServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Identity.ClientAuthentication.Azure;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;

    /// <summary>
    /// DI initialization for services using Corvus.Identity.Azure.
    /// </summary>
    public static class IdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IServiceIdentityAzureTokenCredentialSource"/> configured with a
        /// legacy connection string of the kind recognized by <c>AzureServiceTokenProvider</c>
        /// to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="legacyConnectionString">
        /// A connection string in the old format <c>AzureServiceTokenProvider</c> supports.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
            this IServiceCollection services,
            string legacyConnectionString)
        {
            return services.AddSingleton<IServiceIdentityAzureTokenCredentialSource>(
                new AzureTokenCredentialSource(LegacyAzureServiceTokenProviderConnectionString.ToTokenCredential(legacyConnectionString)));
        }
    }
}