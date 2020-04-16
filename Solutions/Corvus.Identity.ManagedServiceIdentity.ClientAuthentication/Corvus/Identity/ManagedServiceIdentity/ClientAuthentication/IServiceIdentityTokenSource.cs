// <copyright file="IServiceIdentityTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System.Threading.Tasks;

    /// <summary>
    /// Enables applications running inside an Azure service to get access tokens based on that
    /// service's Managed Service Identity (MSI).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Azure is able to associate an identity with certain kinds of service, such as a Function,
    /// or a Web App. This identity is a Service Principal (and associated AAD Application) in the
    /// AAD tenant associated with the Azure subscription that hosts the service. It gets created
    /// automatically, and Azure manages the credentials for us.
    /// </para>
    /// <para>
    /// If a service with an associated MSI (call it 'C' for client) needs to invoke some other
    /// service ('S') that requires authentication, C may be able to authenticate using its MSI.
    /// For this to work, S will need to be able to recognize C's MSI. One common example would be
    /// if S is hosted in Azure and has had Azure Easy Authentication enabled, and configured to
    /// use AAD. For this to work, S will need to be configured to use the same AAD tenant as
    /// provides C's MSI, or some other AAD tenant that has had C's MSI added as an external
    /// identity.
    /// </para>
    /// <para>
    /// Another common scenario is when a service with an MSI needs to access a Microsoft API such
    /// as Azure Resource Manager (ARM), or the Graph API.
    /// </para>
    /// <para>
    /// This interface provides an abstraction for the capability of obtaining access tokens that
    /// enable the service to obtain Bearer authentication tokens that will identify it to other
    /// services that are able to recognize its MSI.
    /// </para>
    /// </remarks>
    public interface IServiceIdentityTokenSource
    {
        /// <summary>
        /// Obtains an access token suitable for use as a Bearer token in an HTTP Authorization
        /// header.
        /// </summary>
        /// <param name="resource">
        /// An identifier for the resource to be accessed. For Microsoft services (e.g., ARM) this
        /// will typically be a well-known URL. For service-to-service authentication where each
        /// service has its own MSI, this will typically be the GUID identifying the target
        /// service's MSI.
        /// </param>
        /// <returns>A task that produces an access token.</returns>
        Task<string?> GetAccessToken(string resource);

        /// <summary>
        /// Obtains an access token suitable for use as a Bearer token in an HTTP Authorization
        /// header, specifying the authority from which to obtain the token.
        /// </summary>
        /// <param name="authority">
        /// The authority that should issue the token.
        /// </param>
        /// <param name="resource">
        /// An identifier for the resource to be accessed. For Microsoft services (e.g., ARM) this
        /// will typically be a well-known URL. For service-to-service authentication where each
        /// service has its own MSI, this will typically be the GUID identifying the target
        /// service's MSI.
        /// </param>
        /// <param name="scope">
        /// The type of access required for this particular token.
        /// </param>
        /// <returns>A task that produces an access token.</returns>
        /// <remarks>
        /// This is the callback form required by <c>Microsoft.Azure.KeyVault.KeyVaultClient</c>.
        /// </remarks>
        Task<string> GetAccessTokenSpecifyingAuthority(string authority, string resource, string scope);
    }
}
