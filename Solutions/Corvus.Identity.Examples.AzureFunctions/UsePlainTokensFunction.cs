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
        private readonly ReadResourceGroupsWithPlainTokens client;
        private readonly ILogger<ReadResourceGroupsWithPlainTokens> logger;
        private readonly ExampleSettings settings;

        /// <summary>
        /// Creates a <see cref="UsePlainTokensFunction"/>.
        /// </summary>
        /// <param name="settings">Example settings.</param>
        /// <param name="client">
        /// The client wrapper for accessing the key vault.
        /// </param>
        /// <param name="logger">Logger.</param>
        public UsePlainTokensFunction(
            ExampleSettings settings,
            ReadResourceGroupsWithPlainTokens client,
            ILogger<ReadResourceGroupsWithPlainTokens> logger)
        {
            this.client = client ?? throw new System.ArgumentNullException(nameof(client));
            this.logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Function endpoint.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>
        /// A task that determines the response.
        /// </returns>
        [FunctionName("UsePlainTokens")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            string result = await this.client.GetResourceGroupsAsync(this.settings.AzureSubscriptionId).ConfigureAwait(false);

            return new OkObjectResult(result);
        }
    }
}