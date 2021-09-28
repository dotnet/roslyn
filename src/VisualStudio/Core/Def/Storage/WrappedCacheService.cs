// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.RpcContracts;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    internal class WrappedCacheService : ICacheService, IDisposable
    {
        private readonly IDisposable? _hubClient;
        private readonly ICacheService _cacheService;

        public WrappedCacheService(IDisposable? hubClient, ICacheService cacheService)
        {
            _hubClient = hubClient;
            _cacheService = cacheService;
        }

        public void Dispose()
        {
            (_cacheService as IDisposable)?.Dispose();
            _hubClient?.Dispose();
        }

        public ValueTask<string> GetRelativePathBaseAsync(CancellationToken cancellationToken)
            => _cacheService.GetRelativePathBaseAsync(cancellationToken);

        public Task<bool> CheckExistsAsync(CacheItemKey key, CancellationToken cancellationToken)
            => _cacheService.CheckExistsAsync(key, cancellationToken);

        public Task<bool> TryGetItemAsync(CacheItemKey key, PipeWriter writer, CancellationToken cancellationToken)
            => _cacheService.TryGetItemAsync(key, writer, cancellationToken);

        public Task SetItemAsync(CacheItemKey key, PipeReader reader, bool shareable, CancellationToken cancellationToken)
            => _cacheService.SetItemAsync(key, reader, shareable, cancellationToken);

        public Task DownloadContainerAsync(CacheContainerKey containerKey, IProgress<ProgressData>? progress, CancellationToken cancellationToken)
           => _cacheService.DownloadContainerAsync(containerKey, progress, cancellationToken);
    }
}
