// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.PooledObjects;
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
            private readonly object _gate;
            private readonly Dictionary<CompilationEvent, AnalyzerStateData?> _pendingEvents;
            private readonly Dictionary<ISymbol, AnalyzerStateData?> _pendingSymbols;
            private readonly Dictionary<ISymbol, Dictionary<int, DeclarationAnalyzerStateData>?> _pendingDeclarations;

            private Dictionary<SourceOrAdditionalFile, AnalyzerStateData>? _lazyFilesWithAnalysisData;
            private int _pendingSyntaxAnalysisFilesCount;
            private Dictionary<ISymbol, AnalyzerStateData?>? _lazyPendingSymbolEndAnalyses;

            private readonly ObjectPool<AnalyzerStateData> _analyzerStateDataPool;
            private readonly ObjectPool<DeclarationAnalyzerStateData> _declarationAnalyzerStateDataPool;
            private readonly ObjectPool<Dictionary<int, DeclarationAnalyzerStateData>> _currentlyAnalyzingDeclarationsMapPool;

            public PerAnalyzerState(
                ObjectPool<AnalyzerStateData> analyzerStateDataPool,
                ObjectPool<DeclarationAnalyzerStateData> declarationAnalyzerStateDataPool,
                ObjectPool<Dictionary<int, DeclarationAnalyzerStateData>> currentlyAnalyzingDeclarationsMapPool)
            {
                _gate = new object();
                _pendingEvents = new Dictionary<CompilationEvent, AnalyzerStateData?>();
                _pendingSymbols = new Dictionary<ISymbol, AnalyzerStateData?>();
                _pendingDeclarations = new Dictionary<ISymbol, Dictionary<int, DeclarationAnalyzerStateData>?>();

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

            public bool HasPendingSyntaxAnalysis(SourceOrAdditionalFile? file)
            {
                lock (_gate)
                {
                    if (_pendingSyntaxAnalysisFilesCount == 0)
                    {
                        return false;
                    }

                    Debug.Assert(_lazyFilesWithAnalysisData != null);

                    if (!file.HasValue)
                    {
                        // We have syntax analysis pending for at least one file.
                        return true;
                    }

                    if (!_lazyFilesWithAnalysisData.TryGetValue(file.Value, out var state))
                    {
                        // We haven't even started analysis for this file.
                        return true;
                    }

                    // See if we have completed analysis for this file.
                    return state.StateKind != StateKind.FullyProcessed;
                }
            }

            public bool HasPendingSymbolAnalysis(ISymbol symbol)
            {
                lock (_gate)
                {
                    return _pendingSymbols.ContainsKey(symbol) ||
                        _lazyPendingSymbolEndAnalyses?.ContainsKey(symbol) == true;
                }
            }

            private bool TryStartProcessingEntity<TAnalysisEntity, TAnalyzerStateData>(
                TAnalysisEntity analysisEntity,
                Dictionary<TAnalysisEntity, TAnalyzerStateData?> pendingEntities,
                ObjectPool<TAnalyzerStateData> pool,
                [NotNullWhen(returnValue: true)] out TAnalyzerStateData? newState)
                where TAnalyzerStateData : AnalyzerStateData, new()
                where TAnalysisEntity : notnull
            {
                lock (_gate)
                {
                    return TryStartProcessingEntity_NoLock(analysisEntity, pendingEntities, pool, out newState);
                }
            }

            private static bool TryStartProcessingEntity_NoLock<TAnalysisEntity, TAnalyzerStateData>(
                TAnalysisEntity analysisEntity,
                Dictionary<TAnalysisEntity, TAnalyzerStateData?> pendingEntities,
                ObjectPool<TAnalyzerStateData> pool,
                [NotNullWhen(returnValue: true)] out TAnalyzerStateData? state)
                where TAnalyzerStateData : AnalyzerStateData
                where TAnalysisEntity : notnull
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

            private void MarkEntityProcessed<TAnalysisEntity, TAnalyzerStateData>(
                TAnalysisEntity analysisEntity,
                Dictionary<TAnalysisEntity, TAnalyzerStateData?> pendingEntities,
                ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
                where TAnalysisEntity : notnull
            {
                lock (_gate)
                {
                    MarkEntityProcessed_NoLock(analysisEntity, pendingEntities, pool);
                }
            }

            private static bool MarkEntityProcessed_NoLock<TAnalysisEntity, TAnalyzerStateData>(
                TAnalysisEntity analysisEntity,
                Dictionary<TAnalysisEntity, TAnalyzerStateData?> pendingEntities,
                ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
                where TAnalysisEntity : notnull
            {
                if (pendingEntities.TryGetValue(analysisEntity, out var state))
                {
                    pendingEntities.Remove(analysisEntity);
                    FreeState_NoLock(state, pool);
                    return true;
                }

                return false;
            }

            private bool TryStartSyntaxAnalysis_NoLock(SourceOrAdditionalFile file, [NotNullWhen(returnValue: true)] out AnalyzerStateData? state)
            {
                Debug.Assert(_lazyFilesWithAnalysisData != null);

                if (_pendingSyntaxAnalysisFilesCount == 0)
                {
                    state = null;
                    return false;
                }

                if (_lazyFilesWithAnalysisData.TryGetValue(file, out state))
                {
                    if (state.StateKind != StateKind.ReadyToProcess)
                    {
                        state = null;
                        return false;
                    }
                }
                else
                {
                    state = _analyzerStateDataPool.Allocate();
                }

                state.SetStateKind(StateKind.InProcess);
                Debug.Assert(state.StateKind == StateKind.InProcess);
                _lazyFilesWithAnalysisData[file] = state;
                return true;
            }

            private void MarkSyntaxAnalysisComplete_NoLock(SourceOrAdditionalFile file)
            {
                if (_pendingSyntaxAnalysisFilesCount == 0)
                {
                    return;
                }

                Debug.Assert(_lazyFilesWithAnalysisData != null);

                var wasAlreadyFullyProcessed = false;
                if (_lazyFilesWithAnalysisData.TryGetValue(file, out var state))
                {
                    if (state.StateKind != StateKind.FullyProcessed)
                    {
                        FreeState_NoLock(state, _analyzerStateDataPool);
                    }
                    else
                    {
                        wasAlreadyFullyProcessed = true;
                    }
                }

                if (!wasAlreadyFullyProcessed)
                {
                    _pendingSyntaxAnalysisFilesCount--;
                }

                _lazyFilesWithAnalysisData[file] = AnalyzerStateData.FullyProcessedInstance;
            }

            private Dictionary<int, DeclarationAnalyzerStateData> EnsureDeclarationDataMap_NoLock(ISymbol symbol, Dictionary<int, DeclarationAnalyzerStateData>? declarationDataMap)
            {
                Debug.Assert(_pendingDeclarations[symbol] == declarationDataMap);

                if (declarationDataMap == null)
                {
                    declarationDataMap = _currentlyAnalyzingDeclarationsMapPool.Allocate();
                    _pendingDeclarations[symbol] = declarationDataMap;
                }

                return declarationDataMap;
            }

            private bool TryStartAnalyzingDeclaration_NoLock(
                ISymbol symbol,
                int declarationIndex,
                [NotNullWhen(returnValue: true)] out DeclarationAnalyzerStateData? state)
            {
                if (!_pendingDeclarations.TryGetValue(symbol, out var declarationDataMap))
                {
                    state = null;
                    return false;
                }

                declarationDataMap = EnsureDeclarationDataMap_NoLock(symbol, declarationDataMap);

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

            private void MarkDeclarationProcessed_NoLock(ISymbol symbol, int declarationIndex)
            {
                if (!_pendingDeclarations.TryGetValue(symbol, out var declarationDataMap))
                {
                    return;
                }

                declarationDataMap = EnsureDeclarationDataMap_NoLock(symbol, declarationDataMap);

                if (declarationDataMap.TryGetValue(declarationIndex, out var state))
                {
                    FreeDeclarationAnalyzerState_NoLock(state);
                }

                declarationDataMap[declarationIndex] = DeclarationAnalyzerStateData.FullyProcessedInstance;
            }

            private void MarkDeclarationsProcessed_NoLock(ISymbol symbol)
            {
                if (_pendingDeclarations.TryGetValue(symbol, out var declarationDataMap))
                {
                    FreeDeclarationDataMap_NoLock(declarationDataMap);
                    _pendingDeclarations.Remove(symbol);
                }
            }

            private void FreeDeclarationDataMap_NoLock(Dictionary<int, DeclarationAnalyzerStateData>? declarationDataMap)
            {
                if (declarationDataMap is object)
                {
                    declarationDataMap.Clear();
                    _currentlyAnalyzingDeclarationsMapPool.Free(declarationDataMap);
                }
            }

            private void FreeDeclarationAnalyzerState_NoLock(DeclarationAnalyzerStateData state)
            {
                if (ReferenceEquals(state, DeclarationAnalyzerStateData.FullyProcessedInstance))
                {
                    return;
                }

                FreeState_NoLock(state, _declarationAnalyzerStateDataPool);
            }

            private static void FreeState_NoLock<TAnalyzerStateData>(TAnalyzerStateData? state, ObjectPool<TAnalyzerStateData> pool)
                where TAnalyzerStateData : AnalyzerStateData
            {
                if (state != null && !ReferenceEquals(state, AnalyzerStateData.FullyProcessedInstance))
                {
                    state.Free();
                    pool.Free(state);
                }
            }

            private bool IsEntityFullyProcessed<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData?> pendingEntities)
                where TAnalyzerStateData : AnalyzerStateData
                where TAnalysisEntity : notnull
            {
                lock (_gate)
                {
                    return IsEntityFullyProcessed_NoLock(analysisEntity, pendingEntities);
                }
            }

            private static bool IsEntityFullyProcessed_NoLock<TAnalysisEntity, TAnalyzerStateData>(TAnalysisEntity analysisEntity, Dictionary<TAnalysisEntity, TAnalyzerStateData?> pendingEntities)
                where TAnalyzerStateData : AnalyzerStateData
                where TAnalysisEntity : notnull
            {
                return !pendingEntities.TryGetValue(analysisEntity, out var state) ||
                    state?.StateKind == StateKind.FullyProcessed;
            }

            private bool IsDeclarationComplete_NoLock(ISymbol symbol, int declarationIndex)
            {
                if (!_pendingDeclarations.TryGetValue(symbol, out var declarationDataMap))
                {
                    return true;
                }

                if (declarationDataMap == null ||
                    !declarationDataMap.TryGetValue(declarationIndex, out var state))
                {
                    return false;
                }

                return state.StateKind == StateKind.FullyProcessed;
            }

            private bool AreDeclarationsProcessed_NoLock(ISymbol symbol, int declarationsCount)
            {
                Debug.Assert(declarationsCount > 0);
                if (!_pendingDeclarations.TryGetValue(symbol, out var declarationDataMap))
                {
                    return true;
                }

                return declarationDataMap?.Count == declarationsCount &&
                    declarationDataMap.Values.All(state => state.StateKind == StateKind.FullyProcessed);
            }

            public bool TryStartProcessingEvent(CompilationEvent compilationEvent, [NotNullWhen(returnValue: true)] out AnalyzerStateData? state)
            {
                return TryStartProcessingEntity(compilationEvent, _pendingEvents, _analyzerStateDataPool, out state);
            }

            public void MarkEventComplete(CompilationEvent compilationEvent)
            {
                MarkEntityProcessed(compilationEvent, _pendingEvents, _analyzerStateDataPool);
            }

            public bool TryStartAnalyzingSymbol(ISymbol symbol, [NotNullWhen(returnValue: true)] out AnalyzerStateData? state)
            {
                return TryStartProcessingEntity(symbol, _pendingSymbols, _analyzerStateDataPool, out state);
            }

            public bool TryStartSymbolEndAnalysis(ISymbol symbol, [NotNullWhen(returnValue: true)] out AnalyzerStateData? state)
            {
                Debug.Assert(_lazyPendingSymbolEndAnalyses != null);
                return TryStartProcessingEntity(symbol, _lazyPendingSymbolEndAnalyses, _analyzerStateDataPool, out state);
            }

            public void MarkSymbolComplete(ISymbol symbol)
            {
                MarkEntityProcessed(symbol, _pendingSymbols, _analyzerStateDataPool);
            }

            public void MarkSymbolEndAnalysisComplete(ISymbol symbol)
            {
                if (_lazyPendingSymbolEndAnalyses != null)
                {
                    MarkEntityProcessed(symbol, _lazyPendingSymbolEndAnalyses, _analyzerStateDataPool);
                }
            }

            public bool TryStartAnalyzingDeclaration(
                ISymbol symbol,
                int declarationIndex,
                [NotNullWhen(returnValue: true)] out DeclarationAnalyzerStateData? state)
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

            public bool TryStartSyntaxAnalysis(SourceOrAdditionalFile tree, [NotNullWhen(returnValue: true)] out AnalyzerStateData? state)
            {
                lock (_gate)
                {
                    Debug.Assert(_lazyFilesWithAnalysisData != null);
                    return TryStartSyntaxAnalysis_NoLock(tree, out state);
                }
            }

            public void MarkSyntaxAnalysisComplete(SourceOrAdditionalFile file)
            {
                lock (_gate)
                {
                    MarkSyntaxAnalysisComplete_NoLock(file);
                }
            }

            public void OnCompilationEventGenerated(CompilationEvent compilationEvent, AnalyzerActionCounts actionCounts)
            {
                lock (_gate)
                {
                    if (compilationEvent is SymbolDeclaredCompilationEvent symbolEvent)
                    {
                        var needsAnalysis = false;
                        var symbol = symbolEvent.Symbol;
                        var skipSymbolAnalysis = AnalysisScope.ShouldSkipSymbolAnalysis(symbolEvent);
                        if (!skipSymbolAnalysis && actionCounts.SymbolActionsCount > 0)
                        {
                            needsAnalysis = true;
                            _pendingSymbols[symbol] = null;
                        }

                        var skipDeclarationAnalysis = AnalysisScope.ShouldSkipDeclarationAnalysis(symbol);
                        if (!skipDeclarationAnalysis &&
                            actionCounts.HasAnyExecutableCodeActions)
                        {
                            needsAnalysis = true;
                            _pendingDeclarations[symbol] = null;
                        }

                        if (actionCounts.SymbolStartActionsCount > 0 && (!skipSymbolAnalysis || !skipDeclarationAnalysis))
                        {
                            needsAnalysis = true;
                            _lazyPendingSymbolEndAnalyses ??= new Dictionary<ISymbol, AnalyzerStateData?>();
                            _lazyPendingSymbolEndAnalyses[symbol] = null;
                        }

                        if (!needsAnalysis)
                        {
                            return;
                        }
                    }
                    else if (compilationEvent is CompilationStartedEvent compilationStartedEvent)
                    {
                        var fileCount = actionCounts.SyntaxTreeActionsCount > 0 ? compilationEvent.Compilation.SyntaxTrees.Count() : 0;
                        fileCount += actionCounts.AdditionalFileActionsCount > 0 ? compilationStartedEvent.AdditionalFiles.Length : 0;
                        if (fileCount > 0)
                        {
                            _lazyFilesWithAnalysisData = new Dictionary<SourceOrAdditionalFile, AnalyzerStateData>();
                            _pendingSyntaxAnalysisFilesCount = fileCount;
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

            public bool IsSymbolComplete(ISymbol symbol)
            {
                return IsEntityFullyProcessed(symbol, _pendingSymbols);
            }

            public bool IsSymbolEndAnalysisComplete(ISymbol symbol)
            {
                Debug.Assert(_lazyPendingSymbolEndAnalyses != null);
                return IsEntityFullyProcessed(symbol, _lazyPendingSymbolEndAnalyses);
            }

            public bool OnSymbolDeclaredEventProcessed(SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                lock (_gate)
                {
                    return OnSymbolDeclaredEventProcessed_NoLock(symbolDeclaredEvent);
                }
            }

            private bool OnSymbolDeclaredEventProcessed_NoLock(SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                // Check if the symbol event has been completely processed or not.

                // Have the symbol actions executed?
                if (!IsEntityFullyProcessed_NoLock(symbolDeclaredEvent.Symbol, _pendingSymbols))
                {
                    return false;
                }

                // Have the node/code block actions executed for all symbol declarations?
                if (!AreDeclarationsProcessed_NoLock(symbolDeclaredEvent.Symbol, symbolDeclaredEvent.DeclaringSyntaxReferences.Length))
                {
                    return false;
                }

                // Have the symbol end actions, if any, executed?
                if (_lazyPendingSymbolEndAnalyses != null && !IsEntityFullyProcessed_NoLock(symbolDeclaredEvent.Symbol, _lazyPendingSymbolEndAnalyses))
                {
                    return false;
                }

                // Mark declarations completely processed.
                MarkDeclarationsProcessed_NoLock(symbolDeclaredEvent.Symbol);

                // Mark the symbol event completely processed.
                return MarkEntityProcessed_NoLock(symbolDeclaredEvent, _pendingEvents, _analyzerStateDataPool);
            }
        }
    }
}
