// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Diagnostic update source for reporting workspace host specific diagnostics,
    /// which may not be related to any given project/document in the solution.
    /// For example, these include diagnostics generated for exceptions from third party analyzers.
    /// </summary>
    internal abstract class AbstractHostDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>> _analyzerHostDiagnosticsMap =
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>>.Empty;

        public abstract Workspace Workspace { get; }

        public bool SupportGetDiagnostics => false;

        public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            => new(ImmutableArray<DiagnosticData>.Empty);

        public event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>? DiagnosticsUpdated;
        public event EventHandler DiagnosticsCleared { add { } remove { } }

        public void RaiseDiagnosticsUpdated(ImmutableArray<DiagnosticsUpdatedArgs> args)
        {
            if (!args.IsEmpty)
                DiagnosticsUpdated?.Invoke(this, args);
        }

        public void ReportAnalyzerDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, ProjectId? projectId)
        {
            // check whether we are reporting project specific diagnostic or workspace wide diagnostic
            var solution = Workspace.CurrentSolution;
            var project = projectId != null ? solution.GetProject(projectId) : null;

            // check whether project the diagnostic belong to still exist
            if (projectId != null && project == null)
            {
                // project the diagnostic belong to already removed from the solution.
                // ignore the diagnostic
                return;
            }

            ReportAnalyzerDiagnostic(analyzer, DiagnosticData.Create(solution, diagnostic, project), project);
        }

        public void ReportAnalyzerDiagnostic(DiagnosticAnalyzer analyzer, DiagnosticData diagnosticData, Project? project)
        {
            var raiseDiagnosticsUpdated = true;

            var dxs = ImmutableInterlocked.AddOrUpdate(ref _analyzerHostDiagnosticsMap,
                analyzer,
                ImmutableHashSet.Create(diagnosticData),
                (a, existing) =>
                {
                    var newDiags = existing.Add(diagnosticData);
                    raiseDiagnosticsUpdated = newDiags.Count > existing.Count;
                    return newDiags;
                });

            if (raiseDiagnosticsUpdated)
            {
                RaiseDiagnosticsUpdated(ImmutableArray.Create(MakeCreatedArgs(analyzer, dxs, project)));
            }
        }

        public void ClearAnalyzerReferenceDiagnostics(AnalyzerFileReference analyzerReference, string language, ProjectId projectId)
        {
            // Perf: if we don't have any diagnostics at all, just return right away; this avoids loading the analyzers
            // which may have not been loaded if you didn't do too much in your session.
            if (_analyzerHostDiagnosticsMap.Count == 0)
                return;

            using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;
            var analyzers = analyzerReference.GetAnalyzers(language);
            AddArgsToClearAnalyzerDiagnostics(ref argsBuilder.AsRef(), analyzers, projectId);
            RaiseDiagnosticsUpdated(argsBuilder.ToImmutableAndClear());
        }

        public void AddArgsToClearAnalyzerDiagnostics(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, ImmutableArray<DiagnosticAnalyzer> analyzers, ProjectId projectId)
        {
            foreach (var analyzer in analyzers)
            {
                AddArgsToClearAnalyzerDiagnostics(ref builder, analyzer, projectId);
            }
        }

        public void AddArgsToClearAnalyzerDiagnostics(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, ProjectId projectId)
        {
            foreach (var (analyzer, _) in _analyzerHostDiagnosticsMap)
            {
                AddArgsToClearAnalyzerDiagnostics(ref builder, analyzer, projectId);
            }
        }

        private void AddArgsToClearAnalyzerDiagnostics(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, DiagnosticAnalyzer analyzer, ProjectId projectId)
        {
            if (!_analyzerHostDiagnosticsMap.TryGetValue(analyzer, out var existing))
            {
                return;
            }

            // Check if analyzer is shared by analyzer references from different projects.
            var sharedAnalyzer = existing.Contains(d => d.ProjectId != null && d.ProjectId != projectId);
            if (sharedAnalyzer)
            {
                var newDiags = existing.Where(d => d.ProjectId != projectId).ToImmutableHashSet();
                if (newDiags.Count < existing.Count &&
                    ImmutableInterlocked.TryUpdate(ref _analyzerHostDiagnosticsMap, analyzer, newDiags, existing))
                {
                    var project = Workspace.CurrentSolution.GetProject(projectId);
                    builder.Add(MakeRemovedArgs(analyzer, project));
                }
            }
            else if (ImmutableInterlocked.TryRemove(ref _analyzerHostDiagnosticsMap, analyzer, out existing))
            {
                var project = Workspace.CurrentSolution.GetProject(projectId);
                builder.Add(MakeRemovedArgs(analyzer, project));

                if (existing.Any(d => d.ProjectId == null))
                {
                    builder.Add(MakeRemovedArgs(analyzer, project: null));
                }
            }
        }

        private DiagnosticsUpdatedArgs MakeCreatedArgs(DiagnosticAnalyzer analyzer, ImmutableHashSet<DiagnosticData> items, Project? project)
        {
            return DiagnosticsUpdatedArgs.DiagnosticsCreated(
                CreateId(analyzer, project), Workspace, project?.Solution, project?.Id, documentId: null, diagnostics: items.ToImmutableArray());
        }

        private DiagnosticsUpdatedArgs MakeRemovedArgs(DiagnosticAnalyzer analyzer, Project? project)
        {
            return DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                CreateId(analyzer, project), Workspace, project?.Solution, project?.Id, documentId: null);
        }

        private HostArgsId CreateId(DiagnosticAnalyzer analyzer, Project? project) => new(this, analyzer, project?.Id);

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor(AbstractHostDiagnosticUpdateSource abstractHostDiagnosticUpdateSource)
        {
            private readonly AbstractHostDiagnosticUpdateSource _abstractHostDiagnosticUpdateSource = abstractHostDiagnosticUpdateSource;

            internal ImmutableArray<DiagnosticData> GetReportedDiagnostics()
                => _abstractHostDiagnosticUpdateSource._analyzerHostDiagnosticsMap.Values.Flatten().ToImmutableArray();

            internal ImmutableHashSet<DiagnosticData> GetReportedDiagnostics(DiagnosticAnalyzer analyzer)
            {
                if (!_abstractHostDiagnosticUpdateSource._analyzerHostDiagnosticsMap.TryGetValue(analyzer, out var diagnostics))
                {
                    diagnostics = ImmutableHashSet<DiagnosticData>.Empty;
                }

                return diagnostics;
            }
        }

        private sealed class HostArgsId(AbstractHostDiagnosticUpdateSource source, DiagnosticAnalyzer analyzer, ProjectId? projectId) : AnalyzerUpdateArgsId(analyzer)
        {
            private readonly AbstractHostDiagnosticUpdateSource _source = source;
            private readonly ProjectId? _projectId = projectId;

            public override bool Equals(object? obj)
            {
                if (obj is not HostArgsId other)
                {
                    return false;
                }

                return _source == other._source && _projectId == other._projectId && base.Equals(obj);
            }

            public override int GetHashCode()
                => Hash.Combine(_source.GetHashCode(), Hash.Combine(_projectId == null ? 1 : _projectId.GetHashCode(), base.GetHashCode()));
        }
    }
}
