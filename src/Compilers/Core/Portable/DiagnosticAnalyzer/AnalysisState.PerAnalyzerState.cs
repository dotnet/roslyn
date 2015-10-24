// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
            private readonly Dictionary<SyntaxNode, DeclarationAnalyzerStateData> _pendingDeclarations = new Dictionary<SyntaxNode, DeclarationAnalyzerStateData>();
            private Dictionary<SyntaxTree, AnalyzerStateData> _lazyPendingSyntaxAnalysisTrees = null;

            private readonly ObjectPool<AnalyzerStateData> _analyzerStateDataPool = new ObjectPool<AnalyzerStateData>(() => new AnalyzerStateData());
            private readonly ObjectPool<DeclarationAnalyzerStateData> _declarationAnalyzerStateDataPool = new ObjectPool<DeclarationAnalyzerStateData>(() => new DeclarationAnalyzerStateData());

            public PerAnalyzerState(ObjectPool<AnalyzerStateData> analyzerStateDataPool, ObjectPool<DeclarationAnalyzerStateData> declarationAnalyzerStateDataPool)
            {
                _analyzerStateDataPool = analyzerStateDataPool;
                _declarationAnalyzerStateDataPool = declarationAnalyzerStateDataPool;
            }

            public IEnumerable<CompilationEvent> PendingEvents_NoLock => _pendingEvents.Keys;

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
                    if (state != null)
                    {
                        state.Free();
                        pool.Free(state);
                    }
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
                return !pendingEntities.ContainsKey(analysisEntity);
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

            public bool TryStartAnalyzingDeclaration(SyntaxReference decl, out DeclarationAnalyzerStateData state)
            {
                return TryStartProcessingEntity(decl.GetSyntax(), _pendingDeclarations, _declarationAnalyzerStateDataPool, out state);
            }

            public bool IsDeclarationComplete(SyntaxNode decl)
            {
                return IsEntityFullyProcessed(decl, _pendingDeclarations);
            }

            public void MarkDeclarationComplete(SyntaxReference decl)
            {
                MarkEntityProcessed(decl.GetSyntax(), _pendingDeclarations, _declarationAnalyzerStateDataPool);
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

            public void MarkDeclarationsComplete(ImmutableArray<SyntaxReference> declarations)
            {
                lock (_gate)
                {
                    foreach (var syntaxRef in declarations)
                    {
                        MarkEntityProcessed_NoLock(syntaxRef.GetSyntax(), _pendingDeclarations, _declarationAnalyzerStateDataPool);
                    }
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
                            (actionCounts.SyntaxNodeActionsCount > 0 ||
                            actionCounts.CodeBlockActionsCount > 0 ||
                            actionCounts.CodeBlockStartActionsCount > 0))
                        {
                            foreach (var syntaxRef in symbolEvent.DeclaringSyntaxReferences)
                            {
                                needsAnalysis = true;
                                _pendingDeclarations[syntaxRef.GetSyntax()] = null;
                            }
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
                foreach (var syntaxRef in symbolDeclaredEvent.DeclaringSyntaxReferences)
                {
                    if (!IsEntityFullyProcessed_NoLock(syntaxRef.GetSyntax(), _pendingDeclarations))
                    {
                        return;
                    }
                }

                // Mark the symbol event completely processed.
                MarkEntityProcessed_NoLock(symbolDeclaredEvent, _pendingEvents, _analyzerStateDataPool);
            }
        }
    }
}
