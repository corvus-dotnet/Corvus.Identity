// <copyright file="Startup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(typeof(Corvus.Identity.Examples.AzureFunctions.Startup))]

namespace Corvus.Identity.Examples.AzureFunctions
{
    using Corvus.Identity.ClientAuthentication.Azure;
    using Corvus.Identity.Examples.UsingAzureCore;
    using Corvus.Identity.Examples.UsingMicrosoftRest;
    using Corvus.Identity.Examples.UsingPlainTokens;

    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// DI initialization, called by Functions host on startup.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IConfiguration config = builder.GetContext().Configuration;
            ExampleSettings appSettings = config.GetSection("ExampleSettings").Get<ExampleSettings>();

            IServiceCollection services = builder.Services;

            services.AddHttpClient();

            services.AddAzureTokenCredentialSourceFromDynamicConfiguration();
            if (appSettings.ServiceIdentity is null)
            {
                services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(
                    config.Get<LegacyAzureServiceTokenProviderOptions>());
            }
            else
            {
                services.AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration(appSettings.ServiceIdentity);
            }

            services.AddMicrosoftRestAdapterForServiceIdentityAccessTokenSource();
            services.AddMicrosoftRestTokenProviderSourceDynamicConfiguration();

            services.AddSingleton(appSettings);
            services.AddSingleton<UseAzureKeyVaultAsServiceIdentityWithNewSdk>();
            services.AddSingleton<UseAzureKeyVaultWithIdentityFromConfigWithNewSdk>();
            services.AddSingleton<UseWebAppManagementAsServiceIdentityWithOldSdk>();
            services.AddSingleton<UseWebAppManagementWithIdentityFromConfigWithOldSdk>();
            services.AddSingleton<ReadResourceGroupsAsServiceIdentityWithPlainTokens>();
            services.AddSingleton<ReadResourceGroupsWithIdentityFromConfigWithPlainTokens>();
        }
    }
}