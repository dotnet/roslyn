// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal abstract class AbstractImportCompletionCacheServiceFactory<TProject, TPortableExecutable> : IWorkspaceServiceFactory
    {
        private readonly ConcurrentDictionary<string, TPortableExecutable> _peItemsCache
            = new ConcurrentDictionary<string, TPortableExecutable>();

        private readonly ConcurrentDictionary<ProjectId, TProject> _projectItemsCache
            = new ConcurrentDictionary<ProjectId, TProject>();

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

        private class ImportCompletionCacheService : IImportCompletionCacheService<TProject, TPortableExecutable>
        {
            public IDictionary<string, TPortableExecutable> PEItemsCache { get; }

            public IDictionary<ProjectId, TProject> ProjectItemsCache { get; }

            public ImportCompletionCacheService(
                ConcurrentDictionary<string, TPortableExecutable> peCache,
                ConcurrentDictionary<ProjectId, TProject> projectCache)
            {
                PEItemsCache = peCache;
                ProjectItemsCache = projectCache;
            }
        }
    }
}
