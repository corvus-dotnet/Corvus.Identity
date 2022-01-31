// <copyright file="AzureTokenCredentialAccessTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using global::Azure.Core.Pipeline;

    /// <summary>
    /// Wrapper for an <see cref="IAzureTokenCredentialSource"/> that implements
    /// <see cref="IAccessTokenSource"/>.
    /// </summary>
    internal class AzureTokenCredentialAccessTokenSource : IAccessTokenSource
    {
        private readonly object sync = new ();
        private readonly IAzureTokenCredentialSource tokenCredentialSource;
        private Task<TokenCacheAdapter>? cacheAdapterTask;

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
                Task<TokenCacheAdapter> cacheAdapterTask;
                lock (this.sync)
                {
                    if (this.cacheAdapterTask?.IsFaulted != false)
                    {
                        // Either we haven't built this task yet, or we have but it failed.
                        this.cacheAdapterTask = this.GetAdapter(replace: false, cancellationToken);
                    }

                    cacheAdapterTask = this.cacheAdapterTask;
                }

                TokenCacheAdapter cacheAdapter = await cacheAdapterTask.ConfigureAwait(false);
                AccessToken result = await cacheAdapter.GetAccessTokenAsync(requiredTokenCharacteristics).ConfigureAwait(false);
                return new AccessTokenDetail(result.Token, result.ExpiresOn);
            }
            catch (Exception x)
            {
                throw new AccessTokenNotIssuedException(x);
            }
        }

        /// <inheritdoc/>
        public async ValueTask<AccessTokenDetail> GetReplacementForFailedAccessTokenAsync(
            AccessTokenRequest requiredTokenCharacteristics,
            CancellationToken cancellationToken)
        {
            try
            {
                Task<TokenCacheAdapter> cacheAdapterTask = this.GetAdapter(replace: true, cancellationToken);
                TokenCacheAdapter cacheAdapter = await cacheAdapterTask.ConfigureAwait(false);

                // We only update the task if the attempt to get a new adapter succeeded. Some
                // IAzureTokenCredentialSource implementations don't support replacement (e.g.,
                // because a specific TokenCredential was supplied at application startup.
                // Attempts to refresh will inevitably fail with those, at which point we're
                // better off keeping hold of the cache we already had: if the situation that
                // prompted the application to attempt a replacement turns out to be some
                // transient external condition, it should recover.
                lock (this.sync)
                {
                    this.cacheAdapterTask = cacheAdapterTask;
                }

                AccessToken result = await cacheAdapter.GetAccessTokenAsync(requiredTokenCharacteristics).ConfigureAwait(false);
                return new AccessTokenDetail(result.Token, result.ExpiresOn);
            }
            catch (Exception x)
            {
                throw new AccessTokenNotIssuedException(x);
            }
        }

        // The Task returned is awaited multiple times, so we can't use ValueTask.
        private async Task<TokenCacheAdapter> GetAdapter(
            bool replace,
            CancellationToken cancellationToken)
        {
            TokenCredential tokenCredential = await (replace
#pragma warning disable CA2012 // Use ValueTasks correctly - overzealous analyzer
                ? this.tokenCredentialSource.GetReplacementForFailedTokenCredentialAsync(cancellationToken)
                : this.tokenCredentialSource.GetTokenCredentialAsync(cancellationToken))
#pragma warning restore CA2012
                .ConfigureAwait(false);

            return new TokenCacheAdapter(tokenCredential);
        }

        /// <summary>
        /// Enables use of Azure.Core's token cache.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Azure.Core provides built-in token caching functionality that all modern Azure SDK
        /// clients get to take advantage of. However, this cache is not exposed as a distinct
        /// feature: it's part of Azure.Core's HTTP pipeline system. This is fine for code that
        /// has gone all-in on Azure.Core (e.g., all new Azure SDK client libraries), because they
        /// will be using the pipeline for all their communications, so they'll pick up the token
        /// caching functionality automatically.
        /// </para>
        /// <para>
        /// The picture is less rosy for code that isn't using the Azure.Core HTTP pipeline. Any
        /// code using <see cref="IAccessTokenSource"/> is unlikely to be using that pipeline,
        /// because it would almost certainly be using <see cref="IAzureTokenCredentialSource"/>
        /// instead. Note that anything using the Microsoft.Rest adapter will be using
        /// <see cref="IAccessTokenSource"/> indirectly, so this applies to anything using the
        /// v3 Corvus.Identity libraries with old-style clients, such as older Azure client SDKs,
        /// or Autorest-generated client libraries. Code working directly with access tokens
        /// outwith the Azure.Core pipeline is, according to the Azure.Core documentation, on
        /// its own for token cache and refresh purposes. This is unfortunate because there's
        /// a fair amount of work involved in getting this right, and Microsoft has already done
        /// all of the necessary work in Azure.Core, it's just buried in a private nested class.
        /// </para>
        /// <para>
        /// This class exploits the fact that although the Azure.Core's caching is not public,
        /// it is possible to access it through a public class. The cache is implemented as a
        /// private nested class in <see cref="BearerTokenAuthenticationPolicy "/>, and while
        /// we can't use that private nested class directly, its contains class is public.
        /// By deriving from <see cref="BearerTokenAuthenticationPolicy "/>, we get access to
        /// protected methods that will then go on to use the private nested access token
        /// cache for us.
        /// </para>
        /// <para>
        /// This is slightly messy because <see cref="BearerTokenAuthenticationPolicy "/> expects
        /// to be used as part of an Azure.Core HTTP pipeline. In practice this means that it
        /// doesn't provide a direct way to obtain the access tokens: instead, it expects to be
        /// passed an <see cref="HttpMessage"/>, which it will then populate with a suitable
        /// header. So we have to fake one of these up, pass that to our base class, and then
        /// dig out the token it set.
        /// </para>
        /// </remarks>
        private class TokenCacheAdapter : BearerTokenAuthenticationPolicy
        {
            private readonly HttpPipeline fakePipeline;

            public TokenCacheAdapter(TokenCredential credential)
                : base(credential, Array.Empty<string>())
            {
                this.fakePipeline = HttpPipelineBuilder.Build(new FakePipelineOptions(), this);
            }

            public async ValueTask<AccessToken> GetAccessTokenAsync(AccessTokenRequest requiredTokenCharacteristics)
            {
                HttpMessage message = this.fakePipeline.CreateMessage();
                var requestContext = new TokenRequestContext(
                    scopes: requiredTokenCharacteristics.Scopes,
                    claims: requiredTokenCharacteristics.Claims,
                    tenantId: requiredTokenCharacteristics.AuthorityId);
                await this.AuthenticateAndAuthorizeRequestAsync(message, requestContext).ConfigureAwait(false);
                if (!message.Request.Headers.TryGetValue(HttpHeader.Names.Authorization, out string? headerValue))
                {
                    throw new InvalidOperationException("Token cache adapter failed to get token");
                }

                if (headerValue.StartsWith("Bearer "))
                {
                    headerValue = headerValue[7..];
                }

                // Unfortnately, although the base class's token cache knows the expiration time,
                // there's no way to get it to reveal that, so we have to make something up.
                // The token cache proactively refreshes tokens ahead of their expiration, so
                // in general it tries never to give back a token that has less than 5 minutes
                // left, so if we report that it has 3 minutes left, it's unlikely to expire before
                // that. Of course, most of the time it will actually have much longer than that
                // left to live, but the effect will be simply that code looking at the expiration
                // time will ask for a new token when it thinks this one has expired, and we'll
                // forward that call into the cache, which will inspect the real expiration time,
                // and determine that it can return the same token without needing to fetch a
                // new one. So the worst case is that we might end up causing more calls into the
                // cache than would otherwise happen. But in practice, the reason for adding this
                // cache adapter is that we observe that Autorest-generated clients make no attempt
                // cache the token we return to them, and ask for a new one frequently in any case,
                // so frequent calls into the cache are likely to happen regardless of what
                // expiration time we report. In any case when the base class is used in as part of
                // an Azure.Core HTTP pipeline (i.e., the scenario for which it is designed), it
                // calls into the cache for each request, so it's designed to cope with that.
                return new AccessToken(headerValue, DateTimeOffset.UtcNow.AddMinutes(3));
            }

            /// <summary>
            /// A concrete <see cref="ClientOptions"/> class to keep
            /// <see cref="HttpPipelineBuilder"/> happy.
            /// </summary>
            /// <remarks>
            /// <para>
            /// <see cref="HttpPipelineBuilder.Build(ClientOptions, HttpPipelinePolicy[])"/>
            /// requires us to pass a non-null <see cref="ClientOptions"/>. And since that's an
            /// abstract type, we need a concrete type to be able to construct one. This isn't
            /// used because we're only using the HTTP pipeline we build as a factory for
            /// <see cref="HttpMessage"/>s. Since we never really use the pipeline properly,
            /// the client options serve no purpose. We only need it because we have to fool
            /// our base class into thinking there is a pipeline so that we can use its cache.
            /// </para>
            /// </remarks>
            private class FakePipelineOptions : ClientOptions
            {
            }
        }
    }
}