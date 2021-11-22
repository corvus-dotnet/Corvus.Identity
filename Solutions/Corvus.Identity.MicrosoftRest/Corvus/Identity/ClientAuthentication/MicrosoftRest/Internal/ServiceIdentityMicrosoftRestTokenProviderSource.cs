// <copyright file="ServiceIdentityMicrosoftRestTokenProviderSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest.Internal
{
    /// <summary>
    /// Wraps an <see cref="IServiceIdentityAccessTokenSource"/> as an
    /// <see cref="IServiceIdentityMicrosoftRestTokenProviderSource"/>.
    /// </summary>
    internal class ServiceIdentityMicrosoftRestTokenProviderSource :
        MicrosoftRestTokenProviderSource,
        IServiceIdentityMicrosoftRestTokenProviderSource
    {
        /// <summary>
        /// Creates a <see cref="ServiceIdentityMicrosoftRestTokenProviderSource"/>.
        /// </summary>
        /// <param name="underlyingSource">
        /// The <see cref="IMicrosoftRestTokenProviderSource"/> to wrap as an
        /// <see cref="IServiceIdentityMicrosoftRestTokenProviderSource"/>.
        /// </param>
        public ServiceIdentityMicrosoftRestTokenProviderSource(
            IServiceIdentityAccessTokenSource underlyingSource)
            : base(underlyingSource)
        {
        }
    }
}