// <copyright file="TestableClientCertificateCredential.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using global::Azure.Identity;

    /// <summary>
    /// <see cref="ClientCertificateCredential"/> that makes it possible for tests to discover
    /// what constructor arguments were used.
    /// </summary>
    internal class TestableClientCertificateCredential : ClientCertificateCredential
    {
        /// <summary>
        /// Creates a <see cref="TestableClientSecretCredential"/>.
        /// </summary>
        /// <param name="tenantId">
        /// The Azure Active Directory tenant (directory) Id of the service principal.
        /// </param>
        /// <param name="clientId">
        /// The client (application) ID of the service principal.
        /// </param>
        /// <param name="clientCertificate">
        /// A client certificate that was generated for the App Registration used to authenticate
        /// the client.
        /// </param>
        public TestableClientCertificateCredential(string tenantId, string clientId, X509Certificate2 clientCertificate)
            : base(tenantId, clientId, clientCertificate)
        {
            this.TenantId = tenantId;
            this.ClientId = clientId;
            this.ClientCertificate = clientCertificate;
        }

        /// <summary>
        /// Gets the value passed as the <c>tenantId</c> constructor argument.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets the value passed as the <c>clientId</c> constructor argument.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the value passed as the <c>clientCertificate</c> constructor argument.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; }
    }
}