// <copyright file="CertificateFromConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Certificates.Internal
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Loads certificate based on configuration.
    /// </summary>
    internal class CertificateFromConfiguration : ICertificateFromConfiguration
    {
        /// <inheritdoc/>
        public ValueTask<X509Certificate2> CertificateForConfigurationAsync(ClientCertificateConfiguration clientCertificateConfiguration)
        {
            throw new NotImplementedException();
        }
    }
}