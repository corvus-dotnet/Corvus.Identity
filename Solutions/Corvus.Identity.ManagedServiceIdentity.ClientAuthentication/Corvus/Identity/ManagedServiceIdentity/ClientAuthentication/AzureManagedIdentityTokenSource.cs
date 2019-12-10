// <copyright file="AzureManagedIdentityTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Services.AppAuthentication;

    /// <summary>
    /// Enables applications running in an Azure service with an associated Managed Identity to
    /// authenticate using that identity.
    /// </summary>
    /// <remarks>
    /// To use this, call the <see cref="Microsoft.Extensions.DependencyInjection.ServiceIdentityServiceCollectionExtensions.AddAzureManagedIdentityBasedTokenSource(Microsoft.Extensions.DependencyInjection.IServiceCollection, AzureManagedIdentityTokenSourceOptions)"/>
    /// method during dependency injection initialization.
    /// </remarks>
    internal class AzureManagedIdentityTokenSource : IServiceIdentityTokenSource
    {
        private readonly AzureServiceTokenProvider azureServiceTokenProvider;
        private AzureServiceTokenProvider.TokenCallback keyVaultTokenCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureManagedIdentityTokenSource"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string, or null.</param>
        internal AzureManagedIdentityTokenSource(string connectionString)
        {
            this.azureServiceTokenProvider = new AzureServiceTokenProvider(connectionString);
        }

        /// <inheritdoc />
        public Task<string> GetAccessToken(string resource) => this.azureServiceTokenProvider.GetAccessTokenAsync(resource);

        /// <inheritdoc />
        public Task<string> GetAccessTokenSpecifyingAuthority(string authority, string resource, string scope)
        {
            // We added this overload because the KeyVaultClient uses it, which is much the same
            // reason that the AzureServiceTokenProvider provides specialized support through its
            // KeyVaultTokenCallback property. And the reason the KeyVaultClient gets special
            // handling is that it inspects the WWW-Authenticate that comes back on a 401, and
            // passes whatever that header specifies in as the authority and resource arguments here.
            // This not how most AutoRest-generated clients do it - normally they ignore any
            // WWW-Authenticate headers returned with a 401, and just presume that the client knows
            // the correct authority to use.

            // The AzureServiceTokenProvider.KeyVaultTokenCallback property allocates a new closure
            // and a new delegate every time you fetch it (in version 1.3.1.0 at any rate), so we
            // want to cache it, rather than forcing an allocation every time. We do it lazily
            // because not everything uses this overload. (We added it to support the
            // KeyVaultClient, which the AzureServiceTokenProvider handles as a special case.)
            if (this.keyVaultTokenCallback == null)
            {
                this.keyVaultTokenCallback = this.azureServiceTokenProvider.KeyVaultTokenCallback;
            }

            return this.keyVaultTokenCallback(authority, resource, scope);
        }
    }
}
