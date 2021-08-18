// <copyright file="UseWebAppManagementWithOldSdk.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Examples.UsingMicrosoftRest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication.MicrosoftRest;

    using Microsoft.Azure.Management.WebSites;
    using Microsoft.Azure.Management.WebSites.Models;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;

    /// <summary>
    /// A service that discovers web apps through ARM.
    /// </summary>
    public class UseWebAppManagementWithOldSdk
    {
        private readonly IServiceIdentityMicrosoftRestTokenProviderSource tokenProviderSource;

        /// <summary>
        /// Creates a <see cref="UseWebAppManagementWithOldSdk"/>.
        /// </summary>
        /// <param name="tokenProviderSource">
        /// The source from which to obtain <see cref="ITokenProvider"/> instances.
        /// </param>
        public UseWebAppManagementWithOldSdk(
            IServiceIdentityMicrosoftRestTokenProviderSource tokenProviderSource)
        {
            this.tokenProviderSource = tokenProviderSource ?? throw new ArgumentNullException(nameof(tokenProviderSource));
        }

        /// <summary>
        /// Gets all of the web sites in an Azure subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription for which to list sites.</param>
        /// <returns>
        /// A task that produces a list of all of the web sites in the subscription.
        /// </returns>
        public async Task<List<string>> GetWebAppsAsync(string subscriptionId)
        {
            ITokenProvider tokenProvider = await this.tokenProviderSource.GetTokenProviderAsync(
                "https://management.azure.com//.default")
                .ConfigureAwait(false);
            var credentials = new TokenCredentials(tokenProvider);
            var client = new WebSiteManagementClient(credentials)
            {
                SubscriptionId = subscriptionId,
            };

            IPage<Site> sitesPage = await client.WebApps.ListAsync().ConfigureAwait(false);
            var result = new List<string>();
            while (true)
            {
                result.AddRange(sitesPage.Select(s => s.Id));
                if (sitesPage.NextPageLink == null)
                {
                    break;
                }

                sitesPage = await client.WebApps.ListNextAsync(sitesPage.NextPageLink).ConfigureAwait(false);
            }

            return result;
        }
    }
}