// <copyright file="ClientIdentitySourceTypes.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System;

    /// <summary>
    /// The various sources from which client identity information can be drawn.
    /// </summary>
    public enum ClientIdentitySourceTypes
    {
        /// <summary>
        /// No client identity is in use (e.g., because the target API allows anonymous access).
        /// </summary>
        None,

        /// <summary>
        /// The client identity is determined by an explicitly configured Client ID (aka App ID)
        /// and authentication with Azure AD is achieved by presenting a client secret.
        /// </summary>
        ClientIdAndSecret,

        /// <summary>
        /// The client identity is determined by an explicitly configured Client ID (aka App ID)
        /// and authentication with Azure AD is achieved with a client certificate over TLS.
        /// </summary>
        ClientIdAndCertificate,

        /// <summary>
        /// The ambient Azure Managed Identity should be used.
        /// </summary>
        SystemAssignedManaged,

        /// <summary>
        /// The old name for <see cref="SystemAssignedManaged"/>.
        /// </summary>
        /// <remarks>
        /// When this enum type was introduced, the only supported managed identity type was a
        /// system-assigned managed identity. The addition of <see cref="UserAssignedManaged"/>
        /// made this old name ambiguous.
        /// </remarks>
        [Obsolete("Use SystemAssignedManaged")]
        Managed = SystemAssignedManaged,

        /// <summary>
        /// The identity with which the Azure CLI is current logged in should be used. (For local
        /// development purposes only.)
        /// </summary>
        AzureCli,

        /// <summary>
        /// Visual Studio's authentication should be used to determine the identity. (For local
        /// development purposes only.)
        /// </summary>
        VisualStudio,

        /// <summary>
        /// The scheme followed by <c>Azure.Identity</c>'s <c>DefaultAzureCredential</c> should be
        /// used, i.e. look for explicit configuration via environment variables, and if that's not
        /// present, look for a Managed Identity, and then try falling back to a series of sources
        /// suitable for local development purposes.
        /// </summary>
        AzureIdentityDefaultAzureCredential,

        /// <summary>
        /// Visual Studio Code's authentication should be used to determine the identity. (For local
        /// development purposes only.)
        /// </summary>
        [Obsolete("Use AzureCli")]
        VisualStudioCode,

        /// <summary>
        /// A user-assigned managed identity.
        /// </summary>
        UserAssignedManaged,
    }
}