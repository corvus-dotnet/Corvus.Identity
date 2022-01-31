// <copyright file="TokenCredentialBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.Azure
{
    using System;
    using System.Collections.Generic;

    using Corvus.Identity.ClientAuthentication.Azure.Internal;

    using global::Azure.Core;
    using global::Azure.Identity;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class TokenCredentialBindings
    {
        private const string KeyForDefaultCredential = "<unnamed>";

        public TokenCredential Credential
        {
            get => this.Credentials.TryGetValue(KeyForDefaultCredential, out TokenCredential? value)
                ? value
                : throw new InvalidOperationException("This test has not set any unnamed credentials");
            set => this.Credentials.Add(KeyForDefaultCredential, value);
        }

        public Dictionary<string, TokenCredential> Credentials { get; } = new();

        [Then("the TokenCredential should be of type '(.*)'")]
        public void ThenTheTokenCredentialShouldBeOfType(string credentialType)
        {
            this.ThenTheTokenCredentialShouldBeOfType(KeyForDefaultCredential, credentialType);
        }

        [Then("the TokenCredential '(.*)' should be of type '(.*)'")]
        public void ThenTheTokenCredentialShouldBeOfType(string credentialName, string credentialType)
        {
            string fullCredentialTypeName = $"Azure.Identity.{credentialType}";
            Type? expectedBaseType = typeof(DefaultAzureCredential).Assembly.GetType(fullCredentialTypeName);
            if (expectedBaseType is null)
            {
                Assert.Fail($"Did not find type {fullCredentialTypeName}");
            }
            else
            {
                Assert.IsInstanceOf(expectedBaseType, this.Credentials[credentialName]);
            }
        }

        [Then("the ClientSecretCredential tenantId should be '(.*)'")]
        public void ThenTheClientSecretCredentialTenantIdShouldBe(string tenantId)
        {
            this.ThenTheClientSecretCredentialTenantIdShouldBe(KeyForDefaultCredential, tenantId);
        }

        [Then("the ClientSecretCredential '(.*)' tenantId should be '(.*)'")]
        public void ThenTheClientSecretCredentialTenantIdShouldBe(string credentialName, string tenantId)
        {
            Assert.AreEqual(
                tenantId,
                ((TestableClientSecretCredential)this.Credentials[credentialName]).TenantId);
        }

        [Then("the ClientSecretCredential appId should be '(.*)'")]
        public void ThenTheClientSecretCredentialAppIdShouldBe(string clientId)
        {
            this.ThenTheClientSecretCredentialAppIdShouldBe(KeyForDefaultCredential, clientId);
        }

        [Then("the ClientSecretCredential '(.*)' appId should be '(.*)'")]
        public void ThenTheClientSecretCredentialAppIdShouldBe(string credentialName, string clientId)
        {
            Assert.AreEqual(
                clientId,
                ((TestableClientSecretCredential)this.Credentials[credentialName]).ClientId);
        }

        [Then("the ClientSecretCredential clientSecret should be '(.*)'")]
        public void ThenTheClientSecretCredentialClientSecretShouldBe(string clientSecret)
        {
            this.ThenTheClientSecretCredentialClientSecretShouldBe(KeyForDefaultCredential, clientSecret);
        }

        [Then("the ClientSecretCredential '(.*)' clientSecret should be '(.*)'")]
        public void ThenTheClientSecretCredentialClientSecretShouldBe(string credentialName, string clientSecret)
        {
            Assert.AreEqual(
                clientSecret,
                ((TestableClientSecretCredential)this.Credentials[credentialName]).ClientSecret);
        }

        public void SetNamedCredential(string? credentialName, TokenCredential credential)
        {
            this.Credentials.Add(
                credentialName ?? KeyForDefaultCredential,
                credential);
        }
    }
}