﻿// <copyright file="AzureTokenCredentialSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Threading.Tasks;

    using global::Azure.Core;

    /// <summary>
    /// <see cref="IServiceIdentityAzureTokenCredentialSource"/> that returns a particular
    /// <see cref="TokenCredential"/>.
    /// </summary>
    internal class AzureTokenCredentialSource : IServiceIdentityAzureTokenCredentialSource
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
        public ValueTask<TokenCredential> GetAccessTokenAsync() => new (this.tokenCredential);
    }
}