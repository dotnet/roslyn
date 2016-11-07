// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), SolutionService.WorkspaceKind_RemoteWorkspace), Shared]
    internal class RemotePersistenceStorageServiceFactory : IWorkspaceServiceFactory
    {
        private IPersistentStorageService _singleton;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_singleton == null)
            {
                var optionService = workspaceServices.GetService<IOptionService>();
                Interlocked.CompareExchange(
                    ref _singleton,
                    new PersistentStorageService(optionService, new RemoteSolutionSizeTracker()), null);
            }

            return _singleton;
        }

        private class RemoteSolutionSizeTracker : ISolutionSizeTracker
        {
            public long GetSolutionSize(Workspace workspace, SolutionId id)
            {
                // Return a value large enough to ensure we always persist data
                // in our OOP server.
                return PersistentStorageService.SolutionSizeThreshold * 2;
            }
        }
    }
}