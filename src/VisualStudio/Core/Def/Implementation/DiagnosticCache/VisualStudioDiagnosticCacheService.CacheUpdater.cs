// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DiagnosticCache
{
    internal sealed partial class VisualStudioDiagnosticCacheService
    {
        /// <summary>
        /// Batch processing requests to update cache for per document diagnostics
        /// </summary>
        private class CacheUpdater : GlobalOperationAwareIdleProcessor
        {
            private readonly SemaphoreSlim _gate = new(initialCount: 0);
            private readonly HashSet<DocumentId> _queued = new();

            private readonly Workspace _workspace;
            private readonly IDiagnosticService _diagnosticService;

            public CacheUpdater(
                Workspace workspace,
                IDiagnosticService diagnosticService,
                IGlobalOperationNotificationService globalOperationNotificationService,
                CancellationToken shutdownToken)
                : base(
                    AsynchronousOperationListenerProvider.NullListener,
                    globalOperationNotificationService,
                    (int)TimeSpan.FromSeconds(5).TotalMilliseconds,
                    shutdownToken)
            {
                _workspace = workspace;
                _diagnosticService = diagnosticService;
                _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
                Start();
            }

            protected override async Task ExecuteAsync()
            {
                await GlobalOperationTask.ConfigureAwait(false);

                var workspaceStatusService = _workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                await workspaceStatusService.WaitUntilFullyLoadedAsync(CancellationToken).ConfigureAwait(false);

                ImmutableArray<DocumentId> queued;
                lock (_queued)
                {
                    queued = _queued.ToImmutableArray();
                    _queued.Clear();
                }

                await CacheDiagnosticsAsync(queued, CancellationToken.None).ConfigureAwait(false);
            }

            protected override Task WaitAsync(CancellationToken cancellationToken)
                => _gate.WaitAsync(cancellationToken);

            protected override void PauseOnGlobalOperation()
            {
            }

            /// <summary>
            /// Queue a document to batch caching of diagnostics for.
            /// But it won't start until workspace is fully loaded.
            /// </summary>
            public void QueueUpdate(DocumentId? documentId)
            {
                if (documentId == null)
                {
                    return;
                }

                lock (_queued)
                {
                    _queued.Add(documentId);
                }

                UpdateLastAccessTime();

                if (_gate.CurrentCount == 0)
                {
                    _gate.Release();
                }
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                if (e.Id is ISupportLiveUpdate)
                {
                    QueueUpdate(e.DocumentId);
                }
            }

            private async Task CacheDiagnosticsAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return;
                }

                var solution = _workspace.CurrentSolution;
                foreach (var documentId in documentIds)
                {
                    var document = solution.GetDocument(documentId);
                    if (document != null)
                    {
                        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);
                        var buckets = _diagnosticService.GetPushDiagnosticBuckets(_workspace, document.Project.Id, document.Id, InternalDiagnosticsOptions.NormalDiagnosticMode, cancellationToken);
                        foreach (var bucket in buckets)
                        {
                            // Only cache results from actual live analysis
                            var id = bucket.Id;
                            if (id is ISupportLiveUpdate && id is not CachedDiagnosticsUpdateArgsId)
                            {
                                var diagnostics = await _diagnosticService.GetPushDiagnosticsAsync(
                                    _workspace, document.Project.Id, document.Id, id,
                                    includeSuppressedDiagnostics: false,
                                    diagnosticMode: InternalDiagnosticsOptions.NormalDiagnosticMode,
                                    cancellationToken).ConfigureAwait(false);
                                builder.AddRange(diagnostics);
                            }
                        }

                        await client.TryInvokeAsync<IRemoteDiagnosticCacheService>(
                            solution,
                            (service, solutionInfo, cancellationToken) => service.CacheDiagnosticsAsync(solutionInfo, document.Id, builder.ToImmutable(), cancellationToken),
                            cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
