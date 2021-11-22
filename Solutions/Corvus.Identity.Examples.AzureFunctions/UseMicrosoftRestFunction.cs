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
        private readonly UseWebAppManagementAsServiceIdentityWithOldSdk serviceIdClient;
        private readonly ILogger<UseMicrosoftRestFunction> logger;
        private readonly ExampleSettings settings;
        private readonly UseWebAppManagementWithIdentityFromConfigWithOldSdk configIdClient;

        /// <summary>
        /// Creates a <see cref="UseMicrosoftRestFunction"/>.
        /// </summary>
        /// <param name="settings">Example settings.</param>
        /// <param name="serviceIdClient">
        /// The client wrapper for accessing the key vault using the service identity.
        /// </param>
        /// <param name="configIdClient">
        /// The client wrapper for accessing the key vault using a configured identity.
        /// </param>
        /// <param name="logger">Logger.</param>
        public UseMicrosoftRestFunction(
            ExampleSettings settings,
            UseWebAppManagementAsServiceIdentityWithOldSdk serviceIdClient,
            UseWebAppManagementWithIdentityFromConfigWithOldSdk configIdClient,
            ILogger<UseMicrosoftRestFunction> logger)
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
        [FunctionName("UseServiceIdentityMicrosoftRestToken")]
        public async Task<IActionResult> UseServiceAzureIdentityAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            List<string> sites = await this.serviceIdClient.GetWebAppsAsync(this.settings.AzureSubscriptionId).ConfigureAwait(false);

            return new OkObjectResult(string.Join(", ", sites));
        }

        /// <summary>
        /// Function endpoint.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>
        /// A task that determines the response.
        /// </returns>
        [FunctionName("UseConfiguredMicrosoftRestToken")]
        public async Task<IActionResult> UseConfiguredAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            List<string> sites = await this.configIdClient.GetWebAppsAsync(
                this.settings.ArmClientIdentity,
                this.settings.AzureSubscriptionId)
                .ConfigureAwait(false);

            return new OkObjectResult(string.Join(", ", sites));
        }
    }
}