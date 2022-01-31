// <copyright file="AccessTokenNotIssuedException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication
{
    using System;

    /// <summary>
    /// Thrown when an <see cref="IAccessTokenSource"/> fails to acquire a token.
    /// </summary>
    public class AccessTokenNotIssuedException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="AccessTokenNotIssuedException"/>.
        /// </summary>
        /// <param name="innerException">
        /// The underlying exception that caused the problem.
        /// </param>
        public AccessTokenNotIssuedException(Exception innerException)
            : base("Access token cannot be acquired", innerException)
        {
        }
    }
}