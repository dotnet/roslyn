// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed class BackgroundCompiler : IDisposable
    {
        private Workspace? _workspace;
        private readonly AsyncBatchingWorkQueue _workQueue;

        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used to keep a strong reference to the built compilations so they are not GC'd")]
        private readonly ConcurrentSet<Compilation> _mostRecentCompilations = new();

        /// <summary>
        /// Token to stop work entirely when this object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposalCancellationSource = new();

        public BackgroundCompiler(Workspace workspace)
        {
            _workspace = workspace;

            // make a scheduler that runs on the thread pool
            var listenerProvider = workspace.Services.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>();

            _workQueue = new AsyncBatchingWorkQueue(
                DelayTimeSpan.NearImmediate,
                BuildCompilationsForVisibleDocumentsAsync,
                listenerProvider.GetListener(),
                _disposalCancellationSource.Token);

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _workspace.DocumentOpened += OnDocumentOpened;
            _workspace.DocumentClosed += OnDocumentClosed;
        }

        public void Dispose()
        {
            _disposalCancellationSource.Cancel();

            _mostRecentCompilations.Clear();

            var workspace = Interlocked.Exchange(ref _workspace, null);
            if (workspace != null)
            {
                workspace.DocumentClosed -= OnDocumentClosed;
                workspace.DocumentOpened -= OnDocumentOpened;
                workspace.WorkspaceChanged -= OnWorkspaceChanged;
            }
        }

        private void OnDocumentOpened(object? sender, DocumentEventArgs args)
            => Rebuild();

        private void OnDocumentClosed(object? sender, DocumentEventArgs args)
            => Rebuild();

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs args)
            => Rebuild();

        private void Rebuild()
        {
            // Stop any work on the current batch and start the next job.
            _workQueue.AddWork(cancelExistingWork: true);
        }

        private async ValueTask BuildCompilationsForVisibleDocumentsAsync(CancellationToken cancellationToken)
        {
            var workspace = _workspace;
            if (workspace is null)
                return;

            using var _ = ArrayBuilder<Compilation>.GetInstance(out var compilations);

            await AddCompilationsForVisibleDocumentsAsync(workspace.CurrentSolution, compilations, cancellationToken).ConfigureAwait(false);

            _mostRecentCompilations.Clear();
            _mostRecentCompilations.AddRange(compilations);
        }

        private static async ValueTask AddCompilationsForVisibleDocumentsAsync(
            Solution solution,
            ArrayBuilder<Compilation> compilations,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trackingService = solution.Services.GetRequiredService<IDocumentTrackingService>();
            var visibleProjectIds = trackingService.GetVisibleDocuments().Select(d => d.ProjectId).ToSet();
            var activeProjectId = trackingService.TryGetActiveDocument()?.ProjectId;

            // Prioritize the project for the active document first.
            await GetCompilationAsync(activeProjectId).ConfigureAwait(false);

            // Then handle any visible documents (as long as we didn't already handle it above).
            foreach (var projectId in visibleProjectIds)
            {
                if (projectId != activeProjectId)
                {
                    await GetCompilationAsync(projectId).ConfigureAwait(false);
                }
            }

            return;

            async ValueTask GetCompilationAsync(ProjectId? projectId)
            {
                var project = solution.GetProject(projectId);
                if (project is null)
                    return;

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                compilations.AddIfNotNull(compilation);
            }
        }
    }
}
