// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Stores the results of analyzer execution:
    /// 1. Local and non-local diagnostics, per-analyzer.
    /// 2. Analyzer execution times, if requested.
    /// </summary>
    internal partial class AnalysisResult
    {
        private readonly object _gate = new object();
        private readonly Dictionary<DiagnosticAnalyzer, TimeSpan> _analyzerExecutionTimeOpt;

        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, List<Diagnostic>>> _localSemanticDiagnosticsOpt = null;
        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, List<Diagnostic>>> _localSyntaxDiagnosticsOpt = null;
        private Dictionary<DiagnosticAnalyzer, List<Diagnostic>> _nonLocalDiagnosticsOpt = null;

        public AnalysisResult(bool logAnalyzerExecutionTime, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            _analyzerExecutionTimeOpt = logAnalyzerExecutionTime ? CreateAnalyzerExecutionTimeMap(analyzers) : null;
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

        public void StoreAnalysisResult(AnalysisScope analysisScope, AnalyzerDriver driver, Compilation compilation)
        {
            foreach (var analyzer in analysisScope.Analyzers)
            {
                // Dequeue reported analyzer diagnostics from the driver and store them in our maps.
                var syntaxDiagnostics = driver.DequeueLocalDiagnostics(analyzer, syntax: true, compilation: compilation);
                var semanticDiagnostics = driver.DequeueLocalDiagnostics(analyzer, syntax: false, compilation: compilation);
                var compilationDiagnostics = driver.DequeueNonLocalDiagnostics(analyzer, compilation);

                lock (_gate)
                {
                    if (syntaxDiagnostics.Length > 0 || semanticDiagnostics.Length > 0 || compilationDiagnostics.Length > 0)
                    {
                        UpdateLocalDiagnostics_NoLock(analyzer, syntaxDiagnostics, ref _localSyntaxDiagnosticsOpt);
                        UpdateLocalDiagnostics_NoLock(analyzer, semanticDiagnostics, ref _localSemanticDiagnosticsOpt);
                        UpdateNonLocalDiagnostics_NoLock(analyzer, compilationDiagnostics);
                    }

                    if (_analyzerExecutionTimeOpt != null)
                    {
                        _analyzerExecutionTimeOpt[analyzer] += driver.ResetAnalyzerExecutionTime(analyzer);
                    }
                }
            }
        }

        private void UpdateLocalDiagnostics_NoLock(DiagnosticAnalyzer analyzer, ImmutableArray<Diagnostic> diagnostics, ref Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, List<Diagnostic>>> lazyLocalDiagnostics)
        {
            if (diagnostics.IsEmpty)
            {
                return;
            }

            lazyLocalDiagnostics = lazyLocalDiagnostics ?? new Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, List<Diagnostic>>>();

            foreach (var diagsByTree in diagnostics.GroupBy(d => d.Location.SourceTree))
            {
                var tree = diagsByTree.Key;

                Dictionary<DiagnosticAnalyzer, List<Diagnostic>> allDiagnostics;
                if (!lazyLocalDiagnostics.TryGetValue(tree, out allDiagnostics))
                {
                    allDiagnostics = new Dictionary<DiagnosticAnalyzer, List<Diagnostic>>();
                    lazyLocalDiagnostics[tree] = allDiagnostics;
                }

                List<Diagnostic> analyzerDiagnostics;
                if (!allDiagnostics.TryGetValue(analyzer, out analyzerDiagnostics))
                {
                    analyzerDiagnostics = new List<Diagnostic>();
                    allDiagnostics[analyzer] = analyzerDiagnostics;
                }

                analyzerDiagnostics.AddRange(diagsByTree);
            }
        }

        private void UpdateNonLocalDiagnostics_NoLock(DiagnosticAnalyzer analyzer, ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsEmpty)
            {
                return;
            }

            _nonLocalDiagnosticsOpt = _nonLocalDiagnosticsOpt ?? new Dictionary<DiagnosticAnalyzer, List<Diagnostic>>();

            List<Diagnostic> currentDiagnostics;
            if (!_nonLocalDiagnosticsOpt.TryGetValue(analyzer, out currentDiagnostics))
            {
                currentDiagnostics = new List<Diagnostic>();
                _nonLocalDiagnosticsOpt[analyzer] = currentDiagnostics;
            }

            currentDiagnostics.AddRange(diagnostics);
        }

        public ImmutableArray<Diagnostic> GetDiagnostics(AnalysisScope analysisScope, bool getLocalDiagnostics, bool getNonLocalDiagnostics)
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
            Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, List<Diagnostic>>> localDiagnostics,
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
            Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, List<Diagnostic>>> localDiagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
        {
            Dictionary<DiagnosticAnalyzer, List<Diagnostic>> diagnosticsForTree;
            if (localDiagnostics != null && localDiagnostics.TryGetValue(analysisScope.FilterTreeOpt, out diagnosticsForTree))
            {
                AddDiagnostics_NoLock(diagnosticsForTree, analysisScope, builder);
            }
        }

        private static void AddDiagnostics_NoLock(
            Dictionary<DiagnosticAnalyzer, List<Diagnostic>> diagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
        {
            Debug.Assert(diagnostics != null);

            foreach (var analyzer in analysisScope.Analyzers)
            {
                List<Diagnostic> diagnosticsByAnalyzer;
                if (diagnostics.TryGetValue(analyzer, out diagnosticsByAnalyzer))
                {
                    builder.AddRange(diagnosticsByAnalyzer);
                }
            }
        }
    }
}
