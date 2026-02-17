// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
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
        private readonly HashSet<DiagnosticAnalyzer> _completedAnalyzersForCompilation;
        private readonly Dictionary<SyntaxTree, HashSet<DiagnosticAnalyzer>> _completedSyntaxAnalyzersByTree;
        private readonly Dictionary<SyntaxTree, HashSet<DiagnosticAnalyzer>> _completedSemanticAnalyzersByTree;
        private readonly Dictionary<AdditionalText, HashSet<DiagnosticAnalyzer>> _completedSyntaxAnalyzersByAdditionalFile;
        private readonly Dictionary<DiagnosticAnalyzer, AnalyzerActionCounts> _analyzerActionCounts;
        private readonly ImmutableDictionary<string, OneOrMany<AdditionalText>> _pathToAdditionalTextMap;

        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? _localSemanticDiagnosticsOpt = null;
        private Dictionary<SyntaxTree, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? _localSyntaxDiagnosticsOpt = null;
        private Dictionary<AdditionalText, Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>>? _localAdditionalFileDiagnosticsOpt = null;
        private Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>.Builder>? _nonLocalDiagnosticsOpt = null;

        internal AnalysisResultBuilder(bool logAnalyzerExecutionTime, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableArray<AdditionalText> additionalFiles)
        {
            _analyzerExecutionTimeOpt = logAnalyzerExecutionTime ? CreateAnalyzerExecutionTimeMap(analyzers) : null;
            _completedAnalyzersForCompilation = new HashSet<DiagnosticAnalyzer>();
            _completedSyntaxAnalyzersByTree = new Dictionary<SyntaxTree, HashSet<DiagnosticAnalyzer>>();
            _completedSemanticAnalyzersByTree = new Dictionary<SyntaxTree, HashSet<DiagnosticAnalyzer>>();
            _completedSyntaxAnalyzersByAdditionalFile = new Dictionary<AdditionalText, HashSet<DiagnosticAnalyzer>>();
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

        private HashSet<DiagnosticAnalyzer>? GetCompletedAnalyzersForFile_NoLock(SourceOrAdditionalFile filterFile, bool syntax)
        {
            if (filterFile.SourceTree is { } tree)
            {
                var completedAnalyzersByTree = syntax ? _completedSyntaxAnalyzersByTree : _completedSemanticAnalyzersByTree;
                if (completedAnalyzersByTree.TryGetValue(tree, out var completedAnalyzersForTree))
                {
                    return completedAnalyzersForTree;
                }
            }
            else if (filterFile.AdditionalFile is { } additionalFile)
            {
                if (_completedSyntaxAnalyzersByAdditionalFile.TryGetValue(additionalFile, out var completedAnalyzersForFile))
                {
                    return completedAnalyzersForFile;
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }

            return null;
        }

        private void AddCompletedAnalyzerForFile_NoLock(SourceOrAdditionalFile filterFile, bool syntax, DiagnosticAnalyzer analyzer)
        {
            var completedAnalyzers = new HashSet<DiagnosticAnalyzer> { analyzer };
            if (filterFile.SourceTree is { } tree)
            {
                var completedAnalyzersByTree = syntax ? _completedSyntaxAnalyzersByTree : _completedSemanticAnalyzersByTree;
                completedAnalyzersByTree.Add(tree, completedAnalyzers);
            }
            else if (filterFile.AdditionalFile is { } additionalFile)
            {
                _completedSyntaxAnalyzersByAdditionalFile.Add(additionalFile, completedAnalyzers);
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// Filters down the given <paramref name="analyzers"/> to only retain the analyzers which have
        /// not completed execution. If the <paramref name="filterScope"/> is non-null, then return
        /// the analyzers which have not fully exected on the filterScope. Otherwise, return the analyzers
        /// which have not fully executed on the entire compilation.
        /// </summary>
        /// <param name="analyzers">Analyzers to be filtered.</param>
        /// <param name="filterScope">Optional scope for filtering.</param>
        /// <returns>
        /// Analyzers which have not fully executed on the given <paramref name="filterScope"/>, if non-null,
        /// or the entire compilation, if <paramref name="filterScope"/> is null.
        /// </returns>
        public ImmutableArray<DiagnosticAnalyzer> GetPendingAnalyzers(ImmutableArray<DiagnosticAnalyzer> analyzers, (SourceOrAdditionalFile file, bool syntax)? filterScope)
        {
            lock (_gate)
            {
                // If we have a non-null filter scope, then fetch the set of analyzers that have
                // already completed execution on this filter scope.
                var completedAnalyzersForFile = filterScope.HasValue
                    ? GetCompletedAnalyzersForFile_NoLock(filterScope.Value.file, filterScope.Value.syntax)
                    : null;

                return analyzers.WhereAsArray(
                    static (analyzer, arg) =>
                    {
                        // If the analyzer has not executed for the entire compilation, or we are computing
                        // pending analyzers for a specific filterScope and the analyzer has not executed on
                        // this filter scope, then we add the analyzer to pending analyzers.
                        if (!arg.self._completedAnalyzersForCompilation.Contains(analyzer) &&
                            (arg.completedAnalyzersForFile == null || !arg.completedAnalyzersForFile.Contains(analyzer)))
                        {
                            return true;
                        }

                        return false;
                    },
                    (self: this, completedAnalyzersForFile));
            }
        }

        public void ApplySuppressionsAndStoreAnalysisResult(AnalysisScope analysisScope, AnalyzerDriver driver, Compilation compilation, Func<DiagnosticAnalyzer, AnalyzerActionCounts> getAnalyzerActionCounts, CancellationToken cancellationToken)
        {
            foreach (var analyzer in analysisScope.Analyzers)
            {
                // Dequeue reported analyzer diagnostics from the driver and store them in our maps.
                var syntaxDiagnostics = driver.DequeueLocalDiagnosticsAndApplySuppressions(analyzer, syntax: true, compilation: compilation, cancellationToken);
                var semanticDiagnostics = driver.DequeueLocalDiagnosticsAndApplySuppressions(analyzer, syntax: false, compilation: compilation, cancellationToken);
                var compilationDiagnostics = driver.DequeueNonLocalDiagnosticsAndApplySuppressions(analyzer, compilation, cancellationToken);

                lock (_gate)
                {
                    if (_completedAnalyzersForCompilation.Contains(analyzer))
                    {
                        // Already stored full analysis result for this analyzer.
                        continue;
                    }

                    // Determine if we have computed fully syntax/semantic diagnostics
                    // for a specific filter file or the entire compilation for this analyzer.
                    // If we have full diagnostics for the filter file/compilation, we add this analyzer to
                    // the corresponding completed analyzers set to avoid re-executing this analyzer
                    // for future diagnostic requests on this analysis scope.
                    bool fullSyntaxDiagnosticsForTree = false;
                    bool fullSyntaxDiagnosticsForAdditionalFile = false;
                    bool fullSemanticDiagnosticsForTree = false;
                    bool fullCompilationDiagnostics = false;

                    // Check if we computed syntax/semantic diagnostics for a specific filter file only.
                    if (analysisScope.FilterFileOpt.HasValue)
                    {
                        var completedAnalyzersForFile = GetCompletedAnalyzersForFile_NoLock(analysisScope.FilterFileOpt.Value, analysisScope.IsSyntacticSingleFileAnalysis);
                        if (completedAnalyzersForFile?.Contains(analyzer) == true)
                        {
                            // Already stored analysis result for this analyzer for the analyzed file.
                            continue;
                        }
                        else if (!analysisScope.FilterSpanOpt.HasValue && !analysisScope.OriginalFilterSpan.HasValue)
                        {
                            // We have complete analysis result for this file.
                            // Mark this file as completely analyzed for this analyzer.
                            if (completedAnalyzersForFile != null)
                            {
                                completedAnalyzersForFile.Add(analyzer);
                            }
                            else
                            {
                                AddCompletedAnalyzerForFile_NoLock(analysisScope.FilterFileOpt.Value, analysisScope.IsSyntacticSingleFileAnalysis, analyzer);
                            }

                            // Set the appropriate full diagnostics for tree/additional file flag.
                            if (analysisScope.IsSyntacticSingleFileAnalysis)
                            {
                                if (analysisScope.FilterFileOpt.Value.SourceTree != null)
                                {
                                    fullSyntaxDiagnosticsForTree = true;
                                }
                                else
                                {
                                    fullSyntaxDiagnosticsForAdditionalFile = true;
                                }
                            }
                            else
                            {
                                fullSemanticDiagnosticsForTree = true;
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(!analysisScope.FilterSpanOpt.HasValue);

                        _completedAnalyzersForCompilation.Add(analyzer);
                        fullCompilationDiagnostics = true;
                        fullSyntaxDiagnosticsForTree = true;
                        fullSyntaxDiagnosticsForAdditionalFile = true;
                        fullSemanticDiagnosticsForTree = true;
                    }

                    // Finally, update the appropriate syntax/semantic/compilation diagnostic maps to store the
                    // computed diagnostics. If we have full diagnostics for the filter file/compilation,
                    // we overwrite the diagnostics in the map.
                    // Otherwise, we have computed partial diagnostics for a file span, so we just
                    // append to the previously computed and stored diagnostics.

                    if (!syntaxDiagnostics.IsEmpty)
                    {
                        UpdateLocalDiagnostics_NoLock(analyzer, syntaxDiagnostics, fullSyntaxDiagnosticsForTree, getSourceTree, ref _localSyntaxDiagnosticsOpt);
                        UpdateLocalDiagnostics_NoLock(analyzer, syntaxDiagnostics, fullSyntaxDiagnosticsForAdditionalFile, getAdditionalTextKey, ref _localAdditionalFileDiagnosticsOpt);
                    }

                    if (!semanticDiagnostics.IsEmpty)
                    {
                        UpdateLocalDiagnostics_NoLock(analyzer, semanticDiagnostics, fullSemanticDiagnosticsForTree, getSourceTree, ref _localSemanticDiagnosticsOpt);
                    }

                    if (!compilationDiagnostics.IsEmpty)
                    {
                        UpdateNonLocalDiagnostics_NoLock(analyzer, compilationDiagnostics, fullCompilationDiagnostics);
                    }

                    if (_analyzerExecutionTimeOpt != null)
                    {
                        var timeSpan = driver.ResetAnalyzerExecutionTime(analyzer);
                        _analyzerExecutionTimeOpt[analyzer] = fullCompilationDiagnostics ?
                            timeSpan :
                            _analyzerExecutionTimeOpt[analyzer] + timeSpan;
                    }

                    if (!_analyzerActionCounts.ContainsKey(analyzer))
                    {
                        _analyzerActionCounts.Add(analyzer, getAnalyzerActionCounts(analyzer));
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
                    if (_pathToAdditionalTextMap.TryGetValue(externalFileLocation.GetLineSpan().Path, out var additionalTexts))
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

                UpdateDiagnosticsCore_NoLock(analyzerDiagnostics, diagsByKey, overwrite);
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

            UpdateDiagnosticsCore_NoLock(currentDiagnostics, diagnostics, overwrite);
        }

        private static void UpdateDiagnosticsCore_NoLock(ImmutableArray<Diagnostic>.Builder currentDiagnostics, IEnumerable<Diagnostic> diagnostics, bool overwrite)
        {
            if (overwrite)
            {
                currentDiagnostics.Clear();
            }
            else
            {
                // Always de-dupe diagnostic to add
                diagnostics = diagnostics.Where(d => !currentDiagnostics.Contains(d));
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

        internal AnalysisResult ToAnalysisResult(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localSyntaxDiagnostics;
            ImmutableDictionary<SyntaxTree, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localSemanticDiagnostics;
            ImmutableDictionary<AdditionalText, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> localAdditionalFileDiagnostics;
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> nonLocalDiagnostics;

            var analyzersSet = PooledHashSet<DiagnosticAnalyzer>.GetInstance();
            analyzersSet.AddRange(analyzers);

            Func<Diagnostic, bool> shouldInclude = analysisScope.ShouldInclude;
            lock (_gate)
            {
                localSyntaxDiagnostics = GetImmutable(analyzersSet, shouldInclude, _localSyntaxDiagnosticsOpt);
                localSemanticDiagnostics = GetImmutable(analyzersSet, shouldInclude, _localSemanticDiagnosticsOpt);
                localAdditionalFileDiagnostics = GetImmutable(analyzersSet, shouldInclude, _localAdditionalFileDiagnosticsOpt);
                nonLocalDiagnostics = GetImmutable(analyzersSet, shouldInclude, _nonLocalDiagnosticsOpt);
            }

            analyzersSet.Free();
            cancellationToken.ThrowIfCancellationRequested();
            var analyzerTelemetryInfo = GetTelemetryInfo(analyzers);
            return new AnalysisResult(analyzers, localSyntaxDiagnostics, localSemanticDiagnostics, localAdditionalFileDiagnostics, nonLocalDiagnostics, analyzerTelemetryInfo);
        }

        /// <summary>
        /// Gets an immutable dictionary from the given local diagnostics map.
        /// </summary>
        /// <param name="analyzers">Limits the analyzers included. Is not modified.</param>
        /// <param name="shouldInclude">Filter determining whether a diagnostic should be included</param>
        /// <param name="localDiagnosticsOpt">Diagnostic map to operate on</param>
        private static ImmutableDictionary<TKey, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>> GetImmutable<TKey>(
            HashSet<DiagnosticAnalyzer> analyzers, // Will not be modified
            Func<Diagnostic, bool> shouldInclude,
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
                        var diagnostics = diagnosticsByAnalyzer.Value.Where(shouldInclude).ToImmutableArray();
                        if (!diagnostics.IsEmpty)
                        {
                            perTreeBuilder.Add(diagnosticsByAnalyzer.Key, diagnostics);
                        }
                    }
                }

                builder.Add(key, perTreeBuilder.ToImmutable());
                perTreeBuilder.Clear();
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Gets an immutable dictionary from the given non-local diagnostics map.
        /// </summary>
        /// <param name="analyzers">Limits the analyzers included. Is not modified.</param>
        /// <param name="shouldInclude">Filter determining whether a diagnostic should be included</param>
        /// <param name="nonLocalDiagnosticsOpt">Diagnostic map to operate on</param>
        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> GetImmutable(
            HashSet<DiagnosticAnalyzer> analyzers,
            Func<Diagnostic, bool> shouldInclude,
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
                    var diagnostics = diagnosticsByAnalyzer.Value.Where(shouldInclude).ToImmutableArray();
                    if (!diagnostics.IsEmpty)
                    {
                        builder.Add(diagnosticsByAnalyzer.Key, diagnostics);
                    }
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
