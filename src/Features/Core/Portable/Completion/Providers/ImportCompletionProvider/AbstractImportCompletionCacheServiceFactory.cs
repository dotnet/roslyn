// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractImportCompletionCacheServiceFactory<TProjectCacheEntry, TMetadataCacheEntry> : IWorkspaceServiceFactory
{
    private readonly ConcurrentDictionary<string, TMetadataCacheEntry> _peItemsCache = [];

    private readonly ConcurrentDictionary<ProjectId, TProjectCacheEntry> _projectItemsCache = [];

    private readonly IAsynchronousOperationListenerProvider _listenerProvider;
    private readonly Func<ImmutableSegmentedList<Project>, CancellationToken, ValueTask> _processBatchAsync;
    private readonly CancellationToken _disposalToken;

    protected AbstractImportCompletionCacheServiceFactory(
        IAsynchronousOperationListenerProvider listenerProvider,
        Func<ImmutableSegmentedList<Project>, CancellationToken, ValueTask> processBatchAsync
        , CancellationToken disposalToken)
    {
        _listenerProvider = listenerProvider;
        _processBatchAsync = processBatchAsync;
        _disposalToken = disposalToken;
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

        var workQueue = new AsyncBatchingWorkQueue<Project>(
            TimeSpan.FromSeconds(1),
            _processBatchAsync,
            _listenerProvider.GetListener(FeatureAttribute.CompletionSet),
            _disposalToken);

        return new ImportCompletionCacheService(
            _peItemsCache, _projectItemsCache, workQueue);
    }

    private void OnCacheFlushRequested(object? sender, EventArgs e)
    {
        _peItemsCache.Clear();
        _projectItemsCache.Clear();
    }

    private class ImportCompletionCacheService(
        ConcurrentDictionary<string, TMetadataCacheEntry> peCache,
        ConcurrentDictionary<ProjectId, TProjectCacheEntry> projectCache,
        AsyncBatchingWorkQueue<Project> workQueue) : IImportCompletionCacheService<TProjectCacheEntry, TMetadataCacheEntry>
    {
        public IDictionary<string, TMetadataCacheEntry> PEItemsCache { get; } = peCache;

        public IDictionary<ProjectId, TProjectCacheEntry> ProjectItemsCache { get; } = projectCache;

        public AsyncBatchingWorkQueue<Project> WorkQueue { get; } = workQueue;
    }
}
