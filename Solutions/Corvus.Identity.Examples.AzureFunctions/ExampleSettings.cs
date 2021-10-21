// <copyright file="ExampleSettings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

#nullable disable annotations

namespace Corvus.Identity.Examples.AzureFunctions
{
    using Corvus.Identity.ClientAuthentication.Azure;

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
        /// Gets or sets the configuration determining client identity to use in
        /// <see cref="UseAzureIdentityFunction.UseConfiguredAzureIdentity(Microsoft.AspNetCore.Http.HttpRequest)"/>.
        /// </summary>
        public ClientIdentityConfiguration KeyVaultClientIdentity { get; set; }

        /// <summary>
        /// Gets or sets the Azure Subscription id used by <see cref="UseMicrosoftRestFunction"/>
        /// and <see cref="UsePlainTokensFunction"/>.
        /// </summary>
        public string AzureSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the configuration determining client identity to use in
        /// <see cref="UseMicrosoftRestFunction.UseConfiguredAsync(Microsoft.AspNetCore.Http.HttpRequest)"/>.
        /// </summary>
        public ClientIdentityConfiguration ArmClientIdentity { get; set; }

        /// <summary>
        /// Gets or sets the configuration determining the client identity to use as the ambient
        /// service identity. (Leave null to use the legacy connection string instead.)
        /// </summary>
        public ClientIdentityConfiguration ServiceIdentity { get; set; }
    }
}