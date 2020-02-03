// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
// When building for source-build, there is no sqlite dependency
#if !DOTNET_BUILD_FROM_SOURCE
using Microsoft.CodeAnalysis.SQLite;
#endif

namespace Microsoft.CodeAnalysis.Storage
{
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Desktop), Shared]
    internal class PersistenceStorageServiceFactory : IWorkspaceServiceFactory
    {
        private readonly ISolutionSizeTracker _solutionSizeTracker;

        [ImportingConstructor]
        public PersistenceStorageServiceFactory(ISolutionSizeTracker solutionSizeTracker)
        {
            _solutionSizeTracker = solutionSizeTracker;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
#if !DOTNET_BUILD_FROM_SOURCE
            var optionService = workspaceServices.GetRequiredService<IOptionService>();
            var database = optionService.GetOption(StorageOptions.Database);
            switch (database)
            {
                case StorageDatabase.SQLite:
                    var locationService = workspaceServices.GetService<IPersistentStorageLocationService>();
                    if (locationService != null)
                    {
                        return new SQLitePersistentStorageService(optionService, locationService, _solutionSizeTracker);
                    }

                    break;
            }
#endif

            return NoOpPersistentStorageService.Instance;
        }
    }
}
