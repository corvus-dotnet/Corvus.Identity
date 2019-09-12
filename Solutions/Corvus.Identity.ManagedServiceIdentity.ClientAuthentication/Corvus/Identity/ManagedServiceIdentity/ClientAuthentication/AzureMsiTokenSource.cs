// <copyright file="AzureMsiTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Enables applications running in an Azure service with an associated Managed Service
    /// Identity (MSI) to authenticate using that identity.
    /// </summary>
    public class AzureMsiTokenSource : IServiceIdentityTokenSource
    {
        private readonly IConfigurationRoot configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureMsiTokenSource"/> class.
        /// </summary>
        /// <param name="configuration">The configuration root.</param>
        public AzureMsiTokenSource(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        /// <inheritdoc />
        public Task<string> GetAccessToken(string resource)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider(this.configuration["AzureServicesAuthConnectionString"]);
            return azureServiceTokenProvider.GetAccessTokenAsync(resource);
        }
    }
}
