// <copyright file="ClientCertificateConfigurationSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure.ClientCertificates
{
    using System.Security.Cryptography.X509Certificates;
    using Corvus.Identity.ClientAuthentication.Azure;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public sealed class ClientCertificateConfigurationSteps
    {
        private ClientCertificateConfiguration? configuration;

        [When("client certificate configuration is")]
        public void GivenConfigurationOf(string configurationJson)
        {
            this.configuration = ConfigLoading.LoadJsonConfiguration<ClientCertificateConfiguration>(configurationJson);
        }

        [Then("the certificate store location is '([^']*)'")]
        public void ThenTheCertificateStoreLocationIs(StoreLocation storeLocation)
        {
            Assert.AreEqual(storeLocation, this.configuration!.StoreLocation);
        }
    }
}