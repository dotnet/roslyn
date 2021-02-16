// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DiagnosticCache
{
    internal sealed partial class VisualStudioDiagnosticCacheService
    {
        private const string DiagnosticsUpdatedEventName = "CachedDiagnosticsUpdated";

        private readonly object _gate = new();

        // This is null from when live analysis started until a new solution is opened,
        // which means no cached diagnostic update is allowed.
        private string? _currentSolutionPath;

        // Keeps track of all documents we pushed cached diagnostics for.
        private readonly Dictionary<DocumentId, DiagnosticsUpdatedArgs> _requestedDocuments = new();

        // Keeps track of documents encountered for live analysis.
        private readonly HashSet<DocumentId> _analyzedDocuments = new();

        private readonly TaskQueue _errorListUpdateQueue;

        public event EventHandler<DiagnosticsUpdatedArgs>? CachedDiagnosticsUpdated;

        private void TryUpdateDiagnosticsLoadedFromCache(Document document, ImmutableArray<DiagnosticData> diagnostics)
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
                if (solutionPath == _currentSolutionPath && !_requestedDocuments.ContainsKey(document.Id))
                {
                    var eventArg = DiagnosticsUpdatedArgs.DiagnosticsCreated(
                            new CachedDiagnosticsUpdateArgsId(document.Id), document.Project.Solution.Workspace, document.Project.Solution, document.Project.Id, document.Id, diagnostics);
                    _requestedDocuments.Add(document.Id, eventArg);
                    _ = _errorListUpdateQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
                    {
                        if (_diagnosticService is DiagnosticService s)
                        {
#pragma warning disable RS0043 // Do not call 'GetTestAccessor()'
                            s.GetTestAccessor().EventListenerTracker.EnsureEventListener(_workspace, s);
#pragma warning restore RS0043 // Do not call 'GetTestAccessor()'
                        }
                        CachedDiagnosticsUpdated?.Invoke(this, eventArg);
                        Log("PushCachedDiagnostics", diagnostics.Length.ToString());
                    }, CancellationToken.None);

                }
            }
        }

        public bool TryGetLoadedCachedDiagnostics(DocumentId documentId, out ImmutableArray<DiagnosticData> cachedDiagnostics)
        {
            lock (_gate)
            {
                if (_requestedDocuments.TryGetValue(documentId, out var createdArgs))
                {
                    cachedDiagnostics = createdArgs.GetPushDiagnostics(createdArgs.Workspace, InternalDiagnosticsOptions.NormalDiagnosticMode);
                    return true;
                }

                cachedDiagnostics = default;
                return false;
            }
        }

        private bool OnLiveAnalysisStarted(Document document)
        {
            lock (_gate)
            {
                var solutionPath = document.Project.Solution.FilePath;
                // _currentSolutionPath == null means live analysis already started.
                if (_currentSolutionPath != null && solutionPath == _currentSolutionPath)
                {
                    var createdArgs = _requestedDocuments.Values.ToImmutableArray();
                    _currentSolutionPath = null;
                    _requestedDocuments.Clear();

                    // Schedule update to clear all cached diagnostics from DiagnosticService
                    _ = _errorListUpdateQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
                    {
                        foreach (var args in createdArgs)
                        {
                            CachedDiagnosticsUpdated?.Invoke(this, GetFromCreate(args));
                        }

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
                    var createdArgs = _requestedDocuments.Values.ToImmutableArray();
                    _currentSolutionPath = e.NewSolution.FilePath;
                    _requestedDocuments.Clear();
                    _analyzedDocuments.Clear();

                    _ = _errorListUpdateQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
                    {
                        foreach (var args in createdArgs)
                        {
                            CachedDiagnosticsUpdated?.Invoke(this, GetFromCreate(args));
                        }

                        Log("ClearCachedDiags", "NewSolution");
                    }, CancellationToken.None);
                }
            }
        }

        private static DiagnosticsUpdatedArgs GetFromCreate(DiagnosticsUpdatedArgs args)
        {
            return DiagnosticsUpdatedArgs.DiagnosticsRemoved(args.Id, args.Workspace, args.Solution, args.ProjectId, args.DocumentId);
        }

        private class CachedDiagnosticsUpdateArgsId : BuildToolId.Base<DocumentId>, ILoadedFromCache
        {
            public CachedDiagnosticsUpdateArgsId(DocumentId documentId)
                : base(documentId)
            {
            }

            public override string BuildTool => nameof(VisualStudioDiagnosticCacheService);
        }
    }
}
