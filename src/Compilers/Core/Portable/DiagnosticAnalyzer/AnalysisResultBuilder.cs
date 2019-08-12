// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Stores the results of analyzer execution:
    /// 1. Local and non-local diagnostics, per-analyzer.
    /// 2. Analyzer execution times, if requested.
    /// </summary>
    internal sealed class AnalysisResultBuilder
    {
        private readonly object _gate = new object();
        private readonly Dictionary<DiagnosticAnalyzer, TimeSpan> _analyzerExecutionTimeOpt;
        private readonly HashSet<DiagnosticAnalyzer> _completedAnalyzers;
        private readonly Dictionary<DiagnosticAnalyzer, AnalyzerActionCounts> _analyzerActionCounts;

        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>> _localSemanticDiagnosticsOpt = null;
        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>> _localSyntaxDiagnosticsOpt = null;
        private Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder> _nonLocalDiagnosticsOpt = null;

        internal AnalysisResultBuilder(bool logAnalyzerExecutionTime, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            _analyzerExecutionTimeOpt = logAnalyzerExecutionTime ? CreateAnalyzerExecutionTimeMap(analyzers) : null;
            _completedAnalyzers = new HashSet<DiagnosticAnalyzer>();
            _analyzerActionCounts = new Dictionary<DiagnosticAnalyzer, AnalyzerActionCounts>(analyzers.Length);
        }

        private static Dictionary<DiagnosticAnalyzer, TimeSpan> CreateAnalyzerExecutionTimeMap(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var map = new Dictionary<DiagnosticAnalyzer, TimeSpan>(analyzers.Length);
            foreach (var analyzer in analyzers)
            {
                map[analyzer] = default(TimeSpan);
            }

            return map;
        }

        public TimeSpan GetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer)
        {
            Debug.Assert(_analyzerExecutionTimeOpt != null);

            lock (_gate)
            {
                return _analyzerExecutionTimeOpt[analyzer];
            }
        }

        internal ImmutableArray<DiagnosticAnalyzer> GetPendingAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            lock (_gate)
            {
                ArrayBuilder<DiagnosticAnalyzer> builder = null;
                foreach (var analyzer in analyzers)
                {
                    if (!_completedAnalyzers.Contains(analyzer))
                    {
                        builder = builder ?? ArrayBuilder<DiagnosticAnalyzer>.GetInstance();
                        builder.Add(analyzer);
                    }
                }

                return builder != null ? builder.ToImmutableAndFree() : ImmutableArray<DiagnosticAnalyzer>.Empty;
            }
        }

        internal void ApplySuppressionsAndStoreAnalysisResult(AnalysisScope analysisScope, AnalyzerDriver driver, Compilation compilation, Func<DiagnosticAnalyzer, AnalyzerActionCounts> getAnalyzerActionCounts, bool fullAnalysisResultForAnalyzersInScope)
        {
            Debug.Assert(!fullAnalysisResultForAnalyzersInScope || analysisScope.FilterTreeOpt == null, "Full analysis result cannot come from partial (tree) analysis.");

            foreach (var analyzer in analysisScope.Analyzers)
            {
                // Dequeue reported analyzer diagnostics from the driver and store them in our maps.
                var syntaxDiagnostics = driver.DequeueLocalDiagnosticsAndApplySuppressions(analyzer, syntax: true, compilation: compilation);
                var semanticDiagnostics = driver.DequeueLocalDiagnosticsAndApplySuppressions(analyzer, syntax: false, compilation: compilation);
                var compilationDiagnostics = driver.DequeueNonLocalDiagnosticsAndApplySuppressions(analyzer, compilation);

                lock (_gate)
                {
                    if (_completedAnalyzers.Contains(analyzer))
                    {
                        // Already stored full analysis result for this analyzer.
                        continue;
                    }

                    if (syntaxDiagnostics.Length > 0 || semanticDiagnostics.Length > 0 || compilationDiagnostics.Length > 0 || fullAnalysisResultForAnalyzersInScope)
                    {
                        UpdateLocalDiagnostics_NoLock(analyzer, syntaxDiagnostics, fullAnalysisResultForAnalyzersInScope, ref _localSyntaxDiagnosticsOpt);
                        UpdateLocalDiagnostics_NoLock(analyzer, semanticDiagnostics, fullAnalysisResultForAnalyzersInScope, ref _localSemanticDiagnosticsOpt);
                        UpdateNonLocalDiagnostics_NoLock(analyzer, compilationDiagnostics, fullAnalysisResultForAnalyzersInScope);
                    }

                    if (_analyzerExecutionTimeOpt != null)
                    {
                        var timeSpan = driver.ResetAnalyzerExecutionTime(analyzer);
                        _analyzerExecutionTimeOpt[analyzer] = fullAnalysisResultForAnalyzersInScope ?
                            timeSpan :
                            _analyzerExecutionTimeOpt[analyzer] + timeSpan;
                    }

                    if (!_analyzerActionCounts.ContainsKey(analyzer))
                    {
                        _analyzerActionCounts.Add(analyzer, getAnalyzerActionCounts(analyzer));
                    }

                    if (fullAnalysisResultForAnalyzersInScope)
                    {
                        _completedAnalyzers.Add(analyzer);
                    }
                }
            }
        }

        private void UpdateLocalDiagnostics_NoLock(
            DiagnosticAnalyzer analyzer,
            ImmutableArray<Diagnostic> diagnostics,
            bool overwrite,
            ref Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>> lazyLocalDiagnostics)
        {
            if (diagnostics.IsEmpty)
            {
                return;
            }

            lazyLocalDiagnostics = lazyLocalDiagnostics ?? new Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>();

            foreach (var diagsByTree in diagnostics.GroupBy(d => d.Location.SourceTree))
            {
                var tree = diagsByTree.Key;

                Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder> allDiagnostics;
                if (!lazyLocalDiagnostics.TryGetValue(tree, out allDiagnostics))
                {
                    allDiagnostics = new Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>();
                    lazyLocalDiagnostics[tree] = allDiagnostics;
                }

                ImmutableArray<Diagnostic>.Builder analyzerDiagnostics;
                if (!allDiagnostics.TryGetValue(analyzer, out analyzerDiagnostics))
                {
                    analyzerDiagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
                    allDiagnostics[analyzer] = analyzerDiagnostics;
                }

                if (overwrite)
                {
                    analyzerDiagnostics.Clear();
                }

                analyzerDiagnostics.AddRange(diagsByTree);
            }
        }

        private void UpdateNonLocalDiagnostics_NoLock(DiagnosticAnalyzer analyzer, ImmutableArray<Diagnostic> diagnostics, bool overwrite)
        {
            if (diagnostics.IsEmpty)
            {
                return;
            }

            _nonLocalDiagnosticsOpt = _nonLocalDiagnosticsOpt ?? new Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>();

            ImmutableArray<Diagnostic>.Builder currentDiagnostics;
            if (!_nonLocalDiagnosticsOpt.TryGetValue(analyzer, out currentDiagnostics))
            {
                currentDiagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
                _nonLocalDiagnosticsOpt[analyzer] = currentDiagnostics;
            }

            if (overwrite)
            {
                currentDiagnostics.Clear();
            }

            currentDiagnostics.AddRange(diagnostics);
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics(AnalysisScope analysisScope, bool getLocalDiagnostics, bool getNonLocalDiagnostics)
        {
            lock (_gate)
            {
                return GetDiagnostics_NoLock(analysisScope, getLocalDiagnostics, getNonLocalDiagnostics);
            }
        }

        private ImmutableArray<Diagnostic> GetDiagnostics_NoLock(AnalysisScope analysisScope, bool getLocalDiagnostics, bool getNonLocalDiagnostics)
        {
            Debug.Assert(getLocalDiagnostics || getNonLocalDiagnostics);

            var builder = ImmutableArray.CreateBuilder<Diagnostic>();
            if (getLocalDiagnostics)
            {
                if (!analysisScope.IsTreeAnalysis)
                {
                    AddAllLocalDiagnostics_NoLock(_localSyntaxDiagnosticsOpt, analysisScope, builder);
                    AddAllLocalDiagnostics_NoLock(_localSemanticDiagnosticsOpt, analysisScope, builder);
                }
                else if (analysisScope.IsSyntaxOnlyTreeAnalysis)
                {
                    AddLocalDiagnosticsForPartialAnalysis_NoLock(_localSyntaxDiagnosticsOpt, analysisScope, builder);
                }
                else
                {
                    AddLocalDiagnosticsForPartialAnalysis_NoLock(_localSemanticDiagnosticsOpt, analysisScope, builder);
                }
            }

            if (getNonLocalDiagnostics && _nonLocalDiagnosticsOpt != null)
            {
                AddDiagnostics_NoLock(_nonLocalDiagnosticsOpt, analysisScope, builder);
            }

            return builder.ToImmutableArray();
        }

        private static void AddAllLocalDiagnostics_NoLock(
            Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>> localDiagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
        {
            if (localDiagnostics != null)
            {
                foreach (var localDiagsByTree in localDiagnostics.Values)
                {
                    AddDiagnostics_NoLock(localDiagsByTree, analysisScope, builder);
                }
            }
        }

        private static void AddLocalDiagnosticsForPartialAnalysis_NoLock(
            Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>> localDiagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
        {
            Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder> diagnosticsForTree;
            if (localDiagnostics != null && localDiagnostics.TryGetValue(analysisScope.FilterTreeOpt, out diagnosticsForTree))
            {
                AddDiagnostics_NoLock(diagnosticsForTree, analysisScope, builder);
            }
        }

        private static void AddDiagnostics_NoLock(
            Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder> diagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
        {
            Debug.Assert(diagnostics != null);

            foreach (var analyzer in analysisScope.Analyzers)
            {
                ImmutableArray<Diagnostic>.Builder diagnosticsByAnalyzer;
                if (diagnostics.TryGetValue(analyzer, out diagnosticsByAnalyzer))
                {
                    builder.AddRange(diagnosticsByAnalyzer);
                }
            }
        }

        internal AnalysisResult ToAnalysisResult(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localSyntaxDiagnostics;
            ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localSemanticDiagnostics;
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> nonLocalDiagnostics;

            var analyzersSet = analyzers.ToImmutableHashSet();
            lock (_gate)
            {
                localSyntaxDiagnostics = GetImmutable(analyzersSet, _localSyntaxDiagnosticsOpt);
                localSemanticDiagnostics = GetImmutable(analyzersSet, _localSemanticDiagnosticsOpt);
                nonLocalDiagnostics = GetImmutable(analyzersSet, _nonLocalDiagnosticsOpt);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var analyzerTelemetryInfo = GetTelemetryInfo(analyzers, cancellationToken);
            return new AnalysisResult(analyzers, localSyntaxDiagnostics, localSemanticDiagnostics, nonLocalDiagnostics, analyzerTelemetryInfo);
        }

        private static ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> GetImmutable(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>> localDiagnosticsOpt)
        {
            if (localDiagnosticsOpt == null)
            {
                return ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>.Empty;
            }

            var builder = ImmutableDictionary.CreateBuilder<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>();
            var perTreeBuilder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>();

            foreach (var diagnosticsByTree in localDiagnosticsOpt)
            {
                var tree = diagnosticsByTree.Key;
                foreach (var diagnosticsByAnalyzer in diagnosticsByTree.Value)
                {
                    if (analyzers.Contains(diagnosticsByAnalyzer.Key))
                    {
                        perTreeBuilder.Add(diagnosticsByAnalyzer.Key, diagnosticsByAnalyzer.Value.ToImmutable());
                    }
                }

                builder.Add(tree, perTreeBuilder.ToImmutable());
                perTreeBuilder.Clear();
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> GetImmutable(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder> nonLocalDiagnosticsOpt)
        {
            if (nonLocalDiagnosticsOpt == null)
            {
                return ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>.Empty;
            }

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>();
            foreach (var diagnosticsByAnalyzer in nonLocalDiagnosticsOpt)
            {
                if (analyzers.Contains(diagnosticsByAnalyzer.Key))
                {
                    builder.Add(diagnosticsByAnalyzer.Key, diagnosticsByAnalyzer.Value.ToImmutable());
                }
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> GetTelemetryInfo(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerTelemetryInfo>();

            lock (_gate)
            {
                foreach (var analyzer in analyzers)
                {
                    var actionCounts = _analyzerActionCounts[analyzer];
                    var suppressionActionCounts = analyzer is DiagnosticSuppressor ? 1 : 0;
                    var executionTime = _analyzerExecutionTimeOpt != null ? _analyzerExecutionTimeOpt[analyzer] : default(TimeSpan);
                    var telemetryInfo = new AnalyzerTelemetryInfo(actionCounts, suppressionActionCounts, executionTime);
                    builder.Add(analyzer, telemetryInfo);
                }
            }

            return builder.ToImmutable();
        }
    }
}
