// <copyright file="ClientCertificateConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Configures where to load a X.509 certificate from.
    /// </summary>
    public class ClientCertificateConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating the location of the certificate store to use.
        /// </summary>
        public StoreLocation StoreLocation { get; set; }

        // Next time: Work out which of these criteria we really want to include:

        // To do:
        // Which store e.g. user or machine.

        // Could do:
        // Issuer name.
        // Subject name.
        // Certificate name?
        // Certificate application type.

        // Won't do:
        // Template name?
        // Subject key name?
        // Thumbprint?
    }
}