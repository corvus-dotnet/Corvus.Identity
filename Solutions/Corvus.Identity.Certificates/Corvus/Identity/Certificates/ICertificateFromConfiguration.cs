// <copyright file="ICertificateFromConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Certificates
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides <see cref="X509Certificate2"/> instances from
    /// <see cref="ClientCertificateConfiguration"/> supplied at runtime.
    /// </summary>
    public interface ICertificateFromConfiguration
    {
        /// <summary>
        /// Returns an <see cref="X509Certificate2"/> as described by a
        /// <see cref="ClientCertificateConfiguration"/>.
        /// </summary>
        /// <param name="clientCertificateConfiguration">
        /// The <see cref="ClientCertificateConfiguration"/> describing the certificate to use.
        /// </param>
        /// <returns>A ValueTask that produces an <see cref="X509Certificate2"/>.</returns>
        ValueTask<X509Certificate2> CertificateForConfigurationAsync(ClientCertificateConfiguration clientCertificateConfiguration);
    }
}