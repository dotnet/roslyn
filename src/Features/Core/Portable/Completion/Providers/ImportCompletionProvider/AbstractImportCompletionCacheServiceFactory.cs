﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            = new();

        private readonly ConcurrentDictionary<ProjectId, TProjectCacheEntry> _projectItemsCache
            = new();

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

        private void OnCacheFlushRequested(object? sender, EventArgs e)
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
