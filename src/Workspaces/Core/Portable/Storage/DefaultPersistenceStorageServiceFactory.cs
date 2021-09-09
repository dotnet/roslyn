// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;

// When building for source-build, there is no sqlite dependency
#if !DOTNET_BUILD_FROM_SOURCE
using Microsoft.CodeAnalysis.SQLite.v2;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Storage
{
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService)), Shared]
    internal class DefaultPersistenceStorageServiceFactory : IWorkspaceServiceFactory
    {
#if DOTNET_BUILD_FROM_SOURCE

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultPersistenceStorageServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return NoOpPersistentStorageService.Instance;
        }

#else

        private readonly SQLiteConnectionPoolService _connectionPoolService;
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultPersistenceStorageServiceFactory(
            SQLiteConnectionPoolService connectionPoolService,
            IAsynchronousOperationListenerProvider asyncOperationListenerProvider)
        {
            _connectionPoolService = connectionPoolService;
            _asyncListener = asyncOperationListenerProvider.GetListener(FeatureAttribute.PersistentStorage);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var configuration = workspaceServices.GetRequiredService<IPersistentStorageConfiguration>();

            var database = GetDatabase(workspaceServices);
            switch (database)
            {
                case StorageDatabase.SQLite:
                    return new SQLitePersistentStorageService(_connectionPoolService, configuration, _asyncListener);

                case StorageDatabase.CloudCache:
                    var factory = workspaceServices.GetService<ICloudCacheStorageServiceFactory>();

                    return factory == null
                        ? NoOpPersistentStorageService.GetOrThrow(configuration)
                        : factory.Create(configuration);

                default:
                    throw ExceptionUtilities.UnexpectedValue(database);
            }
        }

        private static StorageDatabase GetDatabase(HostWorkspaceServices workspaceServices)
        {
            var optionService = workspaceServices.GetRequiredService<IOptionService>();

            return optionService.GetOption(StorageOptions.CloudCacheFeatureFlag) ? StorageDatabase.CloudCache :
                   optionService.GetOption(StorageOptions.Database);
        }

#endif
    }
}
