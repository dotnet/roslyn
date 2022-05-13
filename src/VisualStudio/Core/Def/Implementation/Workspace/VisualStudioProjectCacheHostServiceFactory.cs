// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(IProjectCacheHostService), ServiceLayer.Host)]
    [Shared]
    internal partial class VisualStudioProjectCacheHostServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly TimeSpan ImplicitCacheTimeout = TimeSpan.FromMilliseconds(10000);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioProjectCacheHostServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // we support active document tracking only for visual studio workspace host.
            if (workspaceServices.Workspace is VisualStudioWorkspace)
            {
                return GetVisualStudioProjectCache(workspaceServices);
            }

            return GetMiscProjectCache(workspaceServices);
        }

        private static IWorkspaceService GetMiscProjectCache(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace.Kind != WorkspaceKind.Host)
            {
                return new ProjectCacheService(workspaceServices.Workspace);
            }

            var projectCacheService = new ProjectCacheService(workspaceServices.Workspace, ImplicitCacheTimeout);

            // Also clear the cache when the solution is cleared or removed.
            workspaceServices.Workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind is WorkspaceChangeKind.SolutionCleared or WorkspaceChangeKind.SolutionRemoved)
                {
                    projectCacheService.ClearImplicitCache();
                }
            };

            return projectCacheService;
        }

        private static IWorkspaceService GetVisualStudioProjectCache(HostWorkspaceServices workspaceServices)
        {
            // We will finish setting this up in VisualStudioWorkspaceImpl.DeferredInitializationState
            return new ProjectCacheService(workspaceServices.Workspace, ImplicitCacheTimeout);
        }

        internal static void ConnectProjectCacheServiceToDocumentTracking(HostWorkspaceServices workspaceServices, ProjectCacheService projectCacheService)
        {
            var documentTrackingService = workspaceServices.GetRequiredService<IDocumentTrackingService>();

            // Subscribe to events so that we can cache items from the active document's project
            var manager = new ActiveProjectCacheManager(documentTrackingService, projectCacheService);

            // Subscribe to requests to clear the cache
            var workspaceCacheService = workspaceServices.GetService<IWorkspaceCacheService>();
            if (workspaceCacheService != null)
            {
                workspaceCacheService.CacheFlushRequested += (s, e) => manager.Clear();
            }

            // Also clear the cache when the solution is cleared or removed.
            workspaceServices.Workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind is WorkspaceChangeKind.SolutionCleared or WorkspaceChangeKind.SolutionRemoved)
                {
                    manager.Clear();
                }
            };
        }

        private class ActiveProjectCacheManager
        {
            private readonly ProjectCacheService _projectCacheService;
            private readonly object _guard = new();

            private ProjectId _mostRecentActiveProjectId;
            private IDisposable _mostRecentCache;

            public ActiveProjectCacheManager(IDocumentTrackingService documentTrackingService, ProjectCacheService projectCacheService)
            {
                _projectCacheService = projectCacheService;

                documentTrackingService.ActiveDocumentChanged += UpdateCache;
                UpdateCache(null, documentTrackingService.TryGetActiveDocument());
            }

            private void UpdateCache(object sender, DocumentId activeDocument)
            {
                lock (_guard)
                {
                    if (activeDocument != null && activeDocument.ProjectId != _mostRecentActiveProjectId)
                    {
                        ClearMostRecentCache_NoLock();
                        _mostRecentCache = _projectCacheService.EnableCaching(activeDocument.ProjectId);
                        _mostRecentActiveProjectId = activeDocument.ProjectId;
                    }
                }
            }

            public void Clear()
            {
                lock (_guard)
                {
                    // clear most recent cache
                    ClearMostRecentCache_NoLock();

                    // clear implicit cache
                    _projectCacheService.ClearImplicitCache();
                }
            }

            private void ClearMostRecentCache_NoLock()
            {
                if (_mostRecentCache != null)
                {
                    _mostRecentCache.Dispose();
                    _mostRecentCache = null;
                }

                _mostRecentActiveProjectId = null;
            }
        }
    }
}
