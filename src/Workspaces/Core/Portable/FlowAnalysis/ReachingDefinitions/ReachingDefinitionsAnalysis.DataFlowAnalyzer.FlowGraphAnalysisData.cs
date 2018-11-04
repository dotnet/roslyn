// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.ReachingDefinitions
{
    internal static partial class ReachingDefinitionsAnalysis
    {
        private sealed partial class DataFlowAnalyzer : DataFlowAnalyzer<BasicBlockAnalysisData>
        {
            private sealed class FlowGraphAnalysisData : AnalysisData
            {
                private readonly ImmutableArray<IParameterSymbol> _parameters;

                /// <summary>
                /// Map from basic block to current <see cref="BasicBlockAnalysisData"/> for dataflow analysis.
                /// </summary>
                private readonly PooledDictionary<BasicBlock, BasicBlockAnalysisData> _reachingDefinitionsMap;

                /// <summary>
                /// Callback to analyze lambda/local function invocations and return new block analysis data.
                /// </summary>
                private readonly Func<IMethodSymbol, ControlFlowGraph, AnalysisData, CancellationToken, BasicBlockAnalysisData> _analyzeLocalFunctionOrLambdaInvocation;

                /// <summary>
                /// Map from flow capture ID to set of captured definition addresses along all possible control flow paths.
                /// </summary>
                private readonly PooledDictionary<CaptureId, PooledHashSet<(ISymbol, IOperation)>> _lValueFlowCapturesMap;

                /// <summary>
                /// Map from operations to potential delegate creation targets that could be invoked via delegate invocation
                /// on the operation.
                /// Used to analyze delegate creations/invocations of lambdas and local/functions defined in a method.
                /// </summary>
                private readonly PooledDictionary<IOperation, PooledHashSet<IOperation>> _reachingDelegateCreationTargets;

                /// <summary>
                /// Map from local functions to the <see cref="ControlFlowGraph"/> where the local function was accessed
                /// to create an invocable delegate. This control flow graph is required to lazily get or create the
                /// control flow graph for this local function at delegate invocation callsite.
                /// </summary>
                private readonly PooledDictionary<IMethodSymbol, ControlFlowGraph> _localFunctionTargetsToAccessingCfgMap;

                /// <summary>
                /// Map from lambdas to the <see cref="ControlFlowGraph"/> where the lambda was defined
                /// to create an invocable delegate. This control flow graph is required to lazily get or create the
                /// control flow graph for this lambda at delegate invocation callsite.
                /// </summary>
                private readonly PooledDictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph> _lambdaTargetsToAccessingCfgMap;

                /// <summary>
                /// Map from basic block range to set of definitions within this block range.
                /// Used for try-catch-finally analysis, where start of catch/finally blocks should
                /// consider all definitions in the corresponding try block as reachable.
                /// </summary>
                private readonly PooledDictionary<(int firstBlockOrdinal, int lastBlockOrdinal), PooledHashSet<(ISymbol, IOperation)>> _definitionsInsideBlockRangeMap;

                private FlowGraphAnalysisData(
                    ControlFlowGraph controlFlowGraph,
                    ImmutableArray<IParameterSymbol> parameters,
                    PooledDictionary<BasicBlock, BasicBlockAnalysisData> reachingDefinitionsMap,
                    PooledDictionary<(ISymbol symbol, IOperation operation), bool> definitionUsageMap,
                    PooledHashSet<ISymbol> symbolsRead,
                    PooledHashSet<IMethodSymbol> lambdaOrLocalFunctionsBeingAnalyzed,
                    Func<IMethodSymbol, ControlFlowGraph, AnalysisData, CancellationToken, BasicBlockAnalysisData> analyzeLocalFunctionOrLambdaInvocation,
                    PooledDictionary<IOperation, PooledHashSet<IOperation>> reachingDelegateCreationTargets,
                    PooledDictionary<IMethodSymbol, ControlFlowGraph> localFunctionTargetsToAccessingCfgMap,
                    PooledDictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph> lambdaTargetsToAccessingCfgMap)
                    : base(definitionUsageMap, symbolsRead, lambdaOrLocalFunctionsBeingAnalyzed)
                {
                    ControlFlowGraph = controlFlowGraph;
                    _parameters = parameters;
                    _reachingDefinitionsMap = reachingDefinitionsMap;
                    _analyzeLocalFunctionOrLambdaInvocation = analyzeLocalFunctionOrLambdaInvocation;
                    _reachingDelegateCreationTargets = reachingDelegateCreationTargets;
                    _localFunctionTargetsToAccessingCfgMap = localFunctionTargetsToAccessingCfgMap;
                    _lambdaTargetsToAccessingCfgMap = lambdaTargetsToAccessingCfgMap;

                    _lValueFlowCapturesMap = PooledDictionary<CaptureId, PooledHashSet<(ISymbol, IOperation)>>.GetInstance();
                    LValueFlowCapturesInGraph = LValueFlowCapturesProvider.GetOrCreateLValueFlowCaptures(controlFlowGraph);
                    _definitionsInsideBlockRangeMap = PooledDictionary<(int firstBlockOrdinal, int lastBlockOrdinal), PooledHashSet<(ISymbol, IOperation)>>.GetInstance();
                }

                public static FlowGraphAnalysisData Create(
                    ControlFlowGraph cfg,
                    ISymbol owningSymbol,
                    Func<IMethodSymbol, ControlFlowGraph, AnalysisData, CancellationToken, BasicBlockAnalysisData> analyzeLocalFunctionOrLambdaInvocation)
                {
                    Debug.Assert(cfg.Parent == null);

                    var parameters = owningSymbol.GetParameters();
                    return new FlowGraphAnalysisData(
                        cfg,
                        parameters,
                        reachingDefinitionsMap: CreateReachingDefinitionsMap(cfg),
                        definitionUsageMap: CreateDefinitionsUsageMap(parameters),
                        symbolsRead: PooledHashSet<ISymbol>.GetInstance(),
                        lambdaOrLocalFunctionsBeingAnalyzed: PooledHashSet<IMethodSymbol>.GetInstance(),
                        analyzeLocalFunctionOrLambdaInvocation,
                        reachingDelegateCreationTargets: PooledDictionary<IOperation, PooledHashSet<IOperation>>.GetInstance(),
                        localFunctionTargetsToAccessingCfgMap: PooledDictionary<IMethodSymbol, ControlFlowGraph>.GetInstance(),
                        lambdaTargetsToAccessingCfgMap: PooledDictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph>.GetInstance());
                }

                public static FlowGraphAnalysisData Create(
                    ControlFlowGraph cfg,
                    IMethodSymbol lambdaOrLocalFunction,
                    FlowGraphAnalysisData parentAnalysisData)
                {
                    Debug.Assert(cfg.Parent != null);
                    Debug.Assert(lambdaOrLocalFunction.IsAnonymousFunction() || lambdaOrLocalFunction.IsLocalFunction());
                    Debug.Assert(parentAnalysisData != null);

                    var parameters = lambdaOrLocalFunction.GetParameters();
                    return new FlowGraphAnalysisData(
                        cfg,
                        parameters,
                        reachingDefinitionsMap: CreateReachingDefinitionsMap(cfg),
                        definitionUsageMap: UpdateDefinitionsUsageMap(parentAnalysisData.DefinitionUsageMapBuilder, parameters),
                        symbolsRead: parentAnalysisData.SymbolsReadBuilder,
                        lambdaOrLocalFunctionsBeingAnalyzed: parentAnalysisData.LambdaOrLocalFunctionsBeingAnalyzed,
                        analyzeLocalFunctionOrLambdaInvocation: parentAnalysisData._analyzeLocalFunctionOrLambdaInvocation,
                        reachingDelegateCreationTargets: parentAnalysisData._reachingDelegateCreationTargets,
                        localFunctionTargetsToAccessingCfgMap: parentAnalysisData._localFunctionTargetsToAccessingCfgMap,
                        lambdaTargetsToAccessingCfgMap: parentAnalysisData._lambdaTargetsToAccessingCfgMap);
                }

                private static PooledDictionary<BasicBlock, BasicBlockAnalysisData> CreateReachingDefinitionsMap(
                    ControlFlowGraph cfg)
                {
                    var builder = PooledDictionary<BasicBlock, BasicBlockAnalysisData>.GetInstance();
                    foreach (var block in cfg.Blocks)
                    {
                        builder.Add(block, null);
                    }

                    return builder;
                }

                public ControlFlowGraph ControlFlowGraph { get; }

                /// <summary>
                /// Flow captures for l-value or address captures.
                /// </summary>
                public ImmutableHashSet<CaptureId> LValueFlowCapturesInGraph { get; }

                public BasicBlockAnalysisData GetCurrentBlockAnalysisData(BasicBlock basicBlock)
                    => _reachingDefinitionsMap[basicBlock];

                public BasicBlockAnalysisData GetOrCreateBlockAnalysisData(BasicBlock basicBlock)
                {
                    if (_reachingDefinitionsMap[basicBlock] == null)
                    {
                        _reachingDefinitionsMap[basicBlock] = CreateBlockAnalysisData();
                    }

                    HandleCatchOrFilterOrFinallyInitialization(basicBlock);
                    return _reachingDefinitionsMap[basicBlock];
                }

                private PooledHashSet<(ISymbol, IOperation)> GetOrCreateDefinitionsInBlockRange(int firstBlockOrdinal, int lastBlockOrdinal)
                {
                    if (!_definitionsInsideBlockRangeMap.TryGetValue((firstBlockOrdinal, lastBlockOrdinal), out var definitionsInBlockRange))
                    {
                        // Compute all descendant operations in basic block range.
                        var operations = PooledHashSet<IOperation>.GetInstance();
                        for (int i = firstBlockOrdinal; i <= lastBlockOrdinal; i++)
                        {
                            foreach (var operation in ControlFlowGraph.Blocks[i].DescendantOperations())
                            {
                                operations.Add(operation);
                            }
                        }

                        // Filter down the operations to definitions (writes) within this block range.
                        definitionsInBlockRange = PooledHashSet<(ISymbol, IOperation)>.GetInstance();
                        foreach (var (symbol, definition) in DefinitionUsageMapBuilder.Where(kvp => !kvp.Value).Select(kvp => kvp.Key).ToArray())
                        {
                            if (definition != null && operations.Contains(definition))
                            {
                                definitionsInBlockRange.Add((symbol, definition));
                            }
                        }
                    }

                    return definitionsInBlockRange;
                }

                /// <summary>
                /// Special handling to ensure that at start of catch/filter/finally region analysis,
                /// we mark all definitions from the corresponding try region as reachable in the
                /// catch/filter/finally region.
                /// </summary>
                /// <param name="basicBlock"></param>
                private void HandleCatchOrFilterOrFinallyInitialization(BasicBlock basicBlock)
                {
                    Debug.Assert(_reachingDefinitionsMap[basicBlock] != null);

                    // Ensure we are processing a basic block with following properties:
                    //  1. It has no predecessors
                    //  2. It is not the entry block
                    //  3. It is the first block of its enclosing region.
                    if (!basicBlock.Predecessors.IsEmpty ||
                        basicBlock.Kind == BasicBlockKind.Entry ||
                        basicBlock.EnclosingRegion.FirstBlockOrdinal != basicBlock.Ordinal)
                    {
                        return;
                    }

                    // Find the outermost region for which this block is the first block.
                    var outermostEnclosingRegionStartingBlock = basicBlock.EnclosingRegion;
                    while (outermostEnclosingRegionStartingBlock.EnclosingRegion?.FirstBlockOrdinal == basicBlock.Ordinal)
                    {
                        outermostEnclosingRegionStartingBlock = outermostEnclosingRegionStartingBlock.EnclosingRegion;
                    }

                    // Check if we are at start of catch or filter or finally.
                    switch (outermostEnclosingRegionStartingBlock.Kind)
                    {
                        case ControlFlowRegionKind.Catch:
                        case ControlFlowRegionKind.Filter:
                        case ControlFlowRegionKind.FilterAndHandler:
                        case ControlFlowRegionKind.Finally:
                            break;

                        default:
                            return;
                    }

                    // Find the outer try/catch or try/finally for this region.
                    ControlFlowRegion containingTryCatchFinallyRegion = null;
                    var currentRegion = outermostEnclosingRegionStartingBlock;
                    do
                    {
                        switch (currentRegion.Kind)
                        {
                            case ControlFlowRegionKind.TryAndCatch:
                            case ControlFlowRegionKind.TryAndFinally:
                                containingTryCatchFinallyRegion = currentRegion;
                                break;
                        }

                        currentRegion = currentRegion.EnclosingRegion;
                    }
                    while (containingTryCatchFinallyRegion == null);

                    // All definitions reachable at start of try region are considered reachable at start of catch/finally region.
                    var firstBasicBlockInOutermostRegion = ControlFlowGraph.Blocks[containingTryCatchFinallyRegion.FirstBlockOrdinal];
                    var mergedAnalysisData = _reachingDefinitionsMap[basicBlock];
                    mergedAnalysisData.SetAnalysisDataFrom(GetCurrentBlockAnalysisData(firstBasicBlockInOutermostRegion));

                    // All definitions within the try region are considered reachable at start of catch/finally region.
                    foreach (var (symbol, definition) in GetOrCreateDefinitionsInBlockRange(containingTryCatchFinallyRegion.FirstBlockOrdinal, basicBlock.Ordinal - 1))
                    {
                        mergedAnalysisData.OnWriteReferenceFound(symbol, definition, maybeWritten: true);
                        DefinitionUsageMapBuilder[(symbol, definition)] = true;
                        SymbolsReadBuilder.Add(symbol);
                    }

                    SetBlockAnalysisData(basicBlock, mergedAnalysisData);
                }

                public void SetCurrentBlockAnalysisDataFrom(BasicBlock basicBlock)
                    => SetCurrentBlockAnalysisDataFrom(GetOrCreateBlockAnalysisData(basicBlock));

                public void SetAnalysisDataOnEntryBlockStart()
                {
                    foreach (var parameter in _parameters)
                    {
                        DefinitionUsageMapBuilder[(parameter, null)] = false;
                        CurrentBlockAnalysisData.OnWriteReferenceFound(parameter, operation: null, maybeWritten: false);
                    }
                }

                public void SetBlockAnalysisData(BasicBlock basicBlock, BasicBlockAnalysisData data)
                    => _reachingDefinitionsMap[basicBlock] = data;

                public void SetBlockAnalysisDataFrom(BasicBlock basicBlock, BasicBlockAnalysisData data)
                    => GetOrCreateBlockAnalysisData(basicBlock).SetAnalysisDataFrom(data);

                public void SetAnalysisDataOnExitBlockEnd()
                {
                    if (DefinitionUsageMapBuilder.Count == 0)
                    {
                        return;
                    }

                    // Mark all reachable definitions for ref/out parameters at end of exit block as used.
                    foreach (var parameter in _parameters)
                    {
                        if (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out)
                        {
                            var currentDefinitions = CurrentBlockAnalysisData.GetCurrentDefinitions(parameter);
                            foreach (var definition in currentDefinitions)
                            {
                                if (definition != null)
                                {
                                    DefinitionUsageMapBuilder[(parameter, definition)] = true;
                                }
                            }
                        }
                    }
                }

                public override bool IsLValueFlowCapture(CaptureId captureId)
                    => LValueFlowCapturesInGraph.Contains(captureId);

                public override void OnLValueCaptureFound(ISymbol symbol, IOperation operation, CaptureId captureId)
                {
                    if (!_lValueFlowCapturesMap.TryGetValue(captureId, out var captures))
                    {
                        captures = PooledHashSet<(ISymbol, IOperation)>.GetInstance();
                        _lValueFlowCapturesMap.Add(captureId, captures);
                    }

                    captures.Add((symbol, operation));
                }

                public override void OnLValueDereferenceFound(CaptureId captureId)
                {
                    if (_lValueFlowCapturesMap.TryGetValue(captureId, out var captures))
                    {
                        var mayBeWritten = captures.Count > 1;
                        foreach (var (symbol, definition) in captures)
                        {
                            OnWriteReferenceFound(symbol, definition, mayBeWritten);
                        }
                    }
                }

                protected override BasicBlockAnalysisData AnalyzeLocalFunctionInvocationCore(IMethodSymbol localFunction, CancellationToken cancellationToken)
                {
                    Debug.Assert(localFunction.IsLocalFunction());

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!_localFunctionTargetsToAccessingCfgMap.TryGetValue(localFunction, out var accessingCfg))
                    {
                        accessingCfg = ControlFlowGraph;
                    }

                    var localFunctionCfg = accessingCfg.GetLocalFunctionControlFlowGraphInScope(localFunction, cancellationToken);
                    return _analyzeLocalFunctionOrLambdaInvocation(localFunction, localFunctionCfg, this, cancellationToken);
                }

                protected override BasicBlockAnalysisData AnalyzeLambdaInvocationCore(IFlowAnonymousFunctionOperation lambda, CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!_lambdaTargetsToAccessingCfgMap.TryGetValue(lambda, out var accessingCfg))
                    {
                        accessingCfg = ControlFlowGraph;
                    }

                    var lambdaCfg = accessingCfg.GetAnonymousFunctionControlFlowGraphInScope(lambda, cancellationToken);
                    return _analyzeLocalFunctionOrLambdaInvocation(lambda.Symbol, lambdaCfg, this, cancellationToken);
                }

                public override bool IsTrackingDelegateCreationTargets => true;
                public override void SetTargetsFromSymbolForDelegate(IOperation definition, ISymbol symbol)
                {
                    // Transfer reaching delegate creation targets when assigning from a local/parameter symbol
                    // that has known set of potential delegate creation targets. For example, this method will be called
                    // for definition 'y' from symbol 'x' for below code:
                    //      Action x = () => { };
                    //      Action y = x;

                    var targetsBuilder = PooledHashSet<IOperation>.GetInstance();
                    foreach (var symbolDefinition in CurrentBlockAnalysisData.GetCurrentDefinitions(symbol))
                    {
                        if (symbolDefinition == null)
                        {
                            continue;
                        }

                        if (!_reachingDelegateCreationTargets.TryGetValue(symbolDefinition, out var targetsBuilderForDefinition))
                        {
                            // Unable to find delegate creation targets for this symbol definition.
                            // Bail out without setting targets.
                            targetsBuilder.Free();
                            return;
                        }
                        else
                        {
                            foreach (var target in targetsBuilderForDefinition)
                            {
                                targetsBuilder.Add(target);
                            }
                        }
                    }

                    _reachingDelegateCreationTargets[definition] = targetsBuilder;
                }

                public override void SetLambdaTargetForDelegate(IOperation definition, IFlowAnonymousFunctionOperation lambdaTarget)
                {
                    // Sets a lambda delegate target for the current definition.
                    // For example, this method will be called for the definition 'x' below with assigned lambda.
                    //      Action x = () => { };

                    SetReachingDelegateTargetCore(definition, lambdaTarget);
                    _lambdaTargetsToAccessingCfgMap[lambdaTarget] = ControlFlowGraph;
                }

                public override void SetLocalFunctionTargetForDelegate(IOperation definition, IMethodReferenceOperation localFunctionTarget)
                {
                    // Sets a local function delegate target for the current definition.
                    // For example, this method will be called for the definition 'x' below with assigned LocalFunction delegate.
                    //      Action x = LocalFunction;
                    //      void LocalFunction() { }

                    Debug.Assert(localFunctionTarget.Method.IsLocalFunction());
                    SetReachingDelegateTargetCore(definition, localFunctionTarget);
                    _localFunctionTargetsToAccessingCfgMap[localFunctionTarget.Method] = ControlFlowGraph;
                }

                public override void SetEmptyInvocationTargetsForDelegate(IOperation definition)
                    => SetReachingDelegateTargetCore(definition, targetOpt: null);

                private void SetReachingDelegateTargetCore(IOperation definition, IOperation targetOpt)
                {
                    var targetsBuilder = PooledHashSet<IOperation>.GetInstance();
                    if (targetOpt != null)
                    {
                        targetsBuilder.Add(targetOpt);
                    }
                    _reachingDelegateCreationTargets[definition] = targetsBuilder;
                }

                public override bool TryGetDelegateInvocationTargets(IOperation definition, out ImmutableHashSet<IOperation> targets)
                {
                    // Attempts to return potential lamba/local function delegate invocation targets for the given definition.
                    if (_reachingDelegateCreationTargets.TryGetValue(definition, out var targetsBuilder))
                    {
                        targets = targetsBuilder.ToImmutableHashSet();
                        return true;
                    }

                    targets = ImmutableHashSet<IOperation>.Empty;
                    return false;
                }

                protected override void DisposeCoreData()
                {
                    // We share the base analysis data structures between primary method's flow graph analysis
                    // and it's inner lambda/local function flow graph analysis.
                    // Dispose the base data structures only for primary method's flow analysis data.
                    if (ControlFlowGraph.Parent == null)
                    {
                        DisposeForNonLocalFunctionOrLambdaAnalsis();
                    }

                    DisposeCommon();
                    return;

                    // Local functions.
                    void DisposeForNonLocalFunctionOrLambdaAnalsis()
                    {
                        base.DisposeCoreData();

                        foreach (var creations in _reachingDelegateCreationTargets.Values)
                        {
                            creations.Free();
                        }
                        _reachingDelegateCreationTargets.Free();

                        _localFunctionTargetsToAccessingCfgMap.Free();
                        _lambdaTargetsToAccessingCfgMap.Free();
                    }

                    void DisposeCommon()
                    {
                        // Note the base type already disposes the BasicBlockAnalysisData values
                        // allocated by us, so we only need to free the map.
                        _reachingDefinitionsMap.Free();

                        foreach (var captures in _lValueFlowCapturesMap.Values)
                        {
                            captures.Free();
                        }
                        _lValueFlowCapturesMap.Free();

                        foreach (var operations in _definitionsInsideBlockRangeMap.Values)
                        {
                            operations.Free();
                        }
                        _definitionsInsideBlockRangeMap.Free();
                    }
                }
            }
        }
    }
}
