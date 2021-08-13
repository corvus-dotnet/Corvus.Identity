// <copyright file="LegacyAzureServiceTokenProviderConnectionString.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using System;
    using System.Text.RegularExpressions;

    using global::Azure.Core;
    using global::Azure.Identity;

    /// <summary>
    /// Enables the old style of connection string supported by the AzureServiceTokenProvider
    /// class to be used in the newer world of Azure.Identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Irritatingly, v12 of the Azure SDK has done away with the AppAuthentication connection
    /// strings that AzureServiceTokenProvider used to support, making it very much harder to
    /// allow an application to switch between different modes of authentication via configuration.
    /// This supports some of the ones we often use.
    /// </para>
    /// </remarks>
    public static class LegacyAzureServiceTokenProviderConnectionString
    {
        /// <summary>
        /// Returns a <see cref="TokenCredential"/> as determined by an
        /// <c>AzureServiceTokenProvider</c>-style connection string.
        /// </summary>
        /// <param name="legacyConnectionString">
        /// A connection string in the old format <c>AzureServiceTokenProvider</c> supports.
        /// </param>
        /// <returns>
        /// A <see cref="TokenCredential"/>, where the concrete type is determined by the form
        /// of connection string supplied, initialized where applicable with the details in
        /// the connection string.
        /// </returns>
        public static TokenCredential ToTokenCredential(string legacyConnectionString)
        {
            const string appIdPattern = "RunAs=App;AppId=(?<AppId>[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Fa-f0-9]{12});TenantId=(?<TenantId>[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Fa-f0-9]{12});AppKey=(?<AppKey>[^;]*)";
            TokenCredential keyVaultCredentials = (legacyConnectionString?.Trim() ?? string.Empty) switch
            {
#pragma warning disable SA1122 // Use string.Empty for empty strings - StyleCop analyzer 1.1.118 doesn't understand patterns; it *has* to be "" here
                "" => new DefaultAzureCredential(),
#pragma warning restore SA1122 // Use string.Empty for empty strings

                "RunAs=Developer;DeveloperTool=AzureCli" => new AzureCliCredential(),
                "RunAs=Developer;DeveloperTool=VisualStudio" => new VisualStudioCredential(),
                "RunAs=App" => new ManagedIdentityCredential(),

                string s when Regex.Match(s, appIdPattern) is Match m && m.Success =>
                    new TestableClientSecretCredential(m.Groups["TenantId"].Value, m.Groups["AppId"].Value, m.Groups["AppKey"].Value),

                _ => throw new InvalidOperationException($"AzureServicesAuthConnectionString configuration value '{legacyConnectionString}' is not supported in this version of Corvus.Identity")
            };

            return keyVaultCredentials;
        }

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
}