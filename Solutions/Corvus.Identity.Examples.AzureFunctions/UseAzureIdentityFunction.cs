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
        private readonly UseAzureKeyVaultWithNewSdk client;
        private readonly ILogger<UseAzureIdentityFunction> logger;
        private readonly ExampleSettings settings;

        /// <summary>
        /// Creates a <see cref="UseAzureIdentityFunction"/>.
        /// </summary>
        /// <param name="settings">Example settings.</param>
        /// <param name="client">
        /// The client wrapper for accessing the key vault.
        /// </param>
        /// <param name="logger">Logger.</param>
        public UseAzureIdentityFunction(
            ExampleSettings settings,
            UseAzureKeyVaultWithNewSdk client,
            ILogger<UseAzureIdentityFunction> logger)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
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
        [FunctionName("UseAzureIdentity")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            this.logger.LogInformation($"Request: {req.Path}");

            string secret = await this.client.GetSecretAsync(new Uri(this.settings.KeyVaultUri), this.settings.KeyVaultSecretName).ConfigureAwait(false);

            return new OkObjectResult(secret);
        }
    }
}