// <copyright file="ReadResourceGroupsWithIdentityFromConfigWithPlainTokens.cs" company="Endjin Limited">
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
    using Corvus.Identity.ClientAuthentication.Azure;

    /// <summary>
    /// A service that reads a list of resource groups from the ARM API using an identity specified
    /// as a <see cref="ClientIdentityConfiguration"/>.
    /// </summary>
    public class ReadResourceGroupsWithIdentityFromConfigWithPlainTokens
    {
        private readonly IAccessTokenSourceFromDynamicConfiguration tokenSourceFromConfig;
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// Creates a <see cref="ReadResourceGroupsAsServiceIdentityWithPlainTokens"/>.
        /// </summary>
        /// <param name="tokenSourceFromConfig">
        /// The source from which to obtain tokens representing the service's identity.
        /// </param>
        /// <param name="httpClientFactory">
        /// A source for HttpClient instances.
        /// </param>
        public ReadResourceGroupsWithIdentityFromConfigWithPlainTokens(
            IAccessTokenSourceFromDynamicConfiguration tokenSourceFromConfig,
            IHttpClientFactory httpClientFactory)
        {
            this.tokenSourceFromConfig = tokenSourceFromConfig ?? throw new ArgumentNullException(nameof(tokenSourceFromConfig));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Reads "me" endpoint.
        /// </summary>
        /// <param name="identity">
        /// Configuration describing the identity with which to connect to Azure Key Vault.
        /// </param>
        /// <param name="subscriptionId">The subscription for which to list resource groups.</param>
        /// <returns>
        /// A task that produces the contents of the response from ARM.
        /// </returns>
        public async Task<string> GetResourceGroupsAsync(
            ClientIdentityConfiguration identity,
            string subscriptionId)
        {
            using HttpClient http = this.httpClientFactory.CreateClient("ReadGraphWithPlainTokens");

            IAccessTokenSource tokenSource = await this.tokenSourceFromConfig.AccessTokenSourceForConfigurationAsync(identity).ConfigureAwait(false);
            AccessTokenDetail accessToken = await tokenSource.GetAccessTokenAsync(
                new AccessTokenRequest(new[] { "https://management.azure.com//.default" }),
                CancellationToken.None).ConfigureAwait(false);

            string accessTokenText = accessToken.AccessToken;
            var req = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://management.azure.com/subscriptions/{subscriptionId}/resourcegroups?api-version=2021-04-01"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenText);

            HttpResponseMessage response = await http.SendAsync(req).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}