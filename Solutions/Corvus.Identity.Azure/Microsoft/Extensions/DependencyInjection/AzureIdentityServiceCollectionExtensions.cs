// <copyright file="AzureIdentityServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Identity.ClientAuthentication;
    using Corvus.Identity.ClientAuthentication.Azure;
    using Corvus.Identity.ClientAuthentication.Azure.Internal;

    using global::Azure.Core;

    /// <summary>
    /// DI initialization for services using Corvus.Identity.Azure.
    /// </summary>
    public static class AzureIdentityServiceCollectionExtensions
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
        /// Adds <see cref="IServiceIdentityAzureTokenCredentialSource"/> and
        /// <see cref="IServiceIdentityAccessTokenSource"/> implementations to a service collection
        /// configured with a legacy connection string of the kind recognized by
        /// <c>AzureServiceTokenProvider</c>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="legacyConnectionStringOptions">
        /// A configuration object containing a connection string in the old format <c>AzureServiceTokenProvider</c> supports.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
            this IServiceCollection services,
            LegacyAzureServiceTokenProviderOptions? legacyConnectionStringOptions)
        {
            string connectionString = legacyConnectionStringOptions?.AzureServicesAuthConnectionString ?? string.Empty;
            return services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(
                LegacyAzureServiceTokenProviderConnectionString.ToTokenCredential(connectionString));
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
            AzureTokenCredentialSource source = new (tokenCredential);
            return services
                .AddSingleton<IServiceIdentityAzureTokenCredentialSource>(new ServiceIdentityAzureTokenCredentialSource(source))
                .AddSingleton<IServiceIdentityAccessTokenSource, ServiceIdentityAccessTokenSource>();
        }

        /// <summary>
        /// Adds an <see cref="IServiceIdentityAzureTokenCredentialSource"/> and a
        /// <see cref="IServiceIdentityAccessTokenSource"/> implementation that provide credentials
        /// for the identity described in a <see cref="ClientIdentityConfiguration"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">
        /// A <see cref="ClientIdentityConfiguration"/> describing the identity to use as the
        /// ambient service identity.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration(
            this IServiceCollection services,
            ClientIdentityConfiguration configuration)
        {
            return services
                .AddAzureTokenCredentialSourceFromDynamicConfiguration()
                .AddSingleton<IServiceIdentityAzureTokenCredentialSource>(sp =>
                    new ServiceIdentityAzureTokenCredentialSource(
                        new AzureTokenCredentialSourceForSpecificConfiguration(
                            configuration,
                            sp.GetRequiredService<IAzureTokenCredentialSourceFromDynamicConfiguration>())))
                .AddSingleton<IServiceIdentityAccessTokenSource, ServiceIdentityAccessTokenSource>();
        }

        /// <summary>
        /// Makes an <see cref="IAzureTokenCredentialSourceFromDynamicConfiguration"/> implementation
        /// available.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddAzureTokenCredentialSourceFromDynamicConfiguration(
            this IServiceCollection services)
        {
            return services
                .AddSingleton<IAzureTokenCredentialSourceFromDynamicConfiguration, AzureTokenCredentialSourceFromConfiguration>()
                .AddSingleton<IAccessTokenSourceFromDynamicConfiguration, AccessTokenSourceFromDynamicConfiguration>()
                .AddSingleton<IKeyVaultSecretClientFactory, KeyVaultSecretClientFactory>();
        }
    }
}