// <copyright file="IdentityCertificateServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Identity.Certificates;
    using Corvus.Identity.Certificates.Internal;

    /// <summary>
    /// DI initialization for services using Corvus.Identity.Certificates.
    /// </summary>
    public static class IdentityCertificateServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="ICertificateFromConfiguration"/> implementation to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddCertificateFromConfiguration(
            this IServiceCollection services)
        {
            return services.AddSingleton<ICertificateFromConfiguration, CertificateFromConfiguration>();
        }
    }
}