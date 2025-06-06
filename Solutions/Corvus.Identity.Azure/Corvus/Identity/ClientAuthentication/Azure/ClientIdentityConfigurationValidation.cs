// <copyright file="ClientIdentityConfigurationValidation.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Checks <see cref="ClientIdentityConfiguration"/> instances for validity.
    /// </summary>
    internal static class ClientIdentityConfigurationValidation
    {
        /// <summary>
        /// Checks a <see cref="ClientIdentityConfiguration"/> for validity.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to check.
        /// </param>
        /// <param name="type">
        /// Returns the type of identity configuration the validator has determined this to be.
        /// </param>
        /// <returns>
        /// Null if the configuration is valid. A description of the problem if it is not valid.
        /// </returns>
        internal static string? Validate(
            ClientIdentityConfiguration configuration,
            out ClientIdentitySourceTypes type)
        {
            type = default;

            HashSet<ClientIdentitySourceTypes> indicatedSourceTypes = [];

            if (configuration is null)
            {
                return "must not be null";
            }

            if (configuration.IdentitySourceType.HasValue)
            {
                indicatedSourceTypes.Add(configuration.IdentitySourceType.Value);
            }

            bool adAppTenantIdPresent = !string.IsNullOrWhiteSpace(configuration.AzureAdAppTenantId);
            bool adAppClientIdPresent = !string.IsNullOrWhiteSpace(configuration.AzureAdAppClientId);
            bool adAppClientSecretPlainTextPresent = !string.IsNullOrWhiteSpace(configuration.AzureAdAppClientSecretPlainText);
            bool adAppClientSecretKeyVaultPresent = configuration.AzureAdAppClientSecretInKeyVault is not null;
            bool managedIdClientIdPresent = !string.IsNullOrWhiteSpace(configuration.ManagedIdentityClientId);
            if (configuration.IdentitySourceType == ClientIdentitySourceTypes.ClientIdAndSecret
                || adAppTenantIdPresent
                || adAppClientIdPresent
                || adAppClientSecretPlainTextPresent
                || adAppClientSecretKeyVaultPresent)
            {
                indicatedSourceTypes.Add(ClientIdentitySourceTypes.ClientIdAndSecret);
            }

            if (configuration.IdentitySourceType == ClientIdentitySourceTypes.UserAssignedManaged
                || managedIdClientIdPresent)
            {
                indicatedSourceTypes.Add(ClientIdentitySourceTypes.UserAssignedManaged);
            }

            switch (indicatedSourceTypes.Count)
            {
                case 0:
                    return "unable to determine identity type because no suitable properties have been set";

                case 1:
                    type = indicatedSourceTypes.Single();
                    break;

                default:
                    if (configuration.IdentitySourceType.HasValue)
                    {
                        string sourceTypes = string.Join(", ", indicatedSourceTypes.Except([configuration.IdentitySourceType.Value]));
                        return $"identity type is ambiguous because the IdentitySourceType is {SourceTypeString(configuration.IdentitySourceType)} but the properties set are for {sourceTypes}";
                    }

                    return $"identity type is ambiguous because the properties set are for {string.Join(", ", indicatedSourceTypes)}";
            }

            switch (type)
            {
                case ClientIdentitySourceTypes.ClientIdAndSecret:
                    if (!(adAppTenantIdPresent && adAppClientIdPresent &&
                        (adAppClientSecretPlainTextPresent ^ adAppClientSecretKeyVaultPresent)))
                    {
                        return "ClientIdAndSecret configuration must provide AzureAppTenantId, AzureAdAppClientId, and either AzureAppClientSecretPlainText or AzureAdAppClientSecretInKeyVault";
                    }

                    break;

                case ClientIdentitySourceTypes.UserAssignedManaged:
                    if (!managedIdClientIdPresent)
                    {
                        return "UserAssignedManaged configuration must provide ManagedIdentityClientId";
                    }

                    break;
            }

            return null;
        }

        private static string SourceTypeString(ClientIdentitySourceTypes? type) => type switch
        {
            // When we added support for user-assigned managed identities, we deprecated the
            // old ClientIdentitySourceTypes.Managed enumeration entry, which is now an alias
            // for ClientIdentitySourceTypes.SystemAssignedManaged. (Both have the same numeric
            // value, 3.) ToString picks the old obsolete "Managed" label, not the new, preferred
            // label of "SystemAssignedManaged", so we have to special-cased this.
            ClientIdentitySourceTypes.SystemAssignedManaged => nameof(ClientIdentitySourceTypes.SystemAssignedManaged),
            _ => type?.ToString() ?? "null",
        };
    }
}