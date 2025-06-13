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

            // Passing validOnly: false because we do not want to limit this to certificates validated by an authority.
            // These are client certificates so it is the other party's problem to decide how to validate them.
            // For example, we might generate a certificate using a command line tool and then just upload that to AzureAD.
            return new ValueTask<X509Certificate2>(store.Certificates.Find(X509FindType.FindBySubjectName, clientCertificateConfiguration.SubjectName!, validOnly: false).SingleOrDefault() ?? throw new CertificateNotFoundException());
        }
    }
}