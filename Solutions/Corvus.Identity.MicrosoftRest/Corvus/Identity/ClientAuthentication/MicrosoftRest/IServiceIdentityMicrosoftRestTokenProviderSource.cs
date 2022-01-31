// <copyright file="IServiceIdentityMicrosoftRestTokenProviderSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.MicrosoftRest
{
    using Microsoft.Rest;

    /// <summary>
    /// Enables a service to get a <see cref="ITokenProvider"/> that will issue access tokens
    /// based on the service's own identity.
    /// </summary>
    /// <para>
    /// This does not add any additional methods beyond the base interface,
    /// <see cref="IMicrosoftRestTokenProviderSource"/>. The reason this exists as a separate type is to
    /// enable dependency injection (DI) scenarios in which a class wants access not just to any
    /// old source of access tokens, but specifically to a source that will always produce access
    /// tokens that represent the service identity. Typically, an application would not register a
    /// service for the base <see cref="IMicrosoftRestTokenProviderSource"/> because it would not be
    /// clear what the tokens meant. This type allows a service to be specific about what it
    /// expects.
    /// </para>
    public interface IServiceIdentityMicrosoftRestTokenProviderSource : IMicrosoftRestTokenProviderSource
    {
    }
}