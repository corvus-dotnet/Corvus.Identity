// <copyright file="IServiceIdentityAccessTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication
{
    /// <summary>
    /// Enables a service to get access tokens based on its own identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sometimes, a service needs to be able to make outbound requests using its own identity
    /// (i.e., not acting on behalf of some other entity such as a particular human user). This
    /// interface is designed for scenarios in which the service authenticates itself by passing a
    /// bearer token (typically in the <c>Authorization</c> header of an HTTP request).
    /// </para>
    /// <para>
    /// This does not add any additional methods beyond the base interface,
    /// <see cref="IAccessTokenSource"/>. The reason this exists as a separate type is to enable
    /// dependency injection (DI) scenarios in which a class wants access not just to any old
    /// source of access tokens, but specifically to a source that will always produce access
    /// tokens that represent the service identity. Typically, an application would not register a
    /// service for the base <see cref="IAccessTokenSource"/> because it would not be clear what
    /// the tokens meant. This type allows a service to be specific about what it expects.
    /// </para>
    /// <para>
    /// As per the base <see cref="IAccessTokenSource"/> interface, this does not impose any
    /// requirements on the form of access tokens. Furthermore, this interface does not presume any
    /// particular mechanism for determining the service identity. It might be that the service is
    /// running in Azure in some context in which a Managed Identity is configured (and that is one
    /// of the most important scenarios for which this interface was designed), but it does not
    /// need to be.
    /// </para>
    /// </remarks>
    public interface IServiceIdentityAccessTokenSource : IAccessTokenSource
    {
    }
}