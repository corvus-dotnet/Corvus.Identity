// <copyright file="ReadResourceGroupsWithPlainTokens.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Examples.UsingPlainTokens
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Identity.ClientAuthentication;

    /// <summary>
    /// A service that reads from the graph API.
    /// </summary>
    public class ReadResourceGroupsWithPlainTokens
    {
        private readonly IServiceIdentityAccessTokenSource tokenSource;
        private readonly IHttpClientFactory httpClientFactory;
        private AccessTokenDetail? lastFetchedAccessToken;

        /// <summary>
        /// Creates a <see cref="ReadResourceGroupsWithPlainTokens"/>.
        /// </summary>
        /// <param name="tokenSource">
        /// The source from which to obtain tokens representing the service's identity.
        /// </param>
        /// <param name="httpClientFactory">
        /// A source for HttpClient instances.
        /// </param>
        public ReadResourceGroupsWithPlainTokens(
            IServiceIdentityAccessTokenSource tokenSource,
            IHttpClientFactory httpClientFactory)
        {
            this.tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Reads "me" endpoint.
        /// </summary>
        /// <param name="subscriptionId">The subscription for which to list resource groups.</param>
        /// <returns>
        /// A task that produces the contents of the response from ARM.
        /// </returns>
        public async Task<string> GetResourceGroupsAsync(string subscriptionId)
        {
            using HttpClient http = this.httpClientFactory.CreateClient("ReadGraphWithPlainTokens");

            if (!this.lastFetchedAccessToken.HasValue ||
                ((this.lastFetchedAccessToken.Value.ExpiresOn - DateTimeOffset.UtcNow) < TimeSpan.FromMinutes(1)))
            {
                this.lastFetchedAccessToken = await this.tokenSource.GetAccessTokenAsync(
                    new AccessTokenRequest(new[] { "https://management.azure.com//.default" }),
                    CancellationToken.None).ConfigureAwait(false);
            }

            string accessTokenText = this.lastFetchedAccessToken.Value.AccessToken;
            var req = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://management.azure.com/subscriptions/{subscriptionId}/resourcegroups?api-version=2021-04-01"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenText);

            HttpResponseMessage response = await http.SendAsync(req).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}