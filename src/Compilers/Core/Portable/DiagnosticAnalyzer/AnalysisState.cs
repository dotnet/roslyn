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
            _gate = new object();
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

            lock (_gate)
            {
                OnCompilationEventsGenerated_NoLock(compilationEvents, driver, cancellationToken);
            }
        }

        private void OnCompilationEventsGenerated_NoLock(ImmutableArray<CompilationEvent> compilationEvents, AnalyzerDriver driver, CancellationToken cancellationToken)
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
        public bool TryStartAnalyzingDeclaration(SyntaxReference decl, DiagnosticAnalyzer analyzer, out DeclarationAnalyzerStateData state)
        {
            return GetAnalyzerState(analyzer).TryStartAnalyzingDeclaration(decl, out state);
        }

        /// <summary>
        /// True if the given symbol declaration is fully analyzed.
        /// </summary>
        public bool IsDeclarationComplete(SyntaxNode decl)
        {
            foreach (var analyzerState in _analyzerStates)
            {
                if (!analyzerState.IsDeclarationComplete(decl))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Marks the given symbol declaration as fully analyzed for the given analyzer.
        /// </summary>
        public void MarkDeclarationComplete(SyntaxReference decl, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerState(analyzer).MarkDeclarationComplete(decl);
        }

        /// <summary>
        /// Marks all the symbol declarations for the given symbol as fully analyzed for all the given analyzers.
        /// </summary>
        public void MarkDeclarationsComplete(ImmutableArray<SyntaxReference> declarations, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                GetAnalyzerState(analyzer).MarkDeclarationsComplete(declarations);
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
    }
}
