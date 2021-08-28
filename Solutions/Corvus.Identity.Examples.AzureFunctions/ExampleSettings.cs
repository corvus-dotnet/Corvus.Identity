// <copyright file="ExampleSettings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

#nullable disable annotations

namespace Corvus.Identity.Examples.AzureFunctions
{
    /// <summary>
    /// Configuration settings for the example endpoints.
    /// </summary>
    public class ExampleSettings
    {
        /// <summary>
        /// Gets or sets the URI of the keyvault accessed by <see cref="UseAzureIdentityFunction"/>.
        /// </summary>
        public string KeyVaultUri { get; set; }

        /// <summary>
        /// Gets or sets the key vault secret accessed by <see cref="UseAzureIdentityFunction"/>.
        /// </summary>
        public string KeyVaultSecretName { get; set; }

        /// <summary>
        /// Gets or sets the Azure Subscription id used by <see cref="UseMicrosoftRestFunction"/>
        /// and <see cref="UsePlainTokensFunction"/>.
        /// </summary>
        public string AzureSubscriptionId { get; set; }
    }
}