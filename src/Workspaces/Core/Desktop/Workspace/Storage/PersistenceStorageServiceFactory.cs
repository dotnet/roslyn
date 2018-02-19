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
        private readonly object _gate = new object();
        private readonly SolutionSizeTracker _solutionSizeTracker;

        private IPersistentStorageService _singleton;

        [ImportingConstructor]
        public PersistenceStorageServiceFactory(SolutionSizeTracker solutionSizeTracker)
        {
            _solutionSizeTracker = solutionSizeTracker;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            lock (_gate)
            {
                if (_singleton == null)
                {
                    _singleton = GetPersistentStorageService(workspaceServices);
                }

                return _singleton;
            }
        }

        private IPersistentStorageService GetPersistentStorageService(HostWorkspaceServices workspaceServices)
        {
            var optionService = workspaceServices.GetService<IOptionService>();
            var database = optionService.GetOption(StorageOptions.Database);
            switch (database)
            {
                case StorageDatabase.SQLite:
                    return new SQLitePersistentStorageService(optionService, _solutionSizeTracker);
                case StorageDatabase.None:
                default:
                    return NoOpPersistentStorageService.Instance;
            }
        }
    }
}
