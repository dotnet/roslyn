// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Stores the current partial analysis state for an analyzer.
    /// </summary>
    internal partial class AnalysisState
    {
        private class PerAnalyzerState
        {
            private readonly object _gate = new object();
            private readonly Dictionary<CompilationEvent, AnalyzerStateData> _pendingEvents = new Dictionary<CompilationEvent, AnalyzerStateData>();
            private readonly Dictionary<ISymbol, AnalyzerStateData> _pendingSymbols = new Dictionary<ISymbol, AnalyzerStateData>();
            private readonly Dictionary<ISymbol, Dictionary<int, DeclarationAnalyzerStateData>> _pendingDeclarations = new Dictionary<ISymbol, Dictionary<int, DeclarationAnalyzerStateData>>();

            private Dictionary<SyntaxTree, AnalyzerStateData> _lazyPendingSyntaxAnalysisTrees = null;

            private readonly ObjectPool<AnalyzerStateData> _analyzerStateDataPool;
            private readonly ObjectPool<DeclarationAnalyzerStateData> _declarationAnalyzerStateDataPool;
            private readonly ObjectPool<Dictionary<int, DeclarationAnalyzerStateData>> _currentlyAnalyzingDeclarationsMapPool;

            public PerAnalyzerState(
                ObjectPool<AnalyzerStateData> analyzerStateDataPool,
                ObjectPool<DeclarationAnalyzerStateData> declarationAnalyzerStateDataPool,
                ObjectPool<Dictionary<int, DeclarationAnalyzerStateData>> currentlyAnalyzingDeclarationsMapPool)
            {
                _analyzerStateDataPool = analyzerStateDataPool;
                _declarationAnalyzerStateDataPool = declarationAnalyzerStateDataPool;
                _currentlyAnalyzingDeclarationsMapPool = currentlyAnalyzingDeclarationsMapPool;
            }

            public void AddPendingEvents(HashSet<CompilationEvent> uniqueEvents)
            {
                lock (_gate)
                {
                    foreach (var pendingEvent in _pendingEvents.Keys)
                    {
                        uniqueEvents.Add(pendingEvent);
                    }
                }
            }

            public bool HasPendingSyntaxAnalysis(SyntaxTree treeOpt)
            {
                lock (_gate)
                {
                    return _lazyPendingSyntaxAnalysisTrees != null &&
                        (treeOpt != null ? _lazyPendingSyntaxAnalysisTrees.ContainsKey(treeOpt) : _lazyPendingSyntaxAnalysisTrees.Count > 0);
                }
            }

            public bool HasPendingSymbolAnalysis(ISymbol symbol)
            {
                lock (_gate)
                {
                    return _pendingSymbols.ContainsKey(symbol);
                }
            }

            private bool TryStartProcessingEntity<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, ObjectPool<TAnalyzerStateData> pool, out TAnalyzerStateData newState)
                where TAnalyzerStateData : AnalyzerStateData, new()
            {
                lock (_gate)
                {
                    return TryStartProcessingEntity_NoLock(analysisEntity, pendingEntities, pool, out newState);
                }
            }

            private static bool TryStartProcessingEntity_NoLock<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, ObjectPool<TAnalyzerStateData> pool, out TAnalyzerStateData state)
                where TAnalyzerStateData : AnalyzerStateData
            {
                if (pendingEntities.TryGetValue(analysisEntity, out state) &&
                    (state == null || state.StateKind == StateKind.ReadyToProcess))
                {
                    if (state == null)
                    {
                        state = pool.Allocate();
                    }

                    state.SetStateKind(StateKind.InProcess);
                    Debug.Assert(state.StateKind == StateKind.InProcess);
                    pendingEntities[analysisEntity] = state;
                    return true;
                }

                state = null;
                return false;
            }

            private void MarkEntityProcessed<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
            {
                lock (_gate)
                {
                    MarkEntityProcessed_NoLock(analysisEntity, pendingEntities, pool);
                }
            }

            private static void MarkEntityProcessed_NoLock<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
            {
                TAnalyzerStateData state;
                if (pendingEntities.TryGetValue(analysisEntity, out state))
                {
                    pendingEntities.Remove(analysisEntity);
                    FreeState_NoLock(state, pool);
                }
            }

            private bool TryStartAnalyzingDeclaration_NoLock(ISymbol symbol, int declarationIndex, out DeclarationAnalyzerStateData state)
            {
                Dictionary<int, DeclarationAnalyzerStateData> declarationDataMap;
                if (!_pendingDeclarations.TryGetValue(symbol, out declarationDataMap))
                {
                    state = null;
                    return false;
                }

                if (declarationDataMap.TryGetValue(declarationIndex, out state))
                {
                    if (state.StateKind != StateKind.ReadyToProcess)
                    {
                        state = null;
                        return false;
                    }
                }
                else
                {
                    state = _declarationAnalyzerStateDataPool.Allocate();
                }

                state.SetStateKind(StateKind.InProcess);
                Debug.Assert(state.StateKind == StateKind.InProcess);
                declarationDataMap[declarationIndex] = state;
                return true;
            }

            private void MarkDeclarationProcessed(ISymbol symbol, int declarationIndex)
            {
                lock (_gate)
                {
                    MarkDeclarationProcessed_NoLock(symbol, declarationIndex);
                }
            }

            private void MarkDeclarationProcessed_NoLock(ISymbol symbol, int declarationIndex)
            {
                Dictionary<int, DeclarationAnalyzerStateData> declarationDataMap;
                if (!_pendingDeclarations.TryGetValue(symbol, out declarationDataMap))
                {
                    return;
                }

                DeclarationAnalyzerStateData state;
                if (declarationDataMap.TryGetValue(declarationIndex, out state))
                {
                    FreeDeclarationAnalyzerState_NoLock(state);
                }

                declarationDataMap[declarationIndex] = DeclarationAnalyzerStateData.FullyProcessedInstance;
            }

            private void MarkDeclarationsProcessed_NoLock(ISymbol symbol)
            {
                Dictionary<int, DeclarationAnalyzerStateData> declarationDataMap;
                if (_pendingDeclarations.TryGetValue(symbol, out declarationDataMap))
                {
                    FreeDeclarationDataMap_NoLock(declarationDataMap);
                    _pendingDeclarations.Remove(symbol);
                }
            }

            private void FreeDeclarationDataMap_NoLock(Dictionary<int, DeclarationAnalyzerStateData> declarationDataMap)
            {
                declarationDataMap.Clear();
                _currentlyAnalyzingDeclarationsMapPool.Free(declarationDataMap);
            }

            private void FreeDeclarationAnalyzerState_NoLock(DeclarationAnalyzerStateData state)
            {
                if (ReferenceEquals(state, DeclarationAnalyzerStateData.FullyProcessedInstance))
                {
                    return;
                }

                FreeState_NoLock(state, _declarationAnalyzerStateDataPool);
            }

            private static void FreeState_NoLock<TAnalyzerStateData>(TAnalyzerStateData state, ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
            {
                if (state != null)
                {
                    state.Free();
                    pool.Free(state);
                }
            }

            private bool IsEntityFullyProcessed<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities)
                where TAnalyzerStateData : AnalyzerStateData
            {
                lock (_gate)
                {
                    return IsEntityFullyProcessed_NoLock(analysisEntity, pendingEntities);
                }
            }

            private static bool IsEntityFullyProcessed_NoLock<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities)
                where TAnalyzerStateData : AnalyzerStateData
            {
                TAnalyzerStateData state;
                return !pendingEntities.TryGetValue(analysisEntity, out state) ||
                    state?.StateKind == StateKind.FullyProcessed;
            }

            private bool IsDeclarationComplete_NoLock(ISymbol symbol, int declarationIndex)
            {
                Dictionary<int, DeclarationAnalyzerStateData> declarationDataMap;
                if (!_pendingDeclarations.TryGetValue(symbol, out declarationDataMap))
                {
                    return true;
                }

                DeclarationAnalyzerStateData state;
                if (!declarationDataMap.TryGetValue(declarationIndex, out state))
                {
                    return false;
                }

                return state.StateKind == StateKind.FullyProcessed;
            }

            private bool AreDeclarationsProcessed_NoLock(ISymbol symbol, int declarationsCount)
            {
                Dictionary<int, DeclarationAnalyzerStateData> declarationDataMap;
                if (!_pendingDeclarations.TryGetValue(symbol, out declarationDataMap))
                {
                    return true;
                }

                return declarationDataMap.Count == declarationsCount &&
                    declarationDataMap.Values.All(state => state.StateKind == StateKind.FullyProcessed);
            }

            public bool TryStartProcessingEvent(CompilationEvent compilationEvent, out AnalyzerStateData state)
            {
                return TryStartProcessingEntity(compilationEvent, _pendingEvents, _analyzerStateDataPool, out state);
            }

            public void MarkEventComplete(CompilationEvent compilationEvent)
            {
                MarkEntityProcessed(compilationEvent, _pendingEvents, _analyzerStateDataPool);
            }

            public bool TryStartAnalyzingSymbol(ISymbol symbol, out AnalyzerStateData state)
            {
                return TryStartProcessingEntity(symbol, _pendingSymbols, _analyzerStateDataPool, out state);
            }

            public void MarkSymbolComplete(ISymbol symbol)
            {
                MarkEntityProcessed(symbol, _pendingSymbols, _analyzerStateDataPool);
            }

            public bool TryStartAnalyzingDeclaration(ISymbol symbol, int declarationIndex, out DeclarationAnalyzerStateData state)
            {
                lock (_gate)
                {
                    return TryStartAnalyzingDeclaration_NoLock(symbol, declarationIndex, out state);
                }
            }

            public bool IsDeclarationComplete(ISymbol symbol, int declarationIndex)
            {
                lock (_gate)
                {
                    return IsDeclarationComplete_NoLock(symbol, declarationIndex);
                }
            }

            public void MarkDeclarationComplete(ISymbol symbol, int declarationIndex)
            {
                lock (_gate)
                {
                    MarkDeclarationProcessed_NoLock(symbol, declarationIndex);
                }
            }

            public void MarkDeclarationsComplete(ISymbol symbol)
            {
                lock (_gate)
                {
                    MarkDeclarationsProcessed_NoLock(symbol);
                }
            }

            public bool TryStartSyntaxAnalysis(SyntaxTree tree, out AnalyzerStateData state)
            {
                Debug.Assert(_lazyPendingSyntaxAnalysisTrees != null);
                return TryStartProcessingEntity(tree, _lazyPendingSyntaxAnalysisTrees, _analyzerStateDataPool, out state);
            }

            public void MarkSyntaxAnalysisComplete(SyntaxTree tree)
            {
                if (_lazyPendingSyntaxAnalysisTrees != null)
                {
                    MarkEntityProcessed(tree, _lazyPendingSyntaxAnalysisTrees, _analyzerStateDataPool);
                }
            }

            public void OnCompilationEventGenerated(CompilationEvent compilationEvent, AnalyzerActionCounts actionCounts)
            {
                lock (_gate)
                {
                    var symbolEvent = compilationEvent as SymbolDeclaredCompilationEvent;
                    if (symbolEvent != null)
                    {
                        var needsAnalysis = false;
                        var symbol = symbolEvent.Symbol;
                        if (!AnalysisScope.ShouldSkipSymbolAnalysis(symbolEvent) && actionCounts.SymbolActionsCount > 0)
                        {
                            needsAnalysis = true;
                            _pendingSymbols[symbol] = null;
                        }

                        if (!AnalysisScope.ShouldSkipDeclarationAnalysis(symbol) &&
                            actionCounts.HasAnyExecutableCodeActions)
                        {
                            needsAnalysis = true;
                            _pendingDeclarations[symbol] = _currentlyAnalyzingDeclarationsMapPool.Allocate();
                        }

                        if (!needsAnalysis)
                        {
                            return;
                        }
                    }
                    else if (compilationEvent is CompilationStartedEvent)
                    {
                        if (actionCounts.SyntaxTreeActionsCount > 0)
                        {
                            var trees = compilationEvent.Compilation.SyntaxTrees;
                            var map = new Dictionary<SyntaxTree, AnalyzerStateData>(trees.Count());
                            foreach (var tree in trees)
                            {
                                map[tree] = null;
                            }

                            _lazyPendingSyntaxAnalysisTrees = map;
                        }

                        if (actionCounts.CompilationActionsCount == 0)
                        {
                            return;
                        }
                    }

                    _pendingEvents[compilationEvent] = null;
                }
            }

            public bool IsEventAnalyzed(CompilationEvent compilationEvent)
            {
                return IsEntityFullyProcessed(compilationEvent, _pendingEvents);
            }

            public void OnSymbolDeclaredEventProcessed(SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                lock (_gate)
                {
                    OnSymbolDeclaredEventProcessed_NoLock(symbolDeclaredEvent);
                }
            }

            private void OnSymbolDeclaredEventProcessed_NoLock(SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                // Check if the symbol event has been completely processed or not.

                // Have the symbol actions executed?
                if (!IsEntityFullyProcessed_NoLock(symbolDeclaredEvent.Symbol, _pendingSymbols))
                {
                    return;
                }

                // Have the node/code block actions executed for all symbol declarations?
                if (!AreDeclarationsProcessed_NoLock(symbolDeclaredEvent.Symbol, symbolDeclaredEvent.DeclaringSyntaxReferences.Length))
                {
                    return;
                }

                // Mark the symbol event completely processed.
                MarkEntityProcessed_NoLock(symbolDeclaredEvent, _pendingEvents, _analyzerStateDataPool);

                // Mark declarations completely processed.
                MarkDeclarationsProcessed_NoLock(symbolDeclaredEvent.Symbol);
            }
        }
    }
}
