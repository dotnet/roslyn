// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    internal class RemoteCloudCacheService : ICloudCacheService
    {
        private readonly ICacheService _cacheService;

        public RemoteCloudCacheService(ICacheService cacheService)
            => _cacheService = cacheService;

        private static CacheItemKey Convert(CloudCacheItemKey key)
            => new(Convert(key.ContainerKey), key.ItemName) { Version = key.Version };

        private static CacheContainerKey Convert(CloudCacheContainerKey containerKey)
            => new(containerKey.Component, containerKey.Dimensions);

        public void Dispose()
            => (_cacheService as IDisposable)?.Dispose();

        public Task<bool> CheckExistsAsync(CloudCacheItemKey key, CancellationToken cancellationToken)
            => _cacheService.CheckExistsAsync(Convert(key), cancellationToken);

        public ValueTask<string> GetRelativePathBaseAsync(CancellationToken cancellationToken)
            => _cacheService.GetRelativePathBaseAsync(cancellationToken);

        public Task SetItemAsync(CloudCacheItemKey key, PipeReader reader, bool shareable, CancellationToken cancellationToken)
            => _cacheService.SetItemAsync(Convert(key), reader, shareable, cancellationToken);

        public Task<bool> TryGetItemAsync(CloudCacheItemKey key, PipeWriter writer, CancellationToken cancellationToken)
            => _cacheService.TryGetItemAsync(Convert(key), writer, cancellationToken);
    }
}
