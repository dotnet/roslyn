// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractImportCompletionCacheServiceFactory<TProjectCacheEntry, TMetadataCacheEntry>(
    IAsynchronousOperationListenerProvider listenerProvider,
    Func<ImmutableSegmentedList<Project>, CancellationToken, ValueTask> processBatchAsync,
    CancellationToken disposalToken)
    : IWorkspaceServiceFactory
    where TProjectCacheEntry : class
    where TMetadataCacheEntry : class
{
    private readonly IAsynchronousOperationListenerProvider _listenerProvider = listenerProvider;
    private readonly Func<ImmutableSegmentedList<Project>, CancellationToken, ValueTask> _processBatchAsync = processBatchAsync;
    private readonly CancellationToken _disposalToken = disposalToken;

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
