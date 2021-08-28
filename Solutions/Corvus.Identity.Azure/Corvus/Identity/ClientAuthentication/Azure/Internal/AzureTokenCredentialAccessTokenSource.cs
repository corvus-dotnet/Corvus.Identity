// <copyright file="AzureTokenCredentialAccessTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;

    /// <summary>
    /// Wrapper for a <see cref="TokenCredential"/> that implements
    /// <see cref="IServiceIdentityAccessTokenSource"/>.
    /// </summary>
    internal class AzureTokenCredentialAccessTokenSource : IServiceIdentityAccessTokenSource
    {
        private readonly TokenCredential tokenCredential;

        /// <summary>
        /// Creates a <see cref="AzureTokenCredentialAccessTokenSource"/>.
        /// </summary>
        /// <param name="tokenCredential">The Azure token credential to wrap.</param>
        public AzureTokenCredentialAccessTokenSource(TokenCredential tokenCredential)
        {
            this.tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));
        }

        /// <inheritdoc/>
        public async ValueTask<AccessTokenDetail> GetAccessTokenAsync(
            AccessTokenRequest requiredTokenCharacteristics,
            CancellationToken cancellationToken)
        {
            try
            {
                AccessToken result = await this.tokenCredential.GetTokenAsync(
                    new TokenRequestContext(
                        scopes: requiredTokenCharacteristics.Scopes,
                        claims: requiredTokenCharacteristics.Claims,
                        tenantId: requiredTokenCharacteristics.AuthorityId),
                    cancellationToken)
                    .ConfigureAwait(false);
                return new AccessTokenDetail(result.Token, result.ExpiresOn);
            }
            catch (Exception x)
            {
                throw new AccessTokenNotIssuedException(x);
            }
        }
    }
}