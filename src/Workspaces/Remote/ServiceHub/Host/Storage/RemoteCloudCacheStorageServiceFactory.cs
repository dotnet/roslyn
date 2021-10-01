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
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    /// <summary>
    /// The storage service is a process wide singleton.  It will ensure that any workspace that
    /// wants to read/write data for a particular solution gets the same DB.  This is important,
    /// we do not partition access to the information about a solution to particular workspaces.
    /// </summary>
    [Export(typeof(RemoteCloudCachePersistentStorageService)), Shared]
    internal class RemoteCloudCachePersistentStorageService : AbstractCloudCachePersistentStorageService
    {
        [ExportWorkspaceService(typeof(ICloudCacheStorageServiceFactory), WorkspaceKind.RemoteWorkspace), Shared]
        internal class RemoteCloudCacheStorageServiceFactory : ICloudCacheStorageServiceFactory
        {
            private readonly IChecksummedPersistentStorageService _service;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public RemoteCloudCacheStorageServiceFactory(RemoteCloudCachePersistentStorageService service)
                => _service = service;

            public IChecksummedPersistentStorageService Create()
                => _service;
        }

        private readonly IGlobalServiceBroker _globalServiceBroker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteCloudCachePersistentStorageService(IGlobalServiceBroker globalServiceBroker)
            => _globalServiceBroker = globalServiceBroker;

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
