// <copyright file="TestableClientSecretCredential.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using global::Azure.Identity;

    /// <summary>
    /// <see cref="ClientSecretCredential"/> that makes it possible for tests to discover
    /// what constructor arguments were used.
    /// </summary>
    internal class TestableClientSecretCredential : ClientSecretCredential
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
        /// <param name="clientSecret">
        /// A client secret that was generated for the App Registration used to authenticate
        /// the client.
        /// </param>
        public TestableClientSecretCredential(string tenantId, string clientId, string clientSecret)
            : base(tenantId, clientId, clientSecret)
        {
            this.TenantId = tenantId;
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
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
        /// Gets the value passed as the <c>clientSecret</c> constructor argument.
        /// </summary>
        public string ClientSecret { get; }
    }
}
