// <copyright file="IdentityServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Azure.Core;

    using Corvus.Identity.ClientAuthentication;
    using Corvus.Identity.ClientAuthentication.Azure;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;

    /// <summary>
    /// DI initialization for services using Corvus.Identity.Azure.
    /// </summary>
    public static class IdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <see cref="IServiceIdentityAzureTokenCredentialSource"/> and
        /// <see cref="IServiceIdentityAccessTokenSource"/> implementations to a service collection
        /// configured with a legacy connection string of the kind recognized by
        /// <c>AzureServiceTokenProvider</c>.
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
            return services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(LegacyAzureServiceTokenProviderConnectionString.ToTokenCredential(legacyConnectionString));
        }

        /// <summary>
        /// Adds an <see cref="IServiceIdentityAzureTokenCredentialSource"/> that returns an
        /// existing <see cref="TokenCredential"/>, and a
        /// <see cref="IServiceIdentityAccessTokenSource"/> implementation that uses this to
        /// provide plain access tokens.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="tokenCredential">
        /// The token credential that the <see cref="IServiceIdentityAzureTokenCredentialSource"/>
        /// implementation will always return.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(
            this IServiceCollection services,
            TokenCredential tokenCredential)
        {
            return services
                .AddSingleton<IServiceIdentityAzureTokenCredentialSource>(new AzureTokenCredentialSource(tokenCredential))
                .AddSingleton<IAccessTokenSource>(new AzureTokenCredentialAccessTokenSource(tokenCredential));
        }
    }
}