// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.ServiceHub.Client;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    internal class VisualStudioCloudCacheStorageService : AbstractCloudCachePersistentStorageService
    {
        private readonly IThreadingContext _threadingContext;

        public VisualStudioCloudCacheStorageService(IThreadingContext threadingContext, IPersistentStorageConfiguration configuration)
            : base(configuration)
        {
            _threadingContext = threadingContext;
        }

        private void DisposeCacheService(ICacheService cacheService)
        {
            if (cacheService is IAsyncDisposable asyncDisposable)
            {
                _threadingContext.JoinableTaskFactory.Run(
                    () => asyncDisposable.DisposeAsync().AsTask());
            }
            else if (cacheService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        protected sealed override async ValueTask<WrappedCacheService> CreateCacheServiceAsync(string solutionFolder, CancellationToken cancellationToken)
        {
            var hubClient = new HubClient();

#pragma warning disable ISB001 // Dispose of proxies
            // cache service will be disposed inside VisualStudioCloudCachePersistentStorage.Dispose
            var cacheService = await hubClient.GetProxyAsync<ICacheService>(
                VisualStudioServices.VS2019_10.CacheService,
                new ServiceActivationOptions { ActivationArguments = new Dictionary<string, string> { { "foo", solutionFolder } } },
                cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

            Contract.ThrowIfNull(cacheService);
            return new WrappedCacheService(hubClient, cacheService, this.DisposeCacheService);
        }
    }
}
