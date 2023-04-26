// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportWorkspaceServiceFactory(typeof(IBuildOnlyDiagnosticsService), ServiceLayer.Default), Shared]
    internal sealed class BuildOnlyDiagnosticsServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BuildOnlyDiagnosticsServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new BuildOnlyDiagnosticsService(workspaceServices.Workspace);

        private sealed class BuildOnlyDiagnosticsService : IBuildOnlyDiagnosticsService
        {
            private readonly object _gate = new();
            private readonly Dictionary<DocumentId, ImmutableArray<DiagnosticData>> _documentDiagnostics = new();
            private readonly Dictionary<ProjectId, ImmutableArray<DiagnosticData>> _projectDiagnostics = new();

            public BuildOnlyDiagnosticsService(Workspace workspace)
            {
                workspace.WorkspaceChanged += OnWorkspaceChanged;
            }

            private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                    case WorkspaceChangeKind.SolutionRemoved:
                        ClearAllDiagnostics();
                        break;

                    case WorkspaceChangeKind.ProjectReloaded:
                    case WorkspaceChangeKind.ProjectRemoved:
                        ClearDiagnostics(e.OldSolution.GetProject(e.ProjectId));
                        break;

                    case WorkspaceChangeKind.DocumentRemoved:
                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.AdditionalDocumentRemoved:
                    case WorkspaceChangeKind.AdditionalDocumentReloaded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                        ClearDiagnostics(e.DocumentId);
                        break;
                }
            }

            public void AddBuildOnlyDiagnostics(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableArray<DiagnosticData> diagnostics)
            {
                lock (_gate)
                {
                    if (documentId != null)
                    {
                        _documentDiagnostics[documentId] = diagnostics;
                    }
                    else if (projectId != null)
                    {
                        _projectDiagnostics[projectId] = diagnostics;
                    }
                }
            }

            private void ClearAllDiagnostics()
            {
                lock (_gate)
                {
                    _documentDiagnostics.Clear();
                    _projectDiagnostics.Clear();
                }
            }

            private void ClearDiagnostics(DocumentId? documentId)
            {
                if (documentId == null)
                    return;

                lock (_gate)
                {
                    _documentDiagnostics.Remove(documentId);
                }
            }

            private void ClearDiagnostics(Project? project)
            {
                if (project == null)
                    return;

                lock (_gate)
                {
                    _projectDiagnostics.Remove(project.Id);
                    foreach (var documentId in project.DocumentIds)
                        _documentDiagnostics.Remove(documentId);
                }
            }

            public void ClearBuildOnlyDiagnostics(Solution solution, ProjectId? projectId, DocumentId? documentId)
            {
                if (documentId != null)
                    ClearDiagnostics(documentId);
                else
                    ClearDiagnostics(solution.GetProject(projectId));
            }

            public ImmutableArray<DiagnosticData> GetBuildOnlyDiagnostics(DocumentId documentId)
            {
                lock (_gate)
                {
                    if (_documentDiagnostics.TryGetValue(documentId, out var diagnostics))
                    {
                        return diagnostics;
                    }

                    return ImmutableArray<DiagnosticData>.Empty;
                }
            }

            public ImmutableArray<DiagnosticData> GetBuildOnlyDiagnostics(ProjectId projectId)
            {
                lock (_gate)
                {
                    if (_projectDiagnostics.TryGetValue(projectId, out var diagnostics))
                    {
                        return diagnostics;
                    }

                    return ImmutableArray<DiagnosticData>.Empty;
                }
            }
        }
    }
}
