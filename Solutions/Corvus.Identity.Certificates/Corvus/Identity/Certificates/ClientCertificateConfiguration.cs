// <copyright file="ClientCertificateConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Certificates
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

        /// <summary>
        /// Gets or sets a value indicating the name of the certificate store to use.
        /// </summary>
        public required string StoreName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the subject name of the certificate.
        /// </summary>
        public string? SubjectName { get; set; }
    }
}