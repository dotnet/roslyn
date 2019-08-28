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
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.AnalyzerDriver;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Stores the partial analysis state for analyzers executed on a specific compilation.
    /// </summary>
    internal partial class AnalysisState
    {
        private readonly SemaphoreSlim _gate;

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

        private readonly HashSet<ISymbol> _partialSymbolsWithGeneratedSourceEvents;
        private readonly CompilationData _compilationData;
        private readonly CompilationOptions _compilationOptions;

        private readonly ObjectPool<HashSet<CompilationEvent>> _compilationEventsPool;
        private readonly HashSet<CompilationEvent> _pooledEventsWithAnyActionsSet;

        public AnalysisState(ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationData compilationData, CompilationOptions compilationOptions)
        {
            _gate = new SemaphoreSlim(initialCount: 1);
            _analyzerStateMap = CreateAnalyzerStateMap(analyzers, out _analyzerStates);
            _compilationData = compilationData;
            _compilationOptions = compilationOptions;
            _pendingSourceEvents = new Dictionary<SyntaxTree, HashSet<CompilationEvent>>();
            _pendingNonSourceEvents = new HashSet<CompilationEvent>();
            _lazyAnalyzerActionCountsMap = null;
            _partialSymbolsWithGeneratedSourceEvents = new HashSet<ISymbol>();
            _compilationEventsPool = new ObjectPool<HashSet<CompilationEvent>>(() => new HashSet<CompilationEvent>());
            _pooledEventsWithAnyActionsSet = new HashSet<CompilationEvent>();
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, int> CreateAnalyzerStateMap(ImmutableArray<DiagnosticAnalyzer> analyzers, out ImmutableArray<PerAnalyzerState> analyzerStates)
        {
            var analyzerStateDataPool = new ObjectPool<AnalyzerStateData>(() => new AnalyzerStateData());
            var declarationAnalyzerStateDataPool = new ObjectPool<DeclarationAnalyzerStateData>(() => new DeclarationAnalyzerStateData());
            var currentlyAnalyzingDeclarationsMapPool = new ObjectPool<Dictionary<int, DeclarationAnalyzerStateData>>(
                () => new Dictionary<int, DeclarationAnalyzerStateData>());

            var statesBuilder = ImmutableArray.CreateBuilder<PerAnalyzerState>();
            var map = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, int>();
            var index = 0;
            foreach (var analyzer in analyzers)
            {
                statesBuilder.Add(new PerAnalyzerState(analyzerStateDataPool, declarationAnalyzerStateDataPool, currentlyAnalyzingDeclarationsMapPool));
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

        public async Task OnCompilationEventsGeneratedAsync(ImmutableArray<CompilationEvent> compilationEvents, AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            await EnsureAnalyzerActionCountsInitializedAsync(driver, cancellationToken).ConfigureAwait(false);

            using (_gate.DisposableWait(cancellationToken))
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
                foreach (var (analyzer, _) in _analyzerStateMap)
                {
                    var actionCounts = await driver.GetAnalyzerActionCountsAsync(analyzer, _compilationOptions, cancellationToken).ConfigureAwait(false);
                    builder.Add(analyzer, actionCounts);
                }

                Interlocked.CompareExchange(ref _lazyAnalyzerActionCountsMap, builder.ToImmutable(), null);
            }
        }

        internal async Task<AnalyzerActionCounts> GetOrComputeAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            await EnsureAnalyzerActionCountsInitializedAsync(driver, cancellationToken).ConfigureAwait(false);
            return _lazyAnalyzerActionCountsMap[analyzer];
        }

        internal AnalyzerActionCounts GetAnalyzerActionCounts(DiagnosticAnalyzer analyzer)
        {
            Debug.Assert(_lazyAnalyzerActionCountsMap != null);
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

        private async Task OnSymbolDeclaredEventProcessedAsync(
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            Func<ISymbol, DiagnosticAnalyzer, Task> onSymbolAndMembersProcessedAsync)
        {
            foreach (var analyzer in analyzers)
            {
                var analyzerState = GetAnalyzerState(analyzer);
                if (analyzerState.OnSymbolDeclaredEventProcessed(symbolDeclaredEvent))
                {
                    await onSymbolAndMembersProcessedAsync(symbolDeclaredEvent.Symbol, analyzer).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Invoke this method at completion of event processing for the given analyzers.
        /// It updates the analysis state of this event for each analyzer and if the event has been fully processed for all analyzers, then removes it from our event cache.
        /// </summary>
        public async Task OnCompilationEventProcessedAsync(CompilationEvent compilationEvent, ImmutableArray<DiagnosticAnalyzer> analyzers, Func<ISymbol, DiagnosticAnalyzer, Task> onSymbolAndMembersProcessedAsync)
        {
            // Analyze if the symbol and all its declaring syntax references are analyzed.
            var symbolDeclaredEvent = compilationEvent as SymbolDeclaredCompilationEvent;
            if (symbolDeclaredEvent != null)
            {
                await OnSymbolDeclaredEventProcessedAsync(symbolDeclaredEvent, analyzers, onSymbolAndMembersProcessedAsync).ConfigureAwait(false);
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
            // Note: We do not pass in the cancellationToken to DisposableWait to ensure the state is updated.
            using (_gate.DisposableWait())
            {
                UpdateEventsMap_NoLock(compilationEvent, add: false);
            }
        }

        /// <summary>
        /// Gets pending events for given set of analyzers for the given syntax tree.
        /// </summary>
        public ImmutableArray<CompilationEvent> GetPendingEvents(ImmutableArray<DiagnosticAnalyzer> analyzers, SyntaxTree tree, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
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
        /// <param name="cancellationToken">Cancellation token.</param>
        public ImmutableArray<CompilationEvent> GetPendingEvents(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            bool includeSourceEvents,
            bool includeNonSourceEvents,
            CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
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
        public bool HasPendingSymbolAnalysis(AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.FilterTreeOpt != null);

            var symbolDeclaredEvents = GetPendingSymbolDeclaredEvents(analysisScope.FilterTreeOpt, cancellationToken);
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

        private ImmutableArray<SymbolDeclaredCompilationEvent> GetPendingSymbolDeclaredEvents(SyntaxTree tree, CancellationToken cancellationToken)
        {
            Debug.Assert(tree != null);

            using (_gate.DisposableWait(cancellationToken))
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
        /// Checks if the given event has been fully analyzed for the given analyzer.
        /// </summary>
        public bool IsEventComplete(CompilationEvent compilationEvent, DiagnosticAnalyzer analyzer)
        {
            return GetAnalyzerState(analyzer).IsEventAnalyzed(compilationEvent);
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
        /// Attempts to start executing a symbol's end actions for the given analyzer.
        /// </summary>
        /// <returns>
        /// Returns false if the symbol end actions have already been executed for the analyzer OR are currently being executed by another task.
        /// If true, then it returns a non-null <paramref name="state"/> representing partial analysis state for the given symbol end actions for the given analyzer.
        /// </returns>
        public bool TryStartSymbolEndAnalysis(ISymbol symbol, DiagnosticAnalyzer analyzer, out AnalyzerStateData state)
        {
            return GetAnalyzerState(analyzer).TryStartSymbolEndAnalysis(symbol, out state);
        }

        /// <summary>
        /// Marks the given symbol as fully analyzed for the given analyzer.
        /// </summary>
        public void MarkSymbolComplete(ISymbol symbol, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerState(analyzer).MarkSymbolComplete(symbol);
        }

        /// <summary>
        /// True if the given symbol is fully analyzed for the given analyzer.
        /// </summary>
        public bool IsSymbolComplete(ISymbol symbol, DiagnosticAnalyzer analyzer)
        {
            return GetAnalyzerState(analyzer).IsSymbolComplete(symbol);
        }

        /// <summary>
        /// Marks the given symbol end actions as fully executed for the given analyzers.
        /// </summary>
        public void MarkSymbolEndAnalysisComplete(ISymbol symbol, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                MarkSymbolEndAnalysisComplete(symbol, analyzer);
            }
        }

        /// <summary>
        /// Marks the given symbol end actions as fully executed for the given analyzer.
        /// </summary>
        public void MarkSymbolEndAnalysisComplete(ISymbol symbol, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerState(analyzer).MarkSymbolEndAnalysisComplete(symbol);
        }

        /// <summary>
        /// True if the given symbol end analysis is complete for the given analyzer.
        /// </summary>
        public bool IsSymbolEndAnalysisComplete(ISymbol symbol, DiagnosticAnalyzer analyzer)
        {
            return GetAnalyzerState(analyzer).IsSymbolEndAnalysisComplete(symbol);
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
        /// True if the given symbol declaration is fully analyzed for all the analyzers.
        /// </summary>
        public bool IsDeclarationComplete(ISymbol symbol, int declarationIndex)
        {
            return IsDeclarationComplete(symbol, declarationIndex, _analyzerStates);
        }

        /// <summary>
        /// True if the given symbol declaration is fully analyzed for the given analyzer.
        /// </summary>
        public bool IsDeclarationComplete(ISymbol symbol, int declarationIndex, DiagnosticAnalyzer analyzer)
        {
            var analyzerState = GetAnalyzerState(analyzer);
            return IsDeclarationComplete(symbol, declarationIndex, SpecializedCollections.SingletonEnumerable(analyzerState));
        }

        private static bool IsDeclarationComplete(ISymbol symbol, int declarationIndex, IEnumerable<PerAnalyzerState> analyzerStates)
        {
            foreach (var analyzerState in analyzerStates)
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
