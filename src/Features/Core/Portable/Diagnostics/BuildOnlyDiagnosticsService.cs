// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

[ExportWorkspaceServiceFactory(typeof(IBuildOnlyDiagnosticsService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class BuildOnlyDiagnosticsServiceFactory(
    IAsynchronousOperationListenerProvider asynchronousOperationProvider) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new BuildOnlyDiagnosticsService(workspaceServices.Workspace, asynchronousOperationProvider.GetListener(FeatureAttribute.Workspace));

    private sealed class BuildOnlyDiagnosticsService : IBuildOnlyDiagnosticsService, IDisposable
    {
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private readonly AsyncBatchingWorkQueue<WorkspaceChangeEventArgs> _workQueue;

        private readonly SemaphoreSlim _gate = new(initialCount: 1);
        private readonly Dictionary<DocumentId, ImmutableArray<DiagnosticData>> _documentDiagnostics = [];

        public BuildOnlyDiagnosticsService(
            Workspace workspace,
            IAsynchronousOperationListener asyncListener)
        {
            _workQueue = new AsyncBatchingWorkQueue<WorkspaceChangeEventArgs>(
                TimeSpan.Zero,
                ProcessWorkQueueAsync,
                asyncListener,
                _disposalTokenSource.Token);
            workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        public void Dispose()
            => _disposalTokenSource.Dispose();

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            // Keep this switch in sync with the switch in ProcessWorkQueueAsync
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                    // Cancel existing work as we're going to clear out everything anyways, so no point processing any
                    // document or project work.
                    _workQueue.AddWork(e, cancelExistingWork: true);
                    break;
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.DocumentRemoved:
                case WorkspaceChangeKind.DocumentReloaded:
                case WorkspaceChangeKind.AdditionalDocumentRemoved:
                case WorkspaceChangeKind.AdditionalDocumentReloaded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                    _workQueue.AddWork(e);
                    break;
            }
        }

        private async ValueTask ProcessWorkQueueAsync(ImmutableSegmentedList<WorkspaceChangeEventArgs> list, CancellationToken cancellationToken)
        {
            foreach (var e in list)
            {
                // Keep this switch in sync with the switch in OnWorkspaceChanged
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                    case WorkspaceChangeKind.SolutionRemoved:
                        await ClearAllDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.ProjectReloaded:
                    case WorkspaceChangeKind.ProjectRemoved:
                        await ClearDiagnosticsAsync(e.OldSolution.GetProject(e.ProjectId), cancellationToken).ConfigureAwait(false);
                        break;

                    case WorkspaceChangeKind.DocumentRemoved:
                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.AdditionalDocumentRemoved:
                    case WorkspaceChangeKind.AdditionalDocumentReloaded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                        await ClearDiagnosticsAsync(e.DocumentId, cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
        }

        public async Task AddBuildOnlyDiagnosticsAsync(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (documentId != null)
                    _documentDiagnostics[documentId] = diagnostics;
            }
        }

        private async Task ClearAllDiagnosticsAsync(CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _documentDiagnostics.Clear();
            }
        }

        private async Task ClearDiagnosticsAsync(DocumentId? documentId, CancellationToken cancellationToken)
        {
            if (documentId == null)
                return;

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _documentDiagnostics.Remove(documentId);
            }
        }

        private async Task ClearDiagnosticsAsync(Project? project, CancellationToken cancellationToken)
        {
            if (project == null)
                return;

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var documentId in project.DocumentIds)
                    _documentDiagnostics.Remove(documentId);
            }
        }

        public Task ClearBuildOnlyDiagnosticsAsync(Project project, DocumentId? documentId, CancellationToken cancellationToken)
        {
            if (documentId != null)
                return ClearDiagnosticsAsync(documentId, cancellationToken);
            else
                return ClearDiagnosticsAsync(project, cancellationToken);
        }

        public async ValueTask<ImmutableArray<DiagnosticData>> GetBuildOnlyDiagnosticsAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                return _documentDiagnostics.TryGetValue(documentId, out var diagnostics) ? diagnostics : [];
            }
        }
    }
}
