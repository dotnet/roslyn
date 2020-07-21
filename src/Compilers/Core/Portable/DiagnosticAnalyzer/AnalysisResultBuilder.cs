// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Stores the results of analyzer execution:
    /// 1. Local and non-local diagnostics, per-analyzer.
    /// 2. Analyzer execution times, if requested.
    /// </summary>
    internal sealed class AnalysisResultBuilder
    {
        private static readonly ImmutableDictionary<string, OneOrMany<AdditionalText>> s_emptyPathToAdditionalTextMap =
            ImmutableDictionary<string, OneOrMany<AdditionalText>>.Empty.WithComparers(PathUtilities.Comparer);

        private readonly object _gate = new object();
        private readonly Dictionary<DiagnosticAnalyzer, TimeSpan>? _analyzerExecutionTimeOpt;
        private readonly HashSet<DiagnosticAnalyzer> _completedAnalyzers;
        private readonly Dictionary<DiagnosticAnalyzer, AnalyzerActionCounts> _analyzerActionCounts;
        private readonly ImmutableDictionary<string, OneOrMany<AdditionalText>> _pathToAdditionalTextMap;

        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? _localSemanticDiagnosticsOpt = null;
        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? _localSyntaxDiagnosticsOpt = null;
        private Dictionary<AdditionalText, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? _localAdditionalFileDiagnosticsOpt = null;
        private Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>? _nonLocalDiagnosticsOpt = null;

        internal AnalysisResultBuilder(bool logAnalyzerExecutionTime, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<AdditionalText> additionalFiles)
        {
            _analyzerExecutionTimeOpt = logAnalyzerExecutionTime ? CreateAnalyzerExecutionTimeMap(analyzers) : null;
            _completedAnalyzers = new HashSet<DiagnosticAnalyzer>();
            _analyzerActionCounts = new Dictionary<DiagnosticAnalyzer, AnalyzerActionCounts>(analyzers.Length);
            _pathToAdditionalTextMap = CreatePathToAdditionalTextMap(additionalFiles);
        }

        private static Dictionary<DiagnosticAnalyzer, TimeSpan> CreateAnalyzerExecutionTimeMap(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var map = new Dictionary<DiagnosticAnalyzer, TimeSpan>(analyzers.Length);
            foreach (var analyzer in analyzers)
            {
                map[analyzer] = default;
            }

            return map;
        }

        private static ImmutableDictionary<string, OneOrMany<AdditionalText>> CreatePathToAdditionalTextMap(ImmutableArray<AdditionalText> additionalFiles)
        {
            if (additionalFiles.IsEmpty)
            {
                return s_emptyPathToAdditionalTextMap;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, OneOrMany<AdditionalText>>(PathUtilities.Comparer);
            foreach (var file in additionalFiles)
            {
                // Null file path for additional files is not possible from IDE or command line compiler host.
                // However, it is possible from custom third party analysis hosts.
                // Ensure we handle it gracefully
                var path = file.Path ?? string.Empty;

                // Handle multiple additional files with same path.
                if (builder.TryGetValue(path, out var value))
                {
                    value = value.Add(file);
                }
                else
                {
                    value = new OneOrMany<AdditionalText>(file);
                }

                builder[path] = value;
            }

            return builder.ToImmutable();
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
                ArrayBuilder<DiagnosticAnalyzer>? builder = null;
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
            Debug.Assert(!fullAnalysisResultForAnalyzersInScope || analysisScope.FilterFileOpt == null, "Full analysis result cannot come from partial (tree) analysis.");

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
                        UpdateLocalDiagnostics_NoLock(analyzer, syntaxDiagnostics, fullAnalysisResultForAnalyzersInScope, getSourceTree, ref _localSyntaxDiagnosticsOpt);
                        UpdateLocalDiagnostics_NoLock(analyzer, syntaxDiagnostics, fullAnalysisResultForAnalyzersInScope, getAdditionalTextKey, ref _localAdditionalFileDiagnosticsOpt);
                        UpdateLocalDiagnostics_NoLock(analyzer, semanticDiagnostics, fullAnalysisResultForAnalyzersInScope, getSourceTree, ref _localSemanticDiagnosticsOpt);
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

            static SyntaxTree? getSourceTree(Diagnostic diagnostic)
                => diagnostic.Location.SourceTree;

            AdditionalText? getAdditionalTextKey(Diagnostic diagnostic)
            {
                // Fetch the first additional file that matches diagnostic location.
                if (diagnostic.Location is ExternalFileLocation externalFileLocation)
                {
                    if (_pathToAdditionalTextMap.TryGetValue(externalFileLocation.FilePath, out var additionalTexts))
                    {
                        foreach (var additionalText in additionalTexts)
                        {
                            if (analysisScope.AdditionalFiles.Contains(additionalText))
                            {
                                return additionalText;
                            }
                        }
                    }
                }

                return null;
            }
        }

        private void UpdateLocalDiagnostics_NoLock<TKey>(
            DiagnosticAnalyzer analyzer,
            ImmutableArray<Diagnostic> diagnostics,
            bool overwrite,
            Func<Diagnostic, TKey?> getKeyFunc,
            ref Dictionary<TKey, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? lazyLocalDiagnostics)
            where TKey : class
        {
            if (diagnostics.IsEmpty)
            {
                return;
            }

            lazyLocalDiagnostics = lazyLocalDiagnostics ?? new Dictionary<TKey, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>();

            foreach (var diagsByKey in diagnostics.GroupBy(getKeyFunc))
            {
                var key = diagsByKey.Key;
                if (key is null)
                {
                    continue;
                }

                Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>? allDiagnostics;
                if (!lazyLocalDiagnostics.TryGetValue(key, out allDiagnostics))
                {
                    allDiagnostics = new Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>();
                    lazyLocalDiagnostics[key] = allDiagnostics;
                }

                ImmutableArray<Diagnostic>.Builder? analyzerDiagnostics;
                if (!allDiagnostics.TryGetValue(analyzer, out analyzerDiagnostics))
                {
                    analyzerDiagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
                    allDiagnostics[analyzer] = analyzerDiagnostics;
                }

                if (overwrite)
                {
                    analyzerDiagnostics.Clear();
                }

                analyzerDiagnostics.AddRange(diagsByKey);
            }
        }

        private void UpdateNonLocalDiagnostics_NoLock(DiagnosticAnalyzer analyzer, ImmutableArray<Diagnostic> diagnostics, bool overwrite)
        {
            if (diagnostics.IsEmpty)
            {
                return;
            }

            _nonLocalDiagnosticsOpt = _nonLocalDiagnosticsOpt ?? new Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>();

            ImmutableArray<Diagnostic>.Builder? currentDiagnostics;
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
                if (!analysisScope.IsSingleFileAnalysis)
                {
                    AddAllLocalDiagnostics_NoLock(_localSyntaxDiagnosticsOpt, analysisScope, builder);
                    AddAllLocalDiagnostics_NoLock(_localSemanticDiagnosticsOpt, analysisScope, builder);
                    AddAllLocalDiagnostics_NoLock(_localAdditionalFileDiagnosticsOpt, analysisScope, builder);
                }
                else if (analysisScope.IsSyntacticSingleFileAnalysis)
                {
                    AddLocalDiagnosticsForPartialAnalysis_NoLock(_localSyntaxDiagnosticsOpt, analysisScope, builder);
                    AddLocalDiagnosticsForPartialAnalysis_NoLock(_localAdditionalFileDiagnosticsOpt, analysisScope, builder);
                }
                else
                {
                    AddLocalDiagnosticsForPartialAnalysis_NoLock(_localSemanticDiagnosticsOpt, analysisScope, builder);
                }
            }

            if (getNonLocalDiagnostics && _nonLocalDiagnosticsOpt != null)
            {
                AddDiagnostics_NoLock(_nonLocalDiagnosticsOpt, analysisScope.Analyzers, builder);
            }

            return builder.ToImmutableArray();
        }

        private static void AddAllLocalDiagnostics_NoLock<TKey>(
            Dictionary<TKey, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? lazyLocalDiagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
            where TKey : class
        {
            if (lazyLocalDiagnostics != null)
            {
                foreach (var localDiagsByTree in lazyLocalDiagnostics.Values)
                {
                    AddDiagnostics_NoLock(localDiagsByTree, analysisScope.Analyzers, builder);
                }
            }
        }

        private static void AddLocalDiagnosticsForPartialAnalysis_NoLock(
            Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? localDiagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
            => AddLocalDiagnosticsForPartialAnalysis_NoLock(localDiagnostics, analysisScope.FilterFileOpt!.Value.SourceTree, analysisScope.Analyzers, builder);

        private static void AddLocalDiagnosticsForPartialAnalysis_NoLock(
            Dictionary<AdditionalText, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? localDiagnostics,
            AnalysisScope analysisScope,
            ImmutableArray<Diagnostic>.Builder builder)
            => AddLocalDiagnosticsForPartialAnalysis_NoLock(localDiagnostics, analysisScope.FilterFileOpt!.Value.AdditionalFile, analysisScope.Analyzers, builder);

        private static void AddLocalDiagnosticsForPartialAnalysis_NoLock<TKey>(
            Dictionary<TKey, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? localDiagnostics,
            TKey? key,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<Diagnostic>.Builder builder)
            where TKey : class
        {
            Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>? diagnosticsForTree;
            if (key != null && localDiagnostics != null && localDiagnostics.TryGetValue(key, out diagnosticsForTree))
            {
                AddDiagnostics_NoLock(diagnosticsForTree, analyzers, builder);
            }
        }

        private static void AddDiagnostics_NoLock(
            Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder> diagnostics,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<Diagnostic>.Builder builder)
        {
            Debug.Assert(diagnostics != null);

            foreach (var analyzer in analyzers)
            {
                ImmutableArray<Diagnostic>.Builder? diagnosticsByAnalyzer;
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
            ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localAdditionalFileDiagnostics;
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> nonLocalDiagnostics;

            var analyzersSet = analyzers.ToImmutableHashSet();
            lock (_gate)
            {
                localSyntaxDiagnostics = GetImmutable(analyzersSet, _localSyntaxDiagnosticsOpt);
                localSemanticDiagnostics = GetImmutable(analyzersSet, _localSemanticDiagnosticsOpt);
                localAdditionalFileDiagnostics = GetImmutable(analyzersSet, _localAdditionalFileDiagnosticsOpt);
                nonLocalDiagnostics = GetImmutable(analyzersSet, _nonLocalDiagnosticsOpt);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var analyzerTelemetryInfo = GetTelemetryInfo(analyzers);
            return new AnalysisResult(analyzers, localSyntaxDiagnostics, localSemanticDiagnostics, localAdditionalFileDiagnostics, nonLocalDiagnostics, analyzerTelemetryInfo);
        }

        private static ImmutableDictionary<TKey, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> GetImmutable<TKey>(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            Dictionary<TKey, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? localDiagnosticsOpt)
            where TKey : class
        {
            if (localDiagnosticsOpt == null)
            {
                return ImmutableDictionary<TKey, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>.Empty;
            }

            var builder = ImmutableDictionary.CreateBuilder<TKey, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>>();
            var perTreeBuilder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>();

            foreach (var diagnosticsByTree in localDiagnosticsOpt)
            {
                var key = diagnosticsByTree.Key;
                foreach (var diagnosticsByAnalyzer in diagnosticsByTree.Value)
                {
                    if (analyzers.Contains(diagnosticsByAnalyzer.Key))
                    {
                        perTreeBuilder.Add(diagnosticsByAnalyzer.Key, diagnosticsByAnalyzer.Value.ToImmutable());
                    }
                }

                builder.Add(key, perTreeBuilder.ToImmutable());
                perTreeBuilder.Clear();
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> GetImmutable(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>? nonLocalDiagnosticsOpt)
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
            ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerTelemetryInfo>();

            lock (_gate)
            {
                foreach (var analyzer in analyzers)
                {
                    if (!_analyzerActionCounts.TryGetValue(analyzer, out var actionCounts))
                    {
                        actionCounts = AnalyzerActionCounts.Empty;
                    }

                    var suppressionActionCounts = analyzer is DiagnosticSuppressor ? 1 : 0;
                    var executionTime = _analyzerExecutionTimeOpt != null ? _analyzerExecutionTimeOpt[analyzer] : default;
                    var telemetryInfo = new AnalyzerTelemetryInfo(actionCounts, suppressionActionCounts, executionTime);
                    builder.Add(analyzer, telemetryInfo);
                }
            }

            return builder.ToImmutable();
        }
    }
}
