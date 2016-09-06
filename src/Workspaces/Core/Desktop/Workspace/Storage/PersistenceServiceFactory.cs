// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;

namespace Microsoft.CodeAnalysis.Storage
{
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Desktop), Shared]
    internal class PersistenceStorageServiceFactory : IWorkspaceServiceFactory
    {
        private readonly SolutionSizeTracker _solutionSizeTracker;

        private IPersistentStorageService _singleton;

        [ImportingConstructor]
        public PersistenceStorageServiceFactory(SolutionSizeTracker solutionSizeTracker)
        {
            _solutionSizeTracker = solutionSizeTracker;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_singleton == null)
            {
                var optionService = workspaceServices.GetService<IOptionService>();
                Interlocked.CompareExchange(ref _singleton, new PersistentStorageService(optionService, _solutionSizeTracker), null);
            }

            return _singleton;
        }
    }
}
