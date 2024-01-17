// <copyright file="ClientCertificateConfigurationSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Certificates
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public sealed class ClientCertificateConfigurationSteps
    {
        private TestConfiguration? configuration;
        private ServiceProvider serviceProvider;
        private ICertificateFromConfiguration certificateSource;
        private X509Certificate2? certificate;
        private Exception? exceptionFromGetCertificate;

        public ClientCertificateConfigurationSteps()
        {
            ServiceCollection services = new();
            services.AddCertificateFromConfiguration();

            this.serviceProvider = services.BuildServiceProvider();

            this.certificateSource = this.serviceProvider.GetRequiredService<ICertificateFromConfiguration>();
        }

        [When("client certificate configuration is")]
        public void GivenConfigurationOf(string configurationJson)
        {
            this.configuration = ConfigLoading.LoadJsonConfiguration<TestConfiguration>(configurationJson);
        }

        [Then("the certificate store location is '([^']*)'")]
        public void ThenTheCertificateStoreLocationIs(StoreLocation storeLocation)
        {
            Assert.AreEqual(storeLocation, this.configuration!.ClientCertificate!.StoreLocation);
        }

        [When(@"we attempt to get the configured certificate")]
        public async Task WhenWeAttemptToGetTheConfiguredCertificateAsync()
        {
            try
            {
                this.certificate = await this.certificateSource!.CertificateForConfigurationAsync(this.configuration!.ClientCertificate!).ConfigureAwait(false);
            }
            catch (Exception x)
            {
                this.exceptionFromGetCertificate = x;
            }
        }

        [Then(@"CertificateForConfigurationAsync throws a CertificateNotFoundException")]
        public void ThenCertificateForConfigurationAsyncThrowsACertificateNotFoundException()
        {
            Assert.IsInstanceOf<CertificateNotFoundException>(this.exceptionFromGetCertificate);
        }

        public class TestConfiguration
        {
            public ClientCertificateConfiguration? ClientCertificate { get; set; }
        }
    }
}