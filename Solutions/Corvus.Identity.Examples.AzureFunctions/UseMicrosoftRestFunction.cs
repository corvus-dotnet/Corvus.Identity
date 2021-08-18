// <copyright file="UseMicrosoftRestFunction.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Examples.AzureFunctions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Corvus.Identity.Examples.UsingMicrosoftRest;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Function with HTTP endpoint that runs the example that uses Microsoft.Rest.
    /// </summary>
    public class UseMicrosoftRestFunction
    {
        private readonly UseWebAppManagementWithOldSdk client;
        private readonly ILogger<UseMicrosoftRestFunction> logger;
        private readonly ExampleSettings settings;

        /// <summary>
        /// Creates a <see cref="UseMicrosoftRestFunction"/>.
        /// </summary>
        /// <param name="settings">Example settings.</param>
        /// <param name="client">
        /// The client wrapper for accessing the key vault.
        /// </param>
        /// <param name="logger">Logger.</param>
        public UseMicrosoftRestFunction(
            ExampleSettings settings,
            UseWebAppManagementWithOldSdk client,
            ILogger<UseMicrosoftRestFunction> logger)
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
        [FunctionName("UseMicrosoftRest")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            List<string> sites = await this.client.GetWebAppsAsync(this.settings.AzureSubscriptionId).ConfigureAwait(false);

            return new OkObjectResult(string.Join(", ", sites));
        }
    }
}