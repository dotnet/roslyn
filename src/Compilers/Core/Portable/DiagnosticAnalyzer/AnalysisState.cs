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


        /// <summary>
        /// Cached semantic model for the compilation trees.
        /// PERF: This cache enables us to re-use semantic model's bound node cache across analyzer execution and diagnostic queries.
        /// </summary>
        private readonly ConditionalWeakTable<SyntaxTree, SemanticModel> _semanticModelsMap;

        private readonly ObjectPool<HashSet<CompilationEvent>> _compilationEventsPool;
        private readonly HashSet<CompilationEvent> _pooledEventsWithAnyActionsSet;
        private bool _compilationEndAnalyzed;

        public AnalysisState(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            _gate = new SemaphoreSlim(initialCount: 1);
            _analyzerStateMap = CreateAnalyzerStateMap(analyzers, out _analyzerStates);
            _pendingSourceEvents = new Dictionary<SyntaxTree, HashSet<CompilationEvent>>();
            _pendingNonSourceEvents = new HashSet<CompilationEvent>();
            _lazyAnalyzerActionCountsMap = null;
            _semanticModelsMap = new ConditionalWeakTable<SyntaxTree, SemanticModel>();
            _compilationEventsPool = new ObjectPool<HashSet<CompilationEvent>>(() => new HashSet<CompilationEvent>());
            _pooledEventsWithAnyActionsSet = new HashSet<CompilationEvent>();
            _compilationEndAnalyzed = false;
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, int> CreateAnalyzerStateMap(ImmutableArray<DiagnosticAnalyzer> analyzers, out ImmutableArray<PerAnalyzerState> analyzerStates)
        {
            var analyzerStateDataPool = new ObjectPool<AnalyzerStateData>(() => new AnalyzerStateData());
            var declarationAnalyzerStateDataPool = new ObjectPool<DeclarationAnalyzerStateData>(() => new DeclarationAnalyzerStateData());

            var statesBuilder = ImmutableArray.CreateBuilder<PerAnalyzerState>();
            var map = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, int>();
            var index = 0;
            foreach (var analyzer in analyzers)
            {
                statesBuilder.Add(new PerAnalyzerState(analyzerStateDataPool, declarationAnalyzerStateDataPool));
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

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await OnCompilationEventsGenerated_NoLockAsync(compilationEvents, driver, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task OnCompilationEventsGenerated_NoLockAsync(ImmutableArray<CompilationEvent> compilationEvents, AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            Debug.Assert(_lazyAnalyzerActionCountsMap != null);

            // Add the events to our global pending events map.
            AddToEventsMap_NoLock(compilationEvents);

            // Mark the events for analysis for each analyzer.
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
                        await analyzerState.OnCompilationEventGeneratedAsync(compilationEvent, actionCounts, cancellationToken).ConfigureAwait(false);
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
        }

        private void AddToEventsMap_NoLock(ImmutableArray<CompilationEvent> compilationEvents)
        {
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
                        _compilationEndAnalyzed |= compilationEvent is CompilationCompletedEvent;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unexpected compilation event of type " + compilationEvent.GetType().Name);
                }
            }

            if (_compilationEndAnalyzed && _pendingSourceEvents.Count == 0)
            {
                // Clear the per-compilation data cache if we finished analyzing this compilation.  
                AnalyzerDriver.RemoveCachedCompilationData(compilationEvent.Compilation);
            }
        }

        private void AddPendingSourceEvent_NoLock(SyntaxTree tree, CompilationEvent compilationEvent)
        {
            HashSet<CompilationEvent> currentEvents;
            if (!_pendingSourceEvents.TryGetValue(tree, out currentEvents))
            {
                currentEvents = _compilationEventsPool.Allocate();
                _pendingSourceEvents[tree] = currentEvents;
                AnalyzerDriver.RemoveCachedSemanticModel(tree, compilationEvent.Compilation);
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
                    _compilationEventsPool.Free(currentEvents);
                    _pendingSourceEvents.Remove(tree);
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
                return actionCounts.CodeBlockActionsCount > 0 ||
                    actionCounts.CodeBlockStartActionsCount > 0 ||
                    actionCounts.SymbolActionsCount > 0 ||
                    actionCounts.SyntaxNodeActionsCount > 0;
            }
            else
            {
                return actionCounts.SemanticModelActionsCount > 0;
            }
        }

        private async Task OnSymbolDeclaredEventProcessedAsync(SymbolDeclaredCompilationEvent symbolDeclaredEvent, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            foreach (var analyzer in analyzers)
            {
                var analyzerState = GetAnalyzerState(analyzer);
                await analyzerState.OnSymbolDeclaredEventProcessedAsync(symbolDeclaredEvent, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Invoke this method at completion of event processing for the given analysis scope.
        /// It updates the analysis state of this event for each analyzer and if the event has been fully processed for all analyzers, then removes it from our event cache.
        /// </summary>
        public async Task OnCompilationEventProcessedAsync(CompilationEvent compilationEvent, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            // Analyze if the symbol and all its declaring syntax references are analyzed.
            var symbolDeclaredEvent = compilationEvent as SymbolDeclaredCompilationEvent;
            if (symbolDeclaredEvent != null)
            {
                await OnSymbolDeclaredEventProcessedAsync(symbolDeclaredEvent, analysisScope.Analyzers, cancellationToken).ConfigureAwait(false);
            }

            // Check if event is fully analyzed for all analyzers.
            foreach (var analyzerState in _analyzerStates)
            {
                var eventAnalyzed = await analyzerState.IsEventAnalyzedAsync(compilationEvent, cancellationToken).ConfigureAwait(false);
                if (!eventAnalyzed)
                {
                    return;
                }
            }

            // Remove the event from event map.
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                UpdateEventsMap_NoLock(compilationEvent, add: false);
            }
        }

        /// <summary>
        /// Gets pending events for given set of analyzers for the given syntax tree.
        /// </summary>
        public async Task<ImmutableArray<CompilationEvent>> GetPendingEventsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, SyntaxTree tree, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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
                foreach (var pendingEvent in analyzerState.PendingEvents_NoLock)
                {
                    uniqueEvents.Add(pendingEvent);
                }
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
        public async Task<ImmutableArray<CompilationEvent>> GetPendingEventsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, bool includeSourceEvents, bool includeNonSourceEvents, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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
        public async Task<bool> HasPendingSyntaxAnalysisAsync(AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            if (analysisScope.IsTreeAnalysis && !analysisScope.IsSyntaxOnlyTreeAnalysis)
            {
                return false;
            }

            foreach (var analyzer in analysisScope.Analyzers)
            {
                var analyzerState = GetAnalyzerState(analyzer);
                if (await analyzerState.HasPendingSyntaxAnalysisAsync(analysisScope.FilterTreeOpt, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if we have any pending symbol analysis for given analysis scope.
        /// </summary>
        public async Task<bool> HasPendingSymbolAnalysisAsync(AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.FilterTreeOpt != null);

            var symbolDeclaredEvents = await GetPendingSymbolDeclaredEventsAsync(analysisScope.FilterTreeOpt, cancellationToken).ConfigureAwait(false);
            foreach (var symbolDeclaredEvent in symbolDeclaredEvents)
            {
                if (analysisScope.ShouldAnalyze(symbolDeclaredEvent.Symbol))
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        var analyzerState = GetAnalyzerState(analyzer);
                        if (await analyzerState.HasPendingSymbolAnalysisAsync(symbolDeclaredEvent.Symbol, cancellationToken).ConfigureAwait(false))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private async Task<ImmutableArray<SymbolDeclaredCompilationEvent>> GetPendingSymbolDeclaredEventsAsync(SyntaxTree tree, CancellationToken cancellationToken)
        {
            Debug.Assert(tree != null);

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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
        /// Returns null if the event has already been processed for the analyzer OR is currently being processed by another task.
        /// Otherwise, returns a non-null state representing partial analysis state for the given event for the given analyzer.
        /// </returns>
        public Task<AnalyzerStateData> TryStartProcessingEventAsync(CompilationEvent compilationEvent, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).TryStartProcessingEventAsync(compilationEvent, cancellationToken);
        }

        /// <summary>
        /// Marks the given event as fully analyzed for the given analyzer.
        /// </summary>
        public Task MarkEventCompleteAsync(CompilationEvent compilationEvent, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).MarkEventCompleteAsync(compilationEvent, cancellationToken);
        }

        /// <summary>
        /// Attempts to start processing a symbol for the given analyzer's symbol actions.
        /// </summary>
        /// <returns>
        /// Returns null if the symbol has already been processed for the analyzer OR is currently being processed by another task.
        /// Otherwise, returns a non-null state representing partial analysis state for the given symbol for the given analyzer.
        /// </returns>
        public Task<AnalyzerStateData> TryStartAnalyzingSymbolAsync(ISymbol symbol, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).TryStartAnalyzingSymbolAsync(symbol, cancellationToken);
        }

        /// <summary>
        /// Marks the given symbol as fully analyzed for the given analyzer.
        /// </summary>
        public Task MarkSymbolCompleteAsync(ISymbol symbol, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).MarkSymbolCompleteAsync(symbol, cancellationToken);
        }

        /// <summary>
        /// Attempts to start processing a symbol declaration for the given analyzer's syntax node and code block actions.
        /// </summary>
        /// <returns>
        /// Returns null if the declaration has already been processed for the analyzer OR is currently being processed by another task.
        /// Otherwise, returns a non-null state representing partial analysis state for the given declaration for the given analyzer.
        /// </returns>
        public Task<DeclarationAnalyzerStateData> TryStartAnalyzingDeclarationAsync(SyntaxReference decl, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).TryStartAnalyzingDeclarationAsync(decl, cancellationToken);
        }

        /// <summary>
        /// Returns true if the given symbol declaration is fully analyzed.
        /// </summary>
        public async Task<bool> IsDeclarationCompleteAsync(SyntaxNode decl, CancellationToken cancellationToken)
        {
            foreach (var analyzerState in _analyzerStates)
            {
                var declarationComplete = await analyzerState.IsDeclarationCompleteAsync(decl, cancellationToken).ConfigureAwait(false);
                if (!declarationComplete)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Marks the given symbol declaration as fully analyzed for the given analyzer.
        /// </summary>
        public Task MarkDeclarationCompleteAsync(SyntaxReference decl, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).MarkDeclarationCompleteAsync(decl, cancellationToken);
        }

        /// <summary>
        /// Marks all the symbol declarations for the given symbol as fully analyzed for all the given analyzers.
        /// </summary>
        public async Task MarkDeclarationsCompleteAsync(ImmutableArray<SyntaxReference> declarations, IEnumerable<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            foreach (var analyzer in analyzers)
            {
                await GetAnalyzerState(analyzer).MarkDeclarationsCompleteAsync(declarations, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attempts to start processing a syntax tree for the given analyzer's syntax tree actions.
        /// </summary>
        /// <returns>
        /// Returns null if the tree has already been processed for the analyzer OR is currently being processed by another task.
        /// Otherwise, returns a non-null state representing partial syntax analysis state for the given tree for the given analyzer.
        /// </returns>
        public Task<AnalyzerStateData> TryStartSyntaxAnalysisAsync(SyntaxTree tree, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).TryStartSyntaxAnalysisAsync(tree, cancellationToken);
        }

        /// <summary>
        /// Marks the given tree as fully syntactically analyzed for the given analyzer.
        /// </summary>
        public Task MarkSyntaxAnalysisCompleteAsync(SyntaxTree tree, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return GetAnalyzerState(analyzer).MarkSyntaxAnalysisCompleteAsync(tree, cancellationToken);
        }
    }
}
