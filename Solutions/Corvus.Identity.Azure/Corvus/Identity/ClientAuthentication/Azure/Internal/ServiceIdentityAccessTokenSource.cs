// <copyright file="ServiceIdentityAccessTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    /// <summary>
    /// Wraps an <see cref="IServiceIdentityAzureTokenCredentialSource"/> as an
    /// <see cref="IServiceIdentityAccessTokenSource"/>.
    /// </summary>
    internal class ServiceIdentityAccessTokenSource :
        AzureTokenCredentialAccessTokenSource,
        IServiceIdentityAccessTokenSource
    {
        /// <summary>
        /// Creates a <see cref="AzureTokenCredentialAccessTokenSource"/>.
        /// </summary>
        /// <param name="tokenCredentialSource">The source of Azure token credentials to wrap.</param>
        public ServiceIdentityAccessTokenSource(
            IServiceIdentityAzureTokenCredentialSource tokenCredentialSource)
            : base(tokenCredentialSource)
        {
        }
    }
}