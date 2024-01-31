// <copyright file="ClientCertificateConfigurationSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Certificates
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Org.BouncyCastle.Asn1.Pkcs;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Operators;
    using Org.BouncyCastle.Crypto.Prng;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.X509;
    using TechTalk.SpecFlow;
    using static Org.BouncyCastle.Asn1.Cmp.Challenge;

    [Binding]
    public sealed class ClientCertificateConfigurationSteps : IDisposable
    {
        private TestConfiguration? configuration;
        private ServiceProvider serviceProvider;
        private ICertificateFromConfiguration certificateSource;
        private X509Certificate2? certificate;
        private Exception? exceptionFromGetCertificate;
        private List<(string StoreName, string SubjectName)> tempCertificates = new();

        public ClientCertificateConfigurationSteps()
        {
            ServiceCollection services = new();
            services.AddCertificateFromConfiguration();

            this.serviceProvider = services.BuildServiceProvider();

            this.certificateSource = this.serviceProvider.GetRequiredService<ICertificateFromConfiguration>();
        }

        [Given(@"the '([^']*)' store contains a trusted certificate with the subject name of '([^']*)'")]
        public void GivenTheStoreContainsACertificateWithTheSubjectNameOf(string storeName, string subjectName)
        {
            using X509Store store = new(storeName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var keypairgen = new RsaKeyPairGenerator();
            keypairgen.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), 1024));

            AsymmetricCipherKeyPair keypair = keypairgen.GenerateKeyPair();

            var gen = new X509V3CertificateGenerator();

            var commonName = new X509Name(subjectName);
            var serialNumber = Org.BouncyCastle.Math.BigInteger.ProbablePrime(120, new Random());

            gen.SetSerialNumber(serialNumber);
            gen.SetSubjectDN(commonName);
            gen.SetIssuerDN(commonName);
            gen.SetNotAfter(DateTime.MaxValue);
            gen.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));
            ISignatureFactory signatureFactory = new Asn1SignatureFactory(PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(), keypair.Private);
            gen.SetPublicKey(keypair.Public);

            Org.BouncyCastle.X509.X509Certificate newCert = gen.Generate(signatureFactory);

            var certficate = new X509Certificate2(DotNetUtilities.ToX509Certificate((Org.BouncyCastle.X509.X509Certificate)newCert));

            store.Add(certficate);
            this.tempCertificates.Add((storeName, subjectName));
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

        [When(@"we get the configured certificate")]
        public async Task WhenWeGetTheConfiguredCertificateAsync()
        {
            this.certificate = await this.certificateSource!.CertificateForConfigurationAsync(this.configuration!.ClientCertificate!).ConfigureAwait(false);
        }

        [Then(@"CertificateForConfigurationAsync throws a CertificateNotFoundException")]
        public void ThenCertificateForConfigurationAsyncThrowsACertificateNotFoundException()
        {
            Assert.IsInstanceOf<CertificateNotFoundException>(this.exceptionFromGetCertificate);
        }

        [Then(@"the certificate returned by CertificateForConfigurationAsync has a subject name of '([^']*)'")]
        public void ThenTheCertificateReturnedByCertificateForConfigurationAsyncHasASubjectNameOf(string subjectName)
        {
            Assert.AreEqual(subjectName, this.certificate!.SubjectName);
        }

        public void Dispose()
        {
            foreach ((string storeName, string subjectName) in this.tempCertificates)
            {
                using X509Store store = new(storeName, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                foreach (X509Certificate2 cert in store.Certificates.Find(X509FindType.FindBySubjectName, subjectName.Replace("CN=", string.Empty), validOnly: false))
                {
                    store.Remove(cert);
                }
            }
        }

        public class TestConfiguration
        {
            public ClientCertificateConfiguration? ClientCertificate { get; set; }
        }
    }
}