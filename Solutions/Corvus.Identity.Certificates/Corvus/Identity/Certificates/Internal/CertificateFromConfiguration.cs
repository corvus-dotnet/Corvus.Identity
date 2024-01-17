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
            using X509Store store = new(clientCertificateConfiguration.StoreName, clientCertificateConfiguration.StoreLocation);
            store.Open(OpenFlags.ReadOnly);

            return new ValueTask<X509Certificate2>(store.Certificates.Find(X509FindType.FindBySubjectName, clientCertificateConfiguration.SubjectName!, true).Single());
        }
    }
}