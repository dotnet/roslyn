// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal abstract class AbstractImportCompletionCacheServiceFactory<TProjectCacheEntry, TMetadataCacheEntry> : IWorkspaceServiceFactory
    {
        private readonly ConcurrentDictionary<string, TMetadataCacheEntry> _peItemsCache
            = new ConcurrentDictionary<string, TMetadataCacheEntry>();

        private readonly ConcurrentDictionary<ProjectId, TProjectCacheEntry> _projectItemsCache
            = new ConcurrentDictionary<ProjectId, TProjectCacheEntry>();

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

        private class ImportCompletionCacheService : IImportCompletionCacheService<TProjectCacheEntry, TMetadataCacheEntry>
        {
            public IDictionary<string, TMetadataCacheEntry> PEItemsCache { get; }

            public IDictionary<ProjectId, TProjectCacheEntry> ProjectItemsCache { get; }

            public ImportCompletionCacheService(
                ConcurrentDictionary<string, TMetadataCacheEntry> peCache,
                ConcurrentDictionary<ProjectId, TProjectCacheEntry> projectCache)
            {
                PEItemsCache = peCache;
                ProjectItemsCache = projectCache;
            }
        }
    }
}
