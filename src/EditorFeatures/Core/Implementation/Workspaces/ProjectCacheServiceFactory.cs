// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(IProjectCacheHostService), ServiceLayer.Editor)]
    [Shared]
    internal partial class ProjectCacheHostServiceFactory : IWorkspaceServiceFactory
    {
        private const int ImplicitCacheTimeoutInMS = 10000;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var service = new ProjectCacheService(workspaceServices.Workspace, ImplicitCacheTimeoutInMS);

            // Also clear the cache when the solution is cleared or removed.
            workspaceServices.Workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind == WorkspaceChangeKind.SolutionCleared || e.Kind == WorkspaceChangeKind.SolutionRemoved)
                {
                    service.ClearImplicitCache();
                }
            };

            return service;
        }
    }
}
