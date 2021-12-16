// <copyright file="UsePlainTokensFunction.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Examples.AzureFunctions
{
    using System.Threading.Tasks;

    using Corvus.Identity.Examples.UsingPlainTokens;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Function with HTTP endpoint that runs the example that uses tokens directly.
    /// </summary>
    public class UsePlainTokensFunction
    {
        private readonly ReadResourceGroupsAsServiceIdentityWithPlainTokens serviceIdClient;
        private readonly ILogger<ReadResourceGroupsAsServiceIdentityWithPlainTokens> logger;
        private readonly ExampleSettings settings;
        private readonly ReadResourceGroupsWithIdentityFromConfigWithPlainTokens configIdClient;

        /// <summary>
        /// Creates a <see cref="UsePlainTokensFunction"/>.
        /// </summary>
        /// <param name="settings">Example settings.</param>
        /// <param name="serviceIdClient">
        /// The client wrapper for reading from ARM using the service identity.
        /// </param>
        /// <param name="configIdClient">
        /// The client wrapper for reading from ARM using a configured identity.
        /// </param>
        /// <param name="logger">Logger.</param>
        public UsePlainTokensFunction(
            ExampleSettings settings,
            ReadResourceGroupsAsServiceIdentityWithPlainTokens serviceIdClient,
            ReadResourceGroupsWithIdentityFromConfigWithPlainTokens configIdClient,
            ILogger<ReadResourceGroupsAsServiceIdentityWithPlainTokens> logger)
        {
            this.serviceIdClient = serviceIdClient ?? throw new System.ArgumentNullException(nameof(serviceIdClient));
            this.logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            this.configIdClient = configIdClient;
        }

        /// <summary>
        /// Function endpoint.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>
        /// A task that determines the response.
        /// </returns>
        [FunctionName("UseServiceIdentityPlainTokens")]
        public async Task<IActionResult> UseServiceAzureIdentityAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            string result = await this.serviceIdClient.GetResourceGroupsAsync(this.settings.AzureSubscriptionId)
                .ConfigureAwait(false);

            return new OkObjectResult(result);
        }

        /// <summary>
        /// Function endpoint.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>
        /// A task that determines the response.
        /// </returns>
        [FunctionName("UseConfiguredPlainTokens")]
        public async Task<IActionResult> UseConfiguredAzureIdentity(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            string result = await this.configIdClient.GetResourceGroupsAsync(
                this.settings.KeyVaultClientIdentity,
                this.settings.AzureSubscriptionId)
                .ConfigureAwait(false);

            return new OkObjectResult(result);
        }
    }
}