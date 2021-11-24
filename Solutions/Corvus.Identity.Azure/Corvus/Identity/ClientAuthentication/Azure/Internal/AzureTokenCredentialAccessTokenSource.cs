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
    /// Wrapper for an <see cref="IAzureTokenCredentialSource"/> that implements
    /// <see cref="IAccessTokenSource"/>.
    /// </summary>
    internal class AzureTokenCredentialAccessTokenSource : IAccessTokenSource
    {
        private readonly IAzureTokenCredentialSource tokenCredentialSource;

        /// <summary>
        /// Creates a <see cref="AzureTokenCredentialAccessTokenSource"/>.
        /// </summary>
        /// <param name="tokenCredentialSource">The source of Azure token credentials to wrap.</param>
        public AzureTokenCredentialAccessTokenSource(
            IAzureTokenCredentialSource tokenCredentialSource)
        {
            this.tokenCredentialSource = tokenCredentialSource;
        }

        /// <inheritdoc/>
        public async ValueTask<AccessTokenDetail> GetAccessTokenAsync(
            AccessTokenRequest requiredTokenCharacteristics,
            CancellationToken cancellationToken)
        {
            try
            {
                TokenCredential tokenCredential = await this.tokenCredentialSource
                    .GetTokenCredentialAsync(cancellationToken)
                    .ConfigureAwait(false);
                AccessToken result = await tokenCredential.GetTokenAsync(
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

        /// <inheritdoc/>
        public ValueTask<AccessTokenDetail> GetReplacementForFailedAccessTokenAsync(
            AccessTokenDetail failedToken,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}