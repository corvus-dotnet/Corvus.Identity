﻿// <copyright file="ClientIdentityConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using Corvus.Identity.Certificates;

    /// <summary>
    /// Configuration determining the Azure AD identity client code will use for some operation
    /// (e.g., connecting to a storage service, or reading secrets from a key vault).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A service often needs to present a particular client identity. For example, application
    /// code might want to authenticate through Azure AD as a particular service identity when
    /// accessing a storage service. Or it might be necessary to retrieve the credentials for
    /// accessing a storage account from Key Vault, which in turn will need to be accessed with
    /// a suitable authorized Azure AD identity.
    /// </para>
    /// <para>
    /// TODO: Key Vault certificate based auth. AzureAdAppClientCertificateInKeyVault for when the cert lives
    /// in Key Vault; we'd need to introduce a KeyVaultCertificateConfiguration similar to the
    /// existing KeyVaultSecretConfiguration to support this.
    /// </para>
    /// </remarks>
    public class ClientIdentityConfiguration
    {
        /// <summary>
        /// Gets or sets the tenant id for the tenant defining the Azure AD application
        /// representing the identity with which to authenticate.
        /// </summary>
        public string? AzureAdAppTenantId { get; set; }

        /// <summary>
        /// Gets or sets the client id (aka AppID) of the Azure AD application representing the
        /// identity with which to authenticate.
        /// </summary>
        public string? AzureAdAppClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret to be presented to Azure AD when authenticating.
        /// </summary>
        public string? AzureAdAppClientSecretPlainText { get; set; }

        /// <summary>
        /// Gets or sets the configuration describing where in Azure Key Vault to find the client
        /// secret to be presented to Azure AD when authenticating.
        /// </summary>
        public KeyVaultSecretConfiguration? AzureAdAppClientSecretInKeyVault { get; set; }

        /// <summary>
        /// Gets or sets the configuration describing the client certificate to use when authenticating
        /// to Azure AD as a service principal using certificate based authentication.
        /// </summary>
        public ClientCertificateConfiguration? AzureAdAppClientCertificate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating where the identity to be used comes from (e.g., a
        /// Managed Identity, or Azure CLI).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is optional because in many cases, the source type can be inferred from which
        /// properties are set. However, in some cases there are no other properties. For example,
        /// when this is set to <see cref="ClientIdentitySourceTypes.Managed"/>, no other settings
        /// are required. (In some cases, even this can be omitted, as a managed identity might be
        /// the default, but this is not always the case.)
        /// </para>
        /// </remarks>
        public ClientIdentitySourceTypes? IdentitySourceType { get; set; }

        /// <summary>
        /// Gets or sets the client id of the user-defined managed identity with which to
        /// authenticate.
        /// </summary>
        public string? ManagedIdentityClientId { get; set; }
    }
}