// <copyright file="AccessTokenDetail.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication
{
    using System;

    /// <summary>
    /// An access token and expiry information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class bears a very obvious resemblance to <c>Azure.Core.AccessToken</c>. The reason
    /// we're not using that type is that this library does not impose dependencies on any
    /// particular set of client libraries.
    /// </para>
    /// </remarks>
    public readonly struct AccessTokenDetail
    {
        /// <summary>
        /// Creates an <see cref="AccessTokenDetail"/>.
        /// </summary>
        /// <param name="accessToken">The <see cref="AccessToken"/>.</param>
        /// <param name="expiresOn">The <see cref="ExpiresOn"/>.</param>
        public AccessTokenDetail(string accessToken, DateTimeOffset expiresOn)
        {
            this.AccessToken = accessToken;
            this.ExpiresOn = expiresOn;
        }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the time at which the access token will expire.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; }
    }
}