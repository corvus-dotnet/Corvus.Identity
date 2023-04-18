// <copyright file="UseAzureIdentityFunction.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Examples.AzureFunctions
{
    using System;
    using System.Threading.Tasks;

    using Corvus.Identity.Examples.UsingAzureCore;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Function with HTTP endpoint that runs the example that uses Azure.Identity.
    /// </summary>
    public class UseAzureIdentityFunction
    {
        private readonly UseAzureKeyVaultAsServiceIdentityWithNewSdk serviceIdClient;
        private readonly ILogger<UseAzureIdentityFunction> logger;
        private readonly ExampleSettings settings;
        private readonly UseAzureKeyVaultWithIdentityFromConfigWithNewSdk configIdClient;

        /// <summary>
        /// Creates a <see cref="UseAzureIdentityFunction"/>.
        /// </summary>
        /// <param name="settings">Example settings.</param>
        /// <param name="serviceIdClient">
        /// The client wrapper for accessing the key vault using the service identity.
        /// </param>
        /// <param name="configIdClient">
        /// The client wrapper for accessing the key vault using a configured identity.
        /// </param>
        /// <param name="logger">Logger.</param>
        public UseAzureIdentityFunction(
            ExampleSettings settings,
            UseAzureKeyVaultAsServiceIdentityWithNewSdk serviceIdClient,
            UseAzureKeyVaultWithIdentityFromConfigWithNewSdk configIdClient,
            ILogger<UseAzureIdentityFunction> logger)
        {
            this.serviceIdClient = serviceIdClient ?? throw new ArgumentNullException(nameof(serviceIdClient));
            this.configIdClient = configIdClient ?? throw new ArgumentNullException(nameof(serviceIdClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Function endpoint.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>
        /// A task that determines the response.
        /// </returns>
        [FunctionName("UseServiceAzureIdentity")]
        public async Task<IActionResult> UseServiceAzureIdentityAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            string secret = await this.serviceIdClient.GetSecretAsync(new Uri(this.settings.KeyVaultUri), this.settings.KeyVaultSecretName).ConfigureAwait(false);

            return new OkObjectResult(secret);
        }

        /// <summary>
        /// Function endpoint.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <returns>
        /// A task that determines the response.
        /// </returns>
        [FunctionName("UseConfiguredAzureIdentity")]
        public async Task<IActionResult> UseConfiguredAzureIdentity(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            try
            {
                string secret = await this.configIdClient.GetSecretAsync(
                        this.settings.KeyVaultClientIdentity,
                        new Uri(this.settings.KeyVaultUri),
                        this.settings.KeyVaultSecretName).ConfigureAwait(false);

                return new OkObjectResult(secret);
            }
            catch (Exception x)
            {
                return new ObjectResult("Failed: " + x) { StatusCode = 500 };
            }
        }
    }
}