// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Diagnostic update source for reporting workspace host specific diagnostics,
/// which may not be related to any given project/document in the solution.
/// For example, these include diagnostics generated for exceptions from third party analyzers.
/// </summary>
internal abstract class AbstractHostDiagnosticUpdateSource
{
    private ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>> _analyzerHostDiagnosticsMap =
        ImmutableDictionary<DiagnosticAnalyzer, ImmutableHashSet<DiagnosticData>>.Empty;

    public abstract Workspace Workspace { get; }

    public void ClearAnalyzerReferenceDiagnostics(AnalyzerFileReference analyzerReference, string language, ProjectId projectId)
    {
        // Perf: if we don't have any diagnostics at all, just return right away; this avoids loading the analyzers
        // which may have not been loaded if you didn't do too much in your session.
        if (_analyzerHostDiagnosticsMap.Count == 0)
            return;

        var analyzers = analyzerReference.GetAnalyzers(language);
        AddArgsToClearAnalyzerDiagnostics(analyzers, projectId);
    }

    public void AddArgsToClearAnalyzerDiagnostics(ImmutableArray<DiagnosticAnalyzer> analyzers, ProjectId projectId)
    {
        foreach (var analyzer in analyzers)
            AddArgsToClearAnalyzerDiagnostics(analyzer, projectId);
    }

    public void AddArgsToClearAnalyzerDiagnostics(ProjectId projectId)
    {
        foreach (var (analyzer, _) in _analyzerHostDiagnosticsMap)
            AddArgsToClearAnalyzerDiagnostics(analyzer, projectId);
    }

    private void AddArgsToClearAnalyzerDiagnostics(DiagnosticAnalyzer analyzer, ProjectId projectId)
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
            }
        }
        else
        {
            ImmutableInterlocked.TryRemove(ref _analyzerHostDiagnosticsMap, analyzer, out _);
        }
    }

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
                diagnostics = [];
            }

            return diagnostics;
        }
    }
}
