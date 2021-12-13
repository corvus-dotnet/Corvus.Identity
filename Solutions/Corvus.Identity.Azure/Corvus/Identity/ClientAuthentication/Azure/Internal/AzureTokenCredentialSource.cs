// <copyright file="AzureTokenCredentialSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// <see cref="IServiceIdentityAzureTokenCredentialSource"/> that returns a particular
    /// <see cref="TokenCredential"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is used in scenarios where the application just hands us a <see cref="TokenCredential"/>
    /// that it wants us to use. For example, an application' startup code might call
    /// <see cref="AzureIdentityServiceCollectionExtensions.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(IServiceCollection, TokenCredential)"/>
    /// passing in a <c>DefaultAzureCredential</c> to get the commonly-used behaviour in which an
    /// application will use a Managed Identity if one is available, but will fall back to local
    /// means such as Azure CLI or Visual Studio authentication on development boxes.
    /// </para>
    /// </remarks>
    internal class AzureTokenCredentialSource : IAzureTokenCredentialSource
    {
        private readonly TokenCredential tokenCredential;

        /// <summary>
        /// Creates a <see cref="AzureTokenCredentialSource"/>.
        /// </summary>
        /// <param name="tokenCredential">
        /// The <see cref="TokenCredential"/> that <see cref="GetAccessTokenAsync"/> should return.
        /// </param>
        public AzureTokenCredentialSource(TokenCredential tokenCredential)
        {
            this.tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
        }

        /// <inheritdoc/>
        public ValueTask<TokenCredential> GetAccessTokenAsync() => this.GetTokenCredentialAsync();

        /// <inheritdoc/>
        public ValueTask<TokenCredential> GetTokenCredentialAsync(CancellationToken cancellationToken = default)
             => new (this.tokenCredential);

        /// <inheritdoc/>
        public ValueTask<TokenCredential> GetReplacementForFailedTokenCredentialAsync(
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}