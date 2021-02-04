// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DiagnosticCache
{
    internal sealed partial class VisualStudioDiagnosticCacheService
    {
        /// <summary>
        /// Handles updates to DiagnosticService for diagnostics loaded from cache.
        /// Factors like solution load and live analysis status are also taken into account.
        /// </summary>
        private class UpdateTracker : IDiagnosticUpdateSource
        {
            private const string DiagnosticsUpdatedEventName = "CachedDiagnosticsUpdated";

            private readonly object _gate = new();

            // This is null from when live analysis started until a new solution is opened,
            // which means no cached diagnostic update is allowed.
            private string? _currentSolutionPath;

            // Keeps track of all documents we pushed cached diagnostics for.
            private readonly HashSet<DocumentId> _requestedDocuments = new();

            // Keeps track of documents encountered for live analysis.
            private readonly HashSet<DocumentId> _analyzedDocuments = new();

            private readonly TaskQueue _updateQueue;

            public UpdateTracker(
                Workspace workspace,
                IDiagnosticUpdateSourceRegistrationService registrationService,
                IAsynchronousOperationListenerProvider listenerProvider)
            {
                _currentSolutionPath = workspace.CurrentSolution.FilePath;
                _updateQueue = new TaskQueue(listenerProvider.GetListener(nameof(VisualStudioDiagnosticCacheService)), TaskScheduler.Default);
                workspace.WorkspaceChanged += OnWorkspaceChanged;
                registrationService.Register(this);
            }

            public void TryUpdateDiagnosticsLoadedFromCache(Document document, ImmutableArray<DiagnosticData> diagnostics)
            {
                Debug.Assert(!diagnostics.IsDefaultOrEmpty);

                var solutionPath = document.Project.Solution.FilePath;
                if (solutionPath == null)
                {
                    return;
                }

                lock (_gate)
                {
                    // This check also ensures no update being pushed if _currentSolutionPath is null,
                    // which means live analysis already started.
                    if (solutionPath == _currentSolutionPath && _requestedDocuments.Add(document.Id))
                    {
                        _ = _updateQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
                        {
                            var args = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                                new CachedDiagnosticsUpdateArgsId(document.Id), document.Project.Solution.Workspace, document.Project.Solution, document.Project.Id, document.Id, diagnostics);
                            DiagnosticsUpdated?.Invoke(this, args);
                            Log("PushCachedDiagnostics", diagnostics.Length.ToString());
                        }, CancellationToken.None);

                    }
                }
            }

            public bool OnLiveAnalysisStarted(Document document)
            {
                lock (_gate)
                {
                    var solutionPath = document.Project.Solution.FilePath;
                    // _currentSolutionPath == null means live analysis already started.
                    if (_currentSolutionPath != null && solutionPath == _currentSolutionPath)
                    {
                        _currentSolutionPath = null;
                        _requestedDocuments.Clear();

                        // Schedule update to clear all cached diagnostics from DiagnosticService
                        _ = _updateQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
                        {
                            DiagnosticsCleared?.Invoke(this, EventArgs.Empty);
                            Log("ClearCachedDiags", "LiveAnalysis");
                        }, CancellationToken.None);
                    }

                    return _analyzedDocuments.Add(document.Id);
                }
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                if (e.Kind == WorkspaceChangeKind.SolutionAdded)
                {
                    lock (_gate)
                    {
                        _currentSolutionPath = e.NewSolution.FilePath;
                        _requestedDocuments.Clear();
                        _analyzedDocuments.Clear();
                        _ = _updateQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
                        {
                            DiagnosticsCleared?.Invoke(this, EventArgs.Empty);
                            Log("ClearCachedDiags", "NewSolution");
                        }, CancellationToken.None);
                    }
                }
            }

            public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;
            public event EventHandler? DiagnosticsCleared;

            public bool SupportGetDiagnostics => false;

            public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
                => new(ImmutableArray<DiagnosticData>.Empty);
        }

        private class CachedDiagnosticsUpdateArgsId : BuildToolId.Base<DocumentId>, ISupportLiveUpdate
        {
            public CachedDiagnosticsUpdateArgsId(DocumentId documentId)
                : base(documentId)
            {
            }

            public override string BuildTool => nameof(VisualStudioDiagnosticCacheService);
        }
    }
}
