// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote.Host;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteCloudCacheStorageServiceFactory : ICloudCacheStorageServiceFactory
    {
        private readonly IGlobalServiceBroker _globalServiceBroker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteCloudCacheStorageServiceFactory(IGlobalServiceBroker globalServiceBroker)
        {
            _globalServiceBroker = globalServiceBroker;
        }

        public AbstractPersistentStorageService Create(IPersistentStorageConfiguration configuration)
            => new RemoteCloudCachePersistentStorageService(_globalServiceBroker, configuration);

        private class RemoteCloudCachePersistentStorageService : AbstractCloudCachePersistentStorageService
        {
            private readonly IGlobalServiceBroker _globalServiceBroker;

            public RemoteCloudCachePersistentStorageService(IGlobalServiceBroker globalServiceBroker, IPersistentStorageConfiguration configuration)
                : base(configuration)
            {
                _globalServiceBroker = globalServiceBroker;
            }

            protected override async ValueTask<ICacheService> CreateCacheServiceAsync(string solutionFolder, CancellationToken cancellationToken)
            {
                var serviceBroker = _globalServiceBroker.Instance;

#pragma warning disable ISB001 // Dispose of proxies
                // cache service will be disposed inside RemoteCloudCacheService.Dispose
                var cacheService = await serviceBroker.GetProxyAsync<ICacheService>(
                    VisualStudioServices.VS2019_10.CacheService,
                    // replace with CacheService.RelativePathBaseActivationArgKey once available.
                    new ServiceActivationOptions { ActivationArguments = ImmutableDictionary<string, string>.Empty.Add("RelativePathBase", solutionFolder) },
                    cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

                Contract.ThrowIfNull(cacheService);
                return cacheService;
            }
        }
    }
}
