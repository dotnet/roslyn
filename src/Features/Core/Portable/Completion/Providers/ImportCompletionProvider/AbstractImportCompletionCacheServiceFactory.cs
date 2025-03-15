// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractImportCompletionCacheServiceFactory<TProjectCacheEntry, TMetadataCacheEntry>
    : IWorkspaceServiceFactory
    where TProjectCacheEntry : class
    where TMetadataCacheEntry : class
{
    private static readonly ConditionalWeakTable<MetadataId, TMetadataCacheEntry> s_peItemsCache = new();

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
        var workQueue = new AsyncBatchingWorkQueue<Project>(
            TimeSpan.FromSeconds(1),
            _processBatchAsync,
            _listenerProvider.GetListener(FeatureAttribute.CompletionSet),
            _disposalToken);

        return new ImportCompletionCacheService(workQueue);
    }

    private sealed class ImportCompletionCacheService(
        AsyncBatchingWorkQueue<Project> workQueue) : IImportCompletionCacheService<TProjectCacheEntry, TMetadataCacheEntry>
    {
        public AsyncBatchingWorkQueue<Project> WorkQueue { get; } = workQueue;
    }
}
