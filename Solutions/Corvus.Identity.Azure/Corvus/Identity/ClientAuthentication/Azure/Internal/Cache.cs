﻿// <copyright file="Cache.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Corvus.Identity.ClientAuthentication.Azure.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure;

    /// <summary>
    /// Maintains a cache of <see cref="CachedResponse"/> items.
    /// </summary>
    internal class Cache : IDisposable
    {
        private readonly Dictionary<string, CachedResponse> cache = new Dictionary<string, CachedResponse>(StringComparer.OrdinalIgnoreCase);
        private SemaphoreSlim? semaphore = new SemaphoreSlim(1, 1);

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.semaphore is { })
            {
                this.semaphore.Dispose();
                this.semaphore = null;
            }
        }

        /// <summary>
        /// Gets a valid <see cref="Response"/> or requests and caches a <see cref="CachedResponse"/>.
        /// </summary>
        /// <param name="isAsync">Whether certain operations should be performed asynchronously.</param>
        /// <param name="uri">The URI sans query parameters to cache.</param>
        /// <param name="ttl">The amount of time for which the cached item is valid.</param>
        /// <param name="action">The action to request a response.</param>
        /// <returns>A new <see cref="Response"/>.</returns>
        internal async ValueTask<Response> GetOrAddAsync(bool isAsync, string uri, TimeSpan ttl, Func<ValueTask<Response>> action)
        {
            this.ThrowIfDisposed();

            if (isAsync)
            {
                await this.semaphore!.WaitAsync().ConfigureAwait(false);
            }
            else
            {
                this.semaphore!.Wait();
            }

            try
            {
#nullable disable
                // Try to get a valid cached response inside the lock before fetching.
                if (this.cache.TryGetValue(uri, out CachedResponse cachedResponse) && cachedResponse.IsValid)
                {
                    return await cachedResponse.CloneAsync(isAsync).ConfigureAwait(false);
                }
#nullable enable

                Response response = await action().ConfigureAwait(false);
                if (response.Status == 200 && response.ContentStream is { })
                {
                    cachedResponse = await CachedResponse.CreateAsync(isAsync, response, ttl).ConfigureAwait(false);
                    this.cache[uri] = cachedResponse;
                }

                return response;
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Revmoes an item from the cache.
        /// </summary>
        /// <param name="uri">The uri of the item to remove.</param>
        internal void Remove(string uri)
        {
            this.cache.Remove(uri);
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        internal void Clear()
        {
            this.ThrowIfDisposed();

            this.semaphore!.Wait();
            try
            {
                this.cache.Clear();
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (this.semaphore is null)
            {
                throw new ObjectDisposedException(nameof(this.semaphore));
            }
        }
    }
}
