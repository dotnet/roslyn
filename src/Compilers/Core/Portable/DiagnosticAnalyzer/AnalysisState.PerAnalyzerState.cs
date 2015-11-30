// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
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

            public async Task<bool> HasPendingSyntaxAnalysisAsync(SyntaxTree treeOpt, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    return _lazyPendingSyntaxAnalysisTrees != null &&
                        (treeOpt != null ? _lazyPendingSyntaxAnalysisTrees.ContainsKey(treeOpt) : _lazyPendingSyntaxAnalysisTrees.Count > 0);
                }
            }

            public async Task<bool> HasPendingSymbolAnalysisAsync(ISymbol symbol, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    return _pendingSymbols.ContainsKey(symbol);
                }
            }

            private async Task<TAnalyzerStateData> TryStartProcessingEntityAsync<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, ObjectPool<TAnalyzerStateData> pool, CancellationToken cancellationToken)
                where TAnalyzerStateData : AnalyzerStateData, new()
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    return TryStartProcessingEntity_NoLock(analysisEntity, pendingEntities, pool);
                }
            }

            private static TAnalyzerStateData TryStartProcessingEntity_NoLock<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
            {
                TAnalyzerStateData state;
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
                    return state;
                }

                return null;
            }

            private async Task MarkEntityProcessedAsync<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, ObjectPool<TAnalyzerStateData> pool, CancellationToken cancellationToken)
                where TAnalyzerStateData : AnalyzerStateData
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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

            private async Task<bool> IsEntityFullyProcessedAsync<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities, CancellationToken cancellationToken)
                where TAnalyzerStateData : AnalyzerStateData
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    return IsEntityFullyProcessed_NoLock(analysisEntity, pendingEntities);
                }
            }

            private static bool IsEntityFullyProcessed_NoLock<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData> pendingEntities)
                where TAnalyzerStateData : AnalyzerStateData
            {
                return !pendingEntities.ContainsKey(analysisEntity);
            }

            public Task<AnalyzerStateData> TryStartProcessingEventAsync(CompilationEvent compilationEvent, CancellationToken cancellationToken)
            {
                return TryStartProcessingEntityAsync(compilationEvent, _pendingEvents, _analyzerStateDataPool, cancellationToken);
            }

            public Task MarkEventCompleteAsync(CompilationEvent compilationEvent, CancellationToken cancellationToken)
            {
                return MarkEntityProcessedAsync(compilationEvent, _pendingEvents, _analyzerStateDataPool, cancellationToken);
            }

            public Task<AnalyzerStateData> TryStartAnalyzingSymbolAsync(ISymbol symbol, CancellationToken cancellationToken)
            {
                return TryStartProcessingEntityAsync(symbol, _pendingSymbols, _analyzerStateDataPool, cancellationToken);
            }

            public Task MarkSymbolCompleteAsync(ISymbol symbol, CancellationToken cancellationToken)
            {
                return MarkEntityProcessedAsync(symbol, _pendingSymbols, _analyzerStateDataPool, cancellationToken);
            }

            public Task<DeclarationAnalyzerStateData> TryStartAnalyzingDeclarationAsync(SyntaxReference decl, CancellationToken cancellationToken)
            {
                return TryStartProcessingEntityAsync(decl.GetSyntax(), _pendingDeclarations, _declarationAnalyzerStateDataPool, cancellationToken);
            }

            public Task<bool> IsDeclarationCompleteAsync(SyntaxNode decl, CancellationToken cancellationToken)
            {
                return IsEntityFullyProcessedAsync(decl, _pendingDeclarations, cancellationToken);
            }

            public Task MarkDeclarationCompleteAsync(SyntaxReference decl, CancellationToken cancellationToken)
            {
                return MarkEntityProcessedAsync(decl.GetSyntax(), _pendingDeclarations, _declarationAnalyzerStateDataPool, cancellationToken);
            }

            public Task<AnalyzerStateData> TryStartSyntaxAnalysisAsync(SyntaxTree tree, CancellationToken cancellationToken)
            {
                Debug.Assert(_lazyPendingSyntaxAnalysisTrees != null);
                return TryStartProcessingEntityAsync(tree, _lazyPendingSyntaxAnalysisTrees, _analyzerStateDataPool, cancellationToken);
            }

            public async Task MarkSyntaxAnalysisCompleteAsync(SyntaxTree tree, CancellationToken cancellationToken)
            {
                if (_lazyPendingSyntaxAnalysisTrees != null)
                {
                    await MarkEntityProcessedAsync(tree, _lazyPendingSyntaxAnalysisTrees, _analyzerStateDataPool, cancellationToken).ConfigureAwait(false);
                }
            }

            public async Task MarkDeclarationsCompleteAsync(ImmutableArray<SyntaxReference> declarations, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    foreach (var syntaxRef in declarations)
                    {
                        MarkEntityProcessed_NoLock(syntaxRef.GetSyntax(), _pendingDeclarations, _declarationAnalyzerStateDataPool);
                    }
                }
            }

            public async Task OnCompilationEventGeneratedAsync(CompilationEvent compilationEvent, AnalyzerActionCounts actionCounts, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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

            public Task<bool> IsEventAnalyzedAsync(CompilationEvent compilationEvent, CancellationToken cancellationToken)
            {
                return IsEntityFullyProcessedAsync(compilationEvent, _pendingEvents, cancellationToken);
            }

            public async Task OnSymbolDeclaredEventProcessedAsync(SymbolDeclaredCompilationEvent symbolDeclaredEvent, CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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
