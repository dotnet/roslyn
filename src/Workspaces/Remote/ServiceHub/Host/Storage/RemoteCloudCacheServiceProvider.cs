// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.Host;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    [ExportWorkspaceService(typeof(ICloudCacheServiceProvider), WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteCloudCacheServiceProvider : ICloudCacheServiceProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteCloudCacheServiceProvider()
        {
        }

        public async ValueTask<ICloudCacheService> CreateCacheAsync(CancellationToken cancellationToken)
        {
            var serviceBroker = GlobalServiceBroker.GetGlobalInstance();

#pragma warning disable ISB001 // Dispose of proxies
            // cache service will be disposed inside VisualStudioCloudCachePersistentStorage.Dispose
            var cacheService = await serviceBroker.GetProxyAsync<ICacheService>(VisualStudioServices.VS2019_9.CacheService, cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

            Contract.ThrowIfNull(cacheService);
            return new RemoteCloudCacheService(cacheService);
        }
    }
}
