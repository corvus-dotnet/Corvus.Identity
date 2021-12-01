// <copyright file="LegacyAzureServiceTokenProviderOptions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Configuration settings for legacy Azure Managed Identity connection strings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="LegacyAzureServiceTokenProviderConnectionString"/> class and the
    /// <see cref="AzureIdentityServiceCollectionExtensions.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(IServiceCollection, string)"/>
    /// method both enable applications to continue to use connection strings of the form supported
    /// by <c>AzureServiceTokenProvider</c> in the <c>Microsoft.Azure.Services.AppAuthentication</c>
    /// NuGet package. Conventionally, this connection string is stored in a setting named
    /// <c>AzureServicesAuthConnectionString</c>. This class provides a convenient way to load
    /// that setting in, if you happen to be using either of the
    /// <c>Microsoft.Extensions.Configuration.Binder</c> or <c>Microsoft.Extensions.Options.ConfigurationExtensions</c>
    /// NuGet packages.
    /// </para>
    /// <para>
    /// This doesn't do anything that <c>config["AzureServicesAuthConnectionString"]</c> wouldn't
    /// have achieved. Its main contribution is to avoid every single application having to
    /// specify that magic config setting name.
    /// </para>
    /// </remarks>
    public class LegacyAzureServiceTokenProviderOptions
    {
        /// <summary>
        /// Gets or sets the connection string to use when obtaining tokens as the service identity
        /// when using the old format supported by the <c>Microsoft.Azure.Services.AppAuthentication</c>
        /// library.
        /// </summary>
        /// <remarks>
        /// <p>
        /// In most circumstances, this will be null, indicating that the default behaviour should
        /// be used. The default is that when running in an environment that offers an Azure
        /// Managed Identity (e.g., an Azure Function), the Managed Identity will be used. When
        /// running locally during development, it will look for a Visual Studio-supplied token
        /// file, and if there isn't one it then tries asking the Azure CLI for a token. (These
        /// behaviours all come from the <c>DefaultAzureCredential</c> class in <c>Azure.Identity</c>,
        /// and similar behaviour can be obtained from the old <c>AzureServiceTokenProvider</c> -
        /// they are not specific to this library.)
        /// </p>
        /// <para>
        /// For local development, there are occasions when it is not possible to obtain a token
        /// from Visual Studio or Azure CLI. (This happens if the service for which you need a
        /// token is not one that listed in the Azure AD application registratrion for Visual
        /// Studio or Azure CLI. A common example of this is when authenticating inter-service
        /// calls: if you write, say, an Azure Function that is protected by Azure AD, neither
        /// Visual Studio nor Azure CLI will have your function's application ID listed in their
        /// manifests, so you will be unable to use these to obtain a suitable token.) For these
        /// occasions you can set this to <c>RunAs=App;TenantId=your tenant;AppId=your app id;AppKey=your client secret</c>.
        /// This will authenticate as the specified application, and since you get to define which
        /// AD application to use, you can configure its manifest as required.
        /// </para>
        /// <p>
        /// If you want to be able to set this string via settings (e.g., in a local.settings.json
        /// file) use the following code in your startup:
        /// </p>
        /// <code><![CDATA[
        /// services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(configurationRoot.Get<LegacyAzureServiceTokenProviderOptions>());
        /// ]]></code>
        /// <p>
        /// The <c>Get&lt;T&gt;</c> method used here is an extension method in the
        /// <c>Microsoft.Extensions.Configuration</c> namespace, provided by the
        /// <c>Microsoft.Extensions.Configuration.Binder</c> NuGet package. If you're using the
        /// <c>Microsoft.Extensions.Options.ConfigurationExtensions</c> NuGet package to get access
        /// to the integration of <c>IOptions&lt;T&gt;</c> with configuration, you will already
        /// have an implicit reference to the binder package.
        /// </p>
        /// <p>
        /// There is no requirement to use <c>Microsoft.Extensions.Configuration</c>. You are free
        /// to create an instance of <see cref="LegacyAzureServiceTokenProviderOptions"/> however you like. The
        /// example above just shows an easy way to support the common configuration convention of
        /// enabling the connection string to be set with an application setting named
        /// <c>AzureServicesAuthConnectionString</c>.
        /// </p>
        /// </remarks>
        public string? AzureServicesAuthConnectionString { get; set; }
    }
}