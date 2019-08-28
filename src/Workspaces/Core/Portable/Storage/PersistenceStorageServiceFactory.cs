// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.SQLite;

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

            return NoOpPersistentStorageService.Instance;
        }
    }
}
