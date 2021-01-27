// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    internal class VisualStudioCloudCacheService : ICloudCacheService
    {
        private readonly ICacheService _cacheService;

        public VisualStudioCloudCacheService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public void Dispose()
            => (_cacheService as IDisposable)?.Dispose();

        public Task<bool> CheckExistsAsync(CacheItemKey key, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<string> GetRelativePathBaseAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task SetItemAsync(CacheItemKey key, PipeReader reader, bool shareable, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> TryGetItemAsync(CacheItemKey key, PipeWriter writer, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
