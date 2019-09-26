// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal partial class AbstractTypeImportCompletionService
    {
        private interface IImportCompletionCacheService : IWorkspaceService
        {
            // PE references are keyed on assembly path.
            IDictionary<string, ReferenceCacheEntry> PEItemsCache { get; }

            IDictionary<ProjectId, ReferenceCacheEntry> ProjectItemsCache { get; }
        }

        [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService), ServiceLayer.Editor), Shared]
        private class ImportCompletionCacheServiceFactory : IWorkspaceServiceFactory
        {
            private readonly ConcurrentDictionary<string, ReferenceCacheEntry> _peItemsCache
                = new ConcurrentDictionary<string, ReferenceCacheEntry>();

            private readonly ConcurrentDictionary<ProjectId, ReferenceCacheEntry> _projectItemsCache
                = new ConcurrentDictionary<ProjectId, ReferenceCacheEntry>();

            [ImportingConstructor]
            public ImportCompletionCacheServiceFactory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            {
                var workspace = workspaceServices.Workspace;
                if (workspace.Kind == WorkspaceKind.Host)
                {
                    var cacheService = workspaceServices.GetService<IWorkspaceCacheService>();
                    if (cacheService != null)
                    {
                        cacheService.CacheFlushRequested += OnCacheFlushRequested;
                    }
                }

                return new ImportCompletionCacheService(_peItemsCache, _projectItemsCache);
            }

            private void OnCacheFlushRequested(object sender, EventArgs e)
            {
                _peItemsCache.Clear();
                _projectItemsCache.Clear();
            }

            private class ImportCompletionCacheService : IImportCompletionCacheService
            {
                public IDictionary<string, ReferenceCacheEntry> PEItemsCache { get; }

                public IDictionary<ProjectId, ReferenceCacheEntry> ProjectItemsCache { get; }

                public ImportCompletionCacheService(
                    ConcurrentDictionary<string, ReferenceCacheEntry> peCache,
                    ConcurrentDictionary<ProjectId, ReferenceCacheEntry> projectCache)
                {
                    PEItemsCache = peCache;
                    ProjectItemsCache = projectCache;
                }
            }
        }

    }
}
