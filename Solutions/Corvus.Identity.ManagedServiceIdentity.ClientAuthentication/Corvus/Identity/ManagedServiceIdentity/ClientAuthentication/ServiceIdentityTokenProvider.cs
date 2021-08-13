﻿// <copyright file="ServiceIdentityTokenProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ManagedServiceIdentity.ClientAuthentication
{
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Rest;

    /// <summary>
    /// A token provider for <c>Microsoft.Rest</c>-based clients that obtains its tokens from
    /// <see cref="IServiceIdentityTokenSource"/>.
    /// </summary>
    public class ServiceIdentityTokenProvider : ITokenProvider
    {
        private readonly IServiceIdentityTokenSource serviceIdentityTokenSource;
        private readonly string resource;

        /// <summary>
        /// Create a <see cref="ServiceIdentityTokenProvider"/>.
        /// </summary>
        /// <param name="serviceIdentityTokenSource">
        /// Source of tokens representing the host service's identity.
        /// </param>
        /// <param name="resource">
        /// Identifies the resource we will be accessing with the tokens (e.g., the app id of
        /// Azure App generated by Easy Auth for the target service).
        /// </param>
        public ServiceIdentityTokenProvider(
            IServiceIdentityTokenSource serviceIdentityTokenSource,
            string resource)
        {
            this.serviceIdentityTokenSource = serviceIdentityTokenSource;
            this.resource = resource;
        }

        /// <inheritdoc />
        public async Task<AuthenticationHeaderValue?> GetAuthenticationHeaderAsync(CancellationToken cancellationToken)
        {
            string? token = await this.serviceIdentityTokenSource.GetAccessToken(this.resource).ConfigureAwait(false);
            return token != null
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;
        }
    }
}
