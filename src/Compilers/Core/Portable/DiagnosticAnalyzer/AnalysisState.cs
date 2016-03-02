// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.AnalyzerDriver;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Stores the partial analysis state for analyzers executed on a specific compilation.
    /// </summary>
    internal partial class AnalysisState
    {
        private readonly object _gate;

        /// <summary>
        /// Per-analyzer analysis state map.
        /// The integer value points to the index within the <see cref="_analyzerStates"/> array.
        /// </summary>
        private readonly ImmutableDictionary<DiagnosticAnalyzer, int> _analyzerStateMap;

        /// <summary>
        /// Per-analyzer analysis state.
        /// </summary>
        private readonly ImmutableArray<PerAnalyzerState> _analyzerStates;

        /// <summary>
        /// Compilation events corresponding to source tree, that are not completely processed for all analyzers.
        /// Events are dropped as and when they are fully processed by all analyzers.
        /// </summary>
        private readonly Dictionary<SyntaxTree, HashSet<CompilationEvent>> _pendingSourceEvents;

        /// <summary>
        /// Compilation events corresponding to the compilation (compilation start and completed events), that are not completely processed for all analyzers.
        /// </summary>
        private readonly HashSet<CompilationEvent> _pendingNonSourceEvents;

        /// <summary>
        /// Action counts per-analyzer.
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, AnalyzerActionCounts> _lazyAnalyzerActionCountsMap;

        private readonly HashSet<SyntaxTree> _treesWithGeneratedSourceEvents;
        private readonly HashSet<ISymbol> _partialSymbolsWithGeneratedSourceEvents;
        private readonly CompilationData _compilationData;
        private bool _compilationStartGenerated;
        private bool _compilationEndGenerated;

        /// <summary>
        /// Cached semantic model for the compilation trees.
        /// PERF: This cache enables us to re-use semantic model's bound node cache across analyzer execution and diagnostic queries.
        /// </summary>
        private readonly ConditionalWeakTable<SyntaxTree, SemanticModel> _semanticModelsMap;

        private readonly ObjectPool<HashSet<CompilationEvent>> _compilationEventsPool;
        private readonly HashSet<CompilationEvent> _pooledEventsWithAnyActionsSet;

        // Create static pools for heavily allocated per-analyzer state objects - this helps in reducing allocations across CompilationWithAnalyzer instances.
        private const int PoolSize = 5000;
        private static readonly ObjectPool<AnalyzerStateData> s_analyzerStateDataPool = new ObjectPool<AnalyzerStateData>(() => new AnalyzerStateData(), PoolSize);
        private static readonly ObjectPool<DeclarationAnalyzerStateData> s_declarationAnalyzerStateDataPool = new ObjectPool<DeclarationAnalyzerStateData>(() => new DeclarationAnalyzerStateData(), PoolSize);
        private static readonly ObjectPool<Dictionary<int, DeclarationAnalyzerStateData>> s_currentlyAnalyzingDeclarationsMapPool = new ObjectPool<Dictionary<int, DeclarationAnalyzerStateData>>(() => new Dictionary<int, DeclarationAnalyzerStateData>(), PoolSize);
        private static readonly ObjectPool<PerAnalyzerState> s_perAnalyzerStatePool = new ObjectPool<PerAnalyzerState>(() => new PerAnalyzerState(s_analyzerStateDataPool, s_declarationAnalyzerStateDataPool, s_currentlyAnalyzingDeclarationsMapPool), PoolSize);

        public AnalysisState(ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationData compilationData)
        {
            _gate = new object();
            _analyzerStateMap = CreateAnalyzerStateMap(analyzers, out _analyzerStates);
            _compilationData = compilationData;
            _pendingSourceEvents = new Dictionary<SyntaxTree, HashSet<CompilationEvent>>();
            _pendingNonSourceEvents = new HashSet<CompilationEvent>();
            _lazyAnalyzerActionCountsMap = null;
            _semanticModelsMap = new ConditionalWeakTable<SyntaxTree, SemanticModel>();
            _treesWithGeneratedSourceEvents = new HashSet<SyntaxTree>();
            _partialSymbolsWithGeneratedSourceEvents = new HashSet<ISymbol>();
            _compilationStartGenerated = false;
            _compilationEndGenerated = false;
            _compilationEventsPool = new ObjectPool<HashSet<CompilationEvent>>(() => new HashSet<CompilationEvent>());
            _pooledEventsWithAnyActionsSet = new HashSet<CompilationEvent>();
        }

        ~AnalysisState()
        {
            // Free the per-analyzer state tracking objects.
            foreach (var analyzerState in _analyzerStates)
            {
                analyzerState.Free();
                s_perAnalyzerStatePool.Free(analyzerState);
            }
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, int> CreateAnalyzerStateMap(ImmutableArray<DiagnosticAnalyzer> analyzers, out ImmutableArray<PerAnalyzerState> analyzerStates)
        {
            var statesBuilder = ImmutableArray.CreateBuilder<PerAnalyzerState>();
            var map = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, int>();
            var index = 0;
            foreach (var analyzer in analyzers)
            {
                statesBuilder.Add(s_perAnalyzerStatePool.Allocate());
                map[analyzer] = index;
                index++;
            }

            analyzerStates = statesBuilder.ToImmutable();
            return map.ToImmutable();
        }

        private PerAnalyzerState GetAnalyzerState(DiagnosticAnalyzer analyzer)
        {
            var index = _analyzerStateMap[analyzer];
            return _analyzerStates[index];
        }

        public async Task GenerateSimulatedCompilationEventsAsync(
            AnalysisScope analysisScope,
            Compilation compilation,
            Func<SyntaxTree, Compilation, CancellationToken, SemanticModel> getCachedSemanticModel,
            AnalyzerDriver driver,
            CancellationToken cancellationToken)
        {
            await EnsureAnalyzerActionCountsInitializedAsync(driver, cancellationToken).ConfigureAwait(false);

            // Compilation started event.
            GenerateSimulatedCompilatioNonSourceEvent(compilation, driver, started: true, cancellationToken: cancellationToken);

            // Symbol declared and compilation unit completed events.
            foreach (var tree in analysisScope.SyntaxTrees)
            {
                GenerateSimulatedCompilationSourceEvents(tree, compilation, getCachedSemanticModel, driver, cancellationToken);
            }

            // Compilation ended event.
            if (analysisScope.FilterTreeOpt == null)
            {
                GenerateSimulatedCompilatioNonSourceEvent(compilation, driver, started: false, cancellationToken: cancellationToken);
            }
        }

        private void GenerateSimulatedCompilationSourceEvents(
            SyntaxTree tree,
            Compilation compilation,
            Func<SyntaxTree, Compilation, CancellationToken, SemanticModel> getCachedSemanticModel,
            AnalyzerDriver driver,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_treesWithGeneratedSourceEvents.Contains(tree))
                {
                    return;
                }
            }

            var globalNs = compilation.Assembly.GlobalNamespace;
            var symbols = GetDeclaredSymbolsInTree(tree, compilation, getCachedSemanticModel, cancellationToken);
            var compilationEvents = CreateCompilationEventsForTree(symbols.Concat(globalNs), tree, compilation);

            lock (_gate)
            {
                if (_treesWithGeneratedSourceEvents.Contains(tree))
                {
                    return;
                }

                OnCompilationEventsGenerated_NoLock(compilationEvents, tree, driver, cancellationToken);

                var added = _treesWithGeneratedSourceEvents.Add(tree);
                Debug.Assert(added);
            }
        }

        private IEnumerable<ISymbol> GetDeclaredSymbolsInTree(
            SyntaxTree tree,
            Compilation compilation,
            Func<SyntaxTree, Compilation, CancellationToken, SemanticModel> getCachedSemanticModel,
            CancellationToken cancellationToken)
        {
            var model = getCachedSemanticModel(tree, compilation, cancellationToken);
            var fullSpan = tree.GetRoot(cancellationToken).FullSpan;
            var declarationInfos = new List<DeclarationInfo>();
            model.ComputeDeclarationsInSpan(fullSpan, getSymbol: true, builder: declarationInfos, cancellationToken: cancellationToken);
            return declarationInfos.Select(declInfo => declInfo.DeclaredSymbol).WhereNotNull();
        }

        private static ImmutableArray<CompilationEvent> CreateCompilationEventsForTree(IEnumerable<ISymbol> declaredSymbols, SyntaxTree tree, Compilation compilation)
        {
            var builder = ImmutableArray.CreateBuilder<CompilationEvent>();
            foreach (var symbol in declaredSymbols)
            {
                Debug.Assert(symbol.ContainingAssembly == compilation.Assembly);
                builder.Add(new SymbolDeclaredCompilationEvent(compilation, symbol));
            }

            builder.Add(new CompilationUnitCompletedEvent(compilation, tree));
            return builder.ToImmutable();
        }

        private void GenerateSimulatedCompilatioNonSourceEvent(Compilation compilation, AnalyzerDriver driver, bool started, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var eventAlreadyGenerated = started ? _compilationStartGenerated : _compilationEndGenerated;
                if (eventAlreadyGenerated)
                {
                    return;
                }

                var compilationEvent = started ? (CompilationEvent)new CompilationStartedEvent(compilation) : new CompilationCompletedEvent(compilation);
                var events = ImmutableArray.Create(compilationEvent);
                OnCompilationEventsGenerated_NoLock(events, filterTreeOpt: null, driver: driver, cancellationToken: cancellationToken);

                if (started)
                {
                    _compilationStartGenerated = true;
                }
                else
                {
                    _compilationEndGenerated = true;
                }
            }
        }

        public async Task OnCompilationEventsGeneratedAsync(ImmutableArray<CompilationEvent> compilationEvents, AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            await EnsureAnalyzerActionCountsInitializedAsync(driver, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                OnCompilationEventsGenerated_NoLock(compilationEvents, filterTreeOpt: null, driver: driver, cancellationToken: cancellationToken);
            }
        }

        private void OnCompilationEventsGenerated_NoLock(ImmutableArray<CompilationEvent> compilationEvents, SyntaxTree filterTreeOpt, AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            Debug.Assert(_lazyAnalyzerActionCountsMap != null);

            // Add the events to our global pending events map.
            AddToEventsMap_NoLock(compilationEvents, filterTreeOpt);

            // Mark the events for analysis for each analyzer.
            ArrayBuilder<ISymbol> newPartialSymbols = null;
            Debug.Assert(_pooledEventsWithAnyActionsSet.Count == 0);
            foreach (var kvp in _analyzerStateMap)
            {
                var analyzer = kvp.Key;
                var analyzerState = _analyzerStates[kvp.Value];
                var actionCounts = _lazyAnalyzerActionCountsMap[analyzer];

                foreach (var compilationEvent in compilationEvents)
                {
                    if (HasActionsForEvent(compilationEvent, actionCounts))
                    {
                        _pooledEventsWithAnyActionsSet.Add(compilationEvent);

                        var symbolDeclaredEvent = compilationEvent as SymbolDeclaredCompilationEvent;
                        if (symbolDeclaredEvent?.DeclaringSyntaxReferences.Length > 1)
                        {
                            if (_partialSymbolsWithGeneratedSourceEvents.Contains(symbolDeclaredEvent.Symbol))
                            {
                                // already processed.
                                continue;
                            }
                            else
                            {
                                newPartialSymbols = newPartialSymbols ?? ArrayBuilder<ISymbol>.GetInstance();
                                newPartialSymbols.Add(symbolDeclaredEvent.Symbol);
                            }
                        }

                        analyzerState.OnCompilationEventGenerated(compilationEvent, actionCounts);
                    }
                }
            }

            foreach (var compilationEvent in compilationEvents)
            {
                if (!_pooledEventsWithAnyActionsSet.Remove(compilationEvent))
                {
                    // Event has no relevant actions to execute, so mark it as complete.  
                    UpdateEventsMap_NoLock(compilationEvent, add: false);
                }
            }

            if (newPartialSymbols != null)
            {
                _partialSymbolsWithGeneratedSourceEvents.AddAll(newPartialSymbols);
                newPartialSymbols.Free();
            }
        }

        private void AddToEventsMap_NoLock(ImmutableArray<CompilationEvent> compilationEvents, SyntaxTree filterTreeOpt)
        {
            if (filterTreeOpt != null)
            {
                AddPendingSourceEvents_NoLock(compilationEvents, filterTreeOpt);
                return;
            }

            foreach (var compilationEvent in compilationEvents)
            {
                UpdateEventsMap_NoLock(compilationEvent, add: true);
            }
        }

        private void UpdateEventsMap_NoLock(CompilationEvent compilationEvent, bool add)
        {
            var symbolEvent = compilationEvent as SymbolDeclaredCompilationEvent;
            if (symbolEvent != null)
            {
                // Add/remove symbol events.
                // Any diagnostics request for a tree should trigger symbol and syntax node analysis for symbols with at least one declaring reference in the tree.
                foreach (var location in symbolEvent.Symbol.Locations)
                {
                    if (location.SourceTree != null)
                    {
                        if (add)
                        {
                            AddPendingSourceEvent_NoLock(location.SourceTree, compilationEvent);
                        }
                        else
                        {
                            RemovePendingSourceEvent_NoLock(location.SourceTree, compilationEvent);
                        }
                    }
                }
            }
            else
            {
                // Add/remove compilation unit completed events.
                var compilationUnitCompletedEvent = compilationEvent as CompilationUnitCompletedEvent;
                if (compilationUnitCompletedEvent != null)
                {
                    var tree = compilationUnitCompletedEvent.SemanticModel.SyntaxTree;
                    if (add)
                    {
                        AddPendingSourceEvent_NoLock(tree, compilationEvent);
                    }
                    else
                    {
                        RemovePendingSourceEvent_NoLock(tree, compilationEvent);
                    }
                }
                else if (compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent)
                {
                    // Add/remove compilation events.
                    if (add)
                    {
                        _pendingNonSourceEvents.Add(compilationEvent);
                    }
                    else
                    {
                        _pendingNonSourceEvents.Remove(compilationEvent);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unexpected compilation event of type " + compilationEvent.GetType().Name);
                }
            }
        }

        private void AddPendingSourceEvents_NoLock(ImmutableArray<CompilationEvent> compilationEvents, SyntaxTree tree)
        {
            HashSet<CompilationEvent> currentEvents;
            if (!_pendingSourceEvents.TryGetValue(tree, out currentEvents))
            {
                currentEvents = new HashSet<CompilationEvent>(compilationEvents);
                _pendingSourceEvents[tree] = currentEvents;
                _compilationData.RemoveCachedSemanticModel(tree);
                return;
            }

            currentEvents.AddAll(compilationEvents);
        }

        private void AddPendingSourceEvent_NoLock(SyntaxTree tree, CompilationEvent compilationEvent)
        {
            HashSet<CompilationEvent> currentEvents;
            if (!_pendingSourceEvents.TryGetValue(tree, out currentEvents))
            {
                currentEvents = new HashSet<CompilationEvent>();
                _pendingSourceEvents[tree] = currentEvents;
                _compilationData.RemoveCachedSemanticModel(tree);
            }

            currentEvents.Add(compilationEvent);
        }

        private void RemovePendingSourceEvent_NoLock(SyntaxTree tree, CompilationEvent compilationEvent)
        {
            HashSet<CompilationEvent> currentEvents;
            if (_pendingSourceEvents.TryGetValue(tree, out currentEvents))
            {
                if (currentEvents.Remove(compilationEvent) && currentEvents.Count == 0)
                {
                    _pendingSourceEvents.Remove(tree);
                    _compilationData.RemoveCachedSemanticModel(tree);
                }
            }
        }

        private async Task EnsureAnalyzerActionCountsInitializedAsync(AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            if (_lazyAnalyzerActionCountsMap == null)
            {
                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerActionCounts>();
                foreach (var analyzer in _analyzerStateMap.Keys)
                {
                    var actionCounts = await driver.GetAnalyzerActionCountsAsync(analyzer, cancellationToken).ConfigureAwait(false);
                    builder.Add(analyzer, actionCounts);
                }

                Interlocked.CompareExchange(ref _lazyAnalyzerActionCountsMap, builder.ToImmutable(), null);
            }
        }

        internal async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            await EnsureAnalyzerActionCountsInitializedAsync(driver, cancellationToken).ConfigureAwait(false);
            return _lazyAnalyzerActionCountsMap[analyzer];
        }

        private static bool HasActionsForEvent(CompilationEvent compilationEvent, AnalyzerActionCounts actionCounts)
        {
            if (compilationEvent is CompilationStartedEvent)
            {
                return actionCounts.CompilationActionsCount > 0 ||
                    actionCounts.SyntaxTreeActionsCount > 0;
            }
            else if (compilationEvent is CompilationCompletedEvent)
            {
                return actionCounts.CompilationEndActionsCount > 0;
            }
            else if (compilationEvent is SymbolDeclaredCompilationEvent)
            {
                return actionCounts.SymbolActionsCount > 0 || actionCounts.HasAnyExecutableCodeActions;
            }
            else
            {
                return actionCounts.SemanticModelActionsCount > 0;
            }
        }

        private void OnSymbolDeclaredEventProcessed(SymbolDeclaredCompilationEvent symbolDeclaredEvent, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                var analyzerState = GetAnalyzerState(analyzer);
                analyzerState.OnSymbolDeclaredEventProcessed(symbolDeclaredEvent);
            }
        }

        /// <summary>
        /// Invoke this method at completion of event processing for the given analysis scope.
        /// It updates the analysis state of this event for each analyzer and if the event has been fully processed for all analyzers, then removes it from our event cache.
        /// </summary>
        public void OnCompilationEventProcessed(CompilationEvent compilationEvent, AnalysisScope analysisScope)
        {
            // Analyze if the symbol and all its declaring syntax references are analyzed.
            var symbolDeclaredEvent = compilationEvent as SymbolDeclaredCompilationEvent;
            if (symbolDeclaredEvent != null)
            {
                OnSymbolDeclaredEventProcessed(symbolDeclaredEvent, analysisScope.Analyzers);
            }

            // Check if event is fully analyzed for all analyzers.
            foreach (var analyzerState in _analyzerStates)
            {
                if (!analyzerState.IsEventAnalyzed(compilationEvent))
                {
                    return;
                }
            }

            // Remove the event from event map.
            lock (_gate)
            {
                UpdateEventsMap_NoLock(compilationEvent, add: false);
            }
        }

        /// <summary>
        /// Gets pending events for given set of analyzers for the given syntax tree.
        /// </summary>
        public ImmutableArray<CompilationEvent> GetPendingEvents(ImmutableArray<DiagnosticAnalyzer> analyzers, SyntaxTree tree)
        {
            lock (_gate)
            {
                return GetPendingEvents_NoLock(analyzers, tree);
            }
        }

        private HashSet<CompilationEvent> GetPendingEvents_NoLock(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var uniqueEvents = _compilationEventsPool.Allocate();
            foreach (var analyzer in analyzers)
            {
                var analyzerState = GetAnalyzerState(analyzer);
                analyzerState.AddPendingEvents(uniqueEvents);
            }

            return uniqueEvents;
        }

        /// <summary>
        /// Gets pending events for given set of analyzers for the given syntax tree.
        /// </summary>
        private ImmutableArray<CompilationEvent> GetPendingEvents_NoLock(ImmutableArray<DiagnosticAnalyzer> analyzers, SyntaxTree tree)
        {
            HashSet<CompilationEvent> compilationEventsForTree;
            if (_pendingSourceEvents.TryGetValue(tree, out compilationEventsForTree))
            {
                if (compilationEventsForTree?.Count > 0)
                {
                    HashSet<CompilationEvent> pendingEvents = null;
                    try
                    {
                        pendingEvents = GetPendingEvents_NoLock(analyzers);
                        if (pendingEvents.Count > 0)
                        {
                            pendingEvents.IntersectWith(compilationEventsForTree);
                            return pendingEvents.ToImmutableArray();
                        }
                    }
                    finally
                    {
                        Free(pendingEvents);
                    }
                }
            }

            return ImmutableArray<CompilationEvent>.Empty;
        }

        /// <summary>
        /// Gets all pending events for given set of analyzers.
        /// </summary>
        /// <param name="analyzers"></param>
        /// <param name="includeSourceEvents">Indicates if source events (symbol declared, compilation unit completed event) should be included.</param>
        /// <param name="includeNonSourceEvents">Indicates if compilation wide events (compilation started and completed event) should be included.</param>
        public ImmutableArray<CompilationEvent> GetPendingEvents(ImmutableArray<DiagnosticAnalyzer> analyzers, bool includeSourceEvents, bool includeNonSourceEvents)
        {
            lock (_gate)
            {
                return GetPendingEvents_NoLock(analyzers, includeSourceEvents, includeNonSourceEvents);
            }
        }

        private ImmutableArray<CompilationEvent> GetPendingEvents_NoLock(ImmutableArray<DiagnosticAnalyzer> analyzers, bool includeSourceEvents, bool includeNonSourceEvents)
        {
            HashSet<CompilationEvent> pendingEvents = null, uniqueEvents = null;
            try
            {
                pendingEvents = GetPendingEvents_NoLock(analyzers);
                if (pendingEvents.Count == 0)
                {
                    return ImmutableArray<CompilationEvent>.Empty;
                }

                uniqueEvents = _compilationEventsPool.Allocate();

                if (includeSourceEvents)
                {
                    foreach (var compilationEvents in _pendingSourceEvents.Values)
                    {
                        foreach (var compilationEvent in compilationEvents)
                        {
                            uniqueEvents.Add(compilationEvent);
                        }
                    }
                }

                if (includeNonSourceEvents)
                {
                    foreach (var compilationEvent in _pendingNonSourceEvents)
                    {
                        uniqueEvents.Add(compilationEvent);
                    }
                }

                uniqueEvents.IntersectWith(pendingEvents);
                return uniqueEvents.ToImmutableArray();
            }
            finally
            {
                Free(pendingEvents);
                Free(uniqueEvents);
            }
        }

        private void Free(HashSet<CompilationEvent> events)
        {
            if (events != null)
            {
                events.Clear();
                _compilationEventsPool.Free(events);
            }
        }

        /// <summary>
        /// Returns true if we have any pending syntax analysis for given analysis scope.
        /// </summary>
        public bool HasPendingSyntaxAnalysis(AnalysisScope analysisScope)
        {
            if (analysisScope.IsTreeAnalysis && !analysisScope.IsSyntaxOnlyTreeAnalysis)
            {
                return false;
            }

            foreach (var analyzer in analysisScope.Analyzers)
            {
                var analyzerState = GetAnalyzerState(analyzer);
                if (analyzerState.HasPendingSyntaxAnalysis(analysisScope.FilterTreeOpt))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if we have any pending symbol analysis for given analysis scope.
        /// </summary>
        public bool HasPendingSymbolAnalysis(AnalysisScope analysisScope)
        {
            Debug.Assert(analysisScope.FilterTreeOpt != null);

            var symbolDeclaredEvents = GetPendingSymbolDeclaredEvents(analysisScope.FilterTreeOpt);
            foreach (var symbolDeclaredEvent in symbolDeclaredEvents)
            {
                if (analysisScope.ShouldAnalyze(symbolDeclaredEvent.Symbol))
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        var analyzerState = GetAnalyzerState(analyzer);
                        if (analyzerState.HasPendingSymbolAnalysis(symbolDeclaredEvent.Symbol))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private ImmutableArray<SymbolDeclaredCompilationEvent> GetPendingSymbolDeclaredEvents(SyntaxTree tree)
        {
            Debug.Assert(tree != null);

            lock (_gate)
            {
                HashSet<CompilationEvent> compilationEvents;
                if (!_pendingSourceEvents.TryGetValue(tree, out compilationEvents))
                {
                    return ImmutableArray<SymbolDeclaredCompilationEvent>.Empty;
                }

                return compilationEvents.OfType<SymbolDeclaredCompilationEvent>().ToImmutableArray();
            }
        }

        /// <summary>
        /// Attempts to start processing a compilation event for the given analyzer.
        /// </summary>
        /// <returns>
        /// Returns false if the event has already been processed for the analyzer OR is currently being processed by another task.
        /// If true, then it returns a non-null <paramref name="state"/> representing partial analysis state for the given event for the given analyzer.
        /// </returns>
        public bool TryStartProcessingEvent(CompilationEvent compilationEvent, DiagnosticAnalyzer analyzer, out AnalyzerStateData state)
        {
            return GetAnalyzerState(analyzer).TryStartProcessingEvent(compilationEvent, out state);
        }

        /// <summary>
        /// Marks the given event as fully analyzed for the given analyzer.
        /// </summary>
        public void MarkEventComplete(CompilationEvent compilationEvent, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerState(analyzer).MarkEventComplete(compilationEvent);
        }

        /// <summary>
        /// Marks the given event as fully analyzed for the given analyzers.
        /// </summary>
        public void MarkEventComplete(CompilationEvent compilationEvent, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                GetAnalyzerState(analyzer).MarkEventComplete(compilationEvent);
            }
        }

        /// <summary>
        /// Attempts to start processing a symbol for the given analyzer's symbol actions.
        /// </summary>
        /// <returns>
        /// Returns false if the symbol has already been processed for the analyzer OR is currently being processed by another task.
        /// If true, then it returns a non-null <paramref name="state"/> representing partial analysis state for the given symbol for the given analyzer.
        /// </returns>
        public bool TryStartAnalyzingSymbol(ISymbol symbol, DiagnosticAnalyzer analyzer, out AnalyzerStateData state)
        {
            return GetAnalyzerState(analyzer).TryStartAnalyzingSymbol(symbol, out state);
        }

        /// <summary>
        /// Marks the given symbol as fully analyzed for the given analyzer.
        /// </summary>
        public void MarkSymbolComplete(ISymbol symbol, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerState(analyzer).MarkSymbolComplete(symbol);
        }

        /// <summary>
        /// Attempts to start processing a symbol declaration for the given analyzer's syntax node and code block actions.
        /// </summary>
        /// <returns>
        /// Returns false if the declaration has already been processed for the analyzer OR is currently being processed by another task.
        /// If true, then it returns a non-null <paramref name="state"/> representing partial analysis state for the given declaration for the given analyzer.
        /// </returns>
        public bool TryStartAnalyzingDeclaration(ISymbol symbol, int declarationIndex, DiagnosticAnalyzer analyzer, out DeclarationAnalyzerStateData state)
        {
            return GetAnalyzerState(analyzer).TryStartAnalyzingDeclaration(symbol, declarationIndex, out state);
        }

        /// <summary>
        /// True if the given symbol declaration is fully analyzed.
        /// </summary>
        public bool IsDeclarationComplete(ISymbol symbol, int declarationIndex)
        {
            foreach (var analyzerState in _analyzerStates)
            {
                if (!analyzerState.IsDeclarationComplete(symbol, declarationIndex))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Marks the given symbol declaration as fully analyzed for the given analyzer.
        /// </summary>
        public void MarkDeclarationComplete(ISymbol symbol, int declarationIndex, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerState(analyzer).MarkDeclarationComplete(symbol, declarationIndex);
        }

        /// <summary>
        /// Marks the given symbol declaration as fully analyzed for the given analyzers.
        /// </summary>
        public void MarkDeclarationComplete(ISymbol symbol, int declarationIndex, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                GetAnalyzerState(analyzer).MarkDeclarationComplete(symbol, declarationIndex);
            }
        }

        /// <summary>
        /// Marks all the symbol declarations for the given symbol as fully analyzed for all the given analyzers.
        /// </summary>
        public void MarkDeclarationsComplete(ISymbol symbol, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                GetAnalyzerState(analyzer).MarkDeclarationsComplete(symbol);
            }
        }

        /// <summary>
        /// Attempts to start processing a syntax tree for the given analyzer's syntax tree actions.
        /// </summary>
        /// <returns>
        /// Returns false if the tree has already been processed for the analyzer OR is currently being processed by another task.
        /// If true, then it returns a non-null <paramref name="state"/> representing partial syntax analysis state for the given tree for the given analyzer.
        /// </returns>
        public bool TryStartSyntaxAnalysis(SyntaxTree tree, DiagnosticAnalyzer analyzer, out AnalyzerStateData state)
        {
            return GetAnalyzerState(analyzer).TryStartSyntaxAnalysis(tree, out state);
        }

        /// <summary>
        /// Marks the given tree as fully syntactically analyzed for the given analyzer.
        /// </summary>
        public void MarkSyntaxAnalysisComplete(SyntaxTree tree, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerState(analyzer).MarkSyntaxAnalysisComplete(tree);
        }

        /// <summary>
        /// Marks the given tree as fully syntactically analyzed for the given analyzers.
        /// </summary>
        public void MarkSyntaxAnalysisComplete(SyntaxTree tree, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                GetAnalyzerState(analyzer).MarkSyntaxAnalysisComplete(tree);
            }
        }
    }
}
