// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectCacheHostServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace.Kind != WorkspaceKind.Host)
            {
                return new ProjectCacheService(workspaceServices.Workspace);
            }

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
