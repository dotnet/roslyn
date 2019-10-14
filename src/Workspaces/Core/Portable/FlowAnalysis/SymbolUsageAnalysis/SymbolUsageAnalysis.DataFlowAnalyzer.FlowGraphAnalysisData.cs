// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis
{
    internal static partial class SymbolUsageAnalysis
    {
        private sealed partial class DataFlowAnalyzer : DataFlowAnalyzer<BasicBlockAnalysisData>
        {
            private sealed class FlowGraphAnalysisData : AnalysisData
            {
                private readonly ImmutableArray<IParameterSymbol> _parameters;

                /// <summary>
                /// Map from basic block to current <see cref="BasicBlockAnalysisData"/> for dataflow analysis.
                /// </summary>
                private readonly PooledDictionary<BasicBlock, BasicBlockAnalysisData> _analysisDataByBasicBlockMap;

                /// <summary>
                /// Callback to analyze lambda/local function invocations and return new block analysis data.
                /// </summary>
                private readonly Func<IMethodSymbol, ControlFlowGraph, AnalysisData, CancellationToken, BasicBlockAnalysisData> _analyzeLocalFunctionOrLambdaInvocation;

                /// <summary>
                /// Map from flow capture ID to set of captured symbol addresses along all possible control flow paths.
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
                /// Map from basic block range to set of writes within this block range.
                /// Used for try-catch-finally analysis, where start of catch/finally blocks should
                /// consider all writes in the corresponding try block as reachable.
                /// </summary>
                private readonly PooledDictionary<(int firstBlockOrdinal, int lastBlockOrdinal), PooledHashSet<(ISymbol, IOperation)>> _symbolWritesInsideBlockRangeMap;

                private FlowGraphAnalysisData(
                    ControlFlowGraph controlFlowGraph,
                    ImmutableArray<IParameterSymbol> parameters,
                    PooledDictionary<BasicBlock, BasicBlockAnalysisData> analysisDataByBasicBlockMap,
                    PooledDictionary<(ISymbol symbol, IOperation operation), bool> symbolsWriteMap,
                    PooledHashSet<ISymbol> symbolsRead,
                    PooledHashSet<IMethodSymbol> lambdaOrLocalFunctionsBeingAnalyzed,
                    Func<IMethodSymbol, ControlFlowGraph, AnalysisData, CancellationToken, BasicBlockAnalysisData> analyzeLocalFunctionOrLambdaInvocation,
                    PooledDictionary<IOperation, PooledHashSet<IOperation>> reachingDelegateCreationTargets,
                    PooledDictionary<IMethodSymbol, ControlFlowGraph> localFunctionTargetsToAccessingCfgMap,
                    PooledDictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph> lambdaTargetsToAccessingCfgMap)
                    : base(symbolsWriteMap, symbolsRead, lambdaOrLocalFunctionsBeingAnalyzed)
                {
                    ControlFlowGraph = controlFlowGraph;
                    _parameters = parameters;
                    _analysisDataByBasicBlockMap = analysisDataByBasicBlockMap;
                    _analyzeLocalFunctionOrLambdaInvocation = analyzeLocalFunctionOrLambdaInvocation;
                    _reachingDelegateCreationTargets = reachingDelegateCreationTargets;
                    _localFunctionTargetsToAccessingCfgMap = localFunctionTargetsToAccessingCfgMap;
                    _lambdaTargetsToAccessingCfgMap = lambdaTargetsToAccessingCfgMap;

                    _lValueFlowCapturesMap = PooledDictionary<CaptureId, PooledHashSet<(ISymbol, IOperation)>>.GetInstance();
                    LValueFlowCapturesInGraph = LValueFlowCapturesProvider.CreateLValueFlowCaptures(controlFlowGraph);
                    Debug.Assert(LValueFlowCapturesInGraph.Values.All(kind => kind == FlowCaptureKind.LValueCapture || kind == FlowCaptureKind.LValueAndRValueCapture));

                    _symbolWritesInsideBlockRangeMap = PooledDictionary<(int firstBlockOrdinal, int lastBlockOrdinal), PooledHashSet<(ISymbol, IOperation)>>.GetInstance();
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
                        analysisDataByBasicBlockMap: CreateAnalysisDataByBasicBlockMap(cfg),
                        symbolsWriteMap: CreateSymbolsWriteMap(parameters),
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
                        analysisDataByBasicBlockMap: CreateAnalysisDataByBasicBlockMap(cfg),
                        symbolsWriteMap: UpdateSymbolsWriteMap(parentAnalysisData.SymbolsWriteBuilder, parameters),
                        symbolsRead: parentAnalysisData.SymbolsReadBuilder,
                        lambdaOrLocalFunctionsBeingAnalyzed: parentAnalysisData.LambdaOrLocalFunctionsBeingAnalyzed,
                        analyzeLocalFunctionOrLambdaInvocation: parentAnalysisData._analyzeLocalFunctionOrLambdaInvocation,
                        reachingDelegateCreationTargets: parentAnalysisData._reachingDelegateCreationTargets,
                        localFunctionTargetsToAccessingCfgMap: parentAnalysisData._localFunctionTargetsToAccessingCfgMap,
                        lambdaTargetsToAccessingCfgMap: parentAnalysisData._lambdaTargetsToAccessingCfgMap);
                }

                private static PooledDictionary<BasicBlock, BasicBlockAnalysisData> CreateAnalysisDataByBasicBlockMap(
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
                public ImmutableDictionary<CaptureId, FlowCaptureKind> LValueFlowCapturesInGraph { get; }

                public BasicBlockAnalysisData GetBlockAnalysisData(BasicBlock basicBlock)
                    => _analysisDataByBasicBlockMap[basicBlock];

                public BasicBlockAnalysisData GetOrCreateBlockAnalysisData(BasicBlock basicBlock, CancellationToken cancellationToken)
                {
                    if (_analysisDataByBasicBlockMap[basicBlock] == null)
                    {
                        _analysisDataByBasicBlockMap[basicBlock] = CreateBlockAnalysisData();
                    }

                    HandleCatchOrFilterOrFinallyInitialization(basicBlock, cancellationToken);
                    return _analysisDataByBasicBlockMap[basicBlock];
                }

                private PooledHashSet<(ISymbol, IOperation)> GetOrCreateSymbolWritesInBlockRange(int firstBlockOrdinal, int lastBlockOrdinal, CancellationToken cancellationToken)
                {
                    if (!_symbolWritesInsideBlockRangeMap.TryGetValue((firstBlockOrdinal, lastBlockOrdinal), out var writesInBlockRange))
                    {
                        // Compute all descendant operations in basic block range.
                        var operations = PooledHashSet<IOperation>.GetInstance();
                        AddDescendantOperationsInRange(ControlFlowGraph, firstBlockOrdinal, lastBlockOrdinal, operations, cancellationToken);

                        // Filter down the operations to writes within this block range.
                        writesInBlockRange = PooledHashSet<(ISymbol, IOperation)>.GetInstance();
                        foreach (var (symbol, write) in SymbolsWriteBuilder.Where(kvp => !kvp.Value).Select(kvp => kvp.Key).ToArray())
                        {
                            if (write != null && operations.Contains(write))
                            {
                                writesInBlockRange.Add((symbol, write));
                            }
                        }
                    }

                    return writesInBlockRange;
                }

                private void AddDescendantOperationsInRange(
                    ControlFlowGraph cfg,
                    int firstBlockOrdinal,
                    int lastBlockOrdinal,
                    PooledHashSet<IOperation> operationsBuilder,
                    CancellationToken cancellationToken)
                {
                    // Compute all descendant operations in basic block range.
                    for (var i = firstBlockOrdinal; i <= lastBlockOrdinal; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (var operation in cfg.Blocks[i].DescendantOperations())
                        {
                            var added = operationsBuilder.Add(operation);
                            if (added && operation is IInvocationOperation invocation)
                            {
                                if (invocation.Instance != null &&
                                    _reachingDelegateCreationTargets.TryGetValue(invocation.Instance, out var targets))
                                {
                                    AddDescendantOperationsFromDelegateCreationTargets(targets);
                                }
                                else if (invocation.TargetMethod.IsLocalFunction())
                                {
                                    var localFunctionGraph = cfg.GetLocalFunctionControlFlowGraphInScope(invocation.TargetMethod.OriginalDefinition);
                                    if (localFunctionGraph != null)
                                    {
                                        AddDescendantOperationsInLambdaOrLocalFunctionGraph(localFunctionGraph);
                                    }
                                }
                            }
                        }
                    }

                    return;

                    // Local functions.
                    void AddDescendantOperationsFromDelegateCreationTargets(PooledHashSet<IOperation> targets)
                    {
                        foreach (var target in targets)
                        {
                            ControlFlowGraph lambdaOrLocalFunctionCfgOpt = null;
                            switch (target)
                            {
                                case IFlowAnonymousFunctionOperation flowAnonymousFunctionOperation:
                                    lambdaOrLocalFunctionCfgOpt = TryGetAnonymousFunctionControlFlowGraphInScope(flowAnonymousFunctionOperation);
                                    break;

                                case ILocalFunctionOperation localFunctionOperation:
                                    lambdaOrLocalFunctionCfgOpt = TryGetLocalFunctionControlFlowGraphInScope(localFunctionOperation.Symbol);
                                    break;

                                case IMethodReferenceOperation methodReferenceOperation when (methodReferenceOperation.Method.IsLocalFunction()):
                                    lambdaOrLocalFunctionCfgOpt = TryGetLocalFunctionControlFlowGraphInScope(methodReferenceOperation.Method);
                                    break;
                            }

                            if (lambdaOrLocalFunctionCfgOpt != null &&
                                operationsBuilder.Add(target))
                            {
                                AddDescendantOperationsInLambdaOrLocalFunctionGraph(lambdaOrLocalFunctionCfgOpt);
                            }
                        }
                    }

                    void AddDescendantOperationsInLambdaOrLocalFunctionGraph(ControlFlowGraph lambdaOrLocalFunctionCfg)
                    {
                        Debug.Assert(lambdaOrLocalFunctionCfg != null);
                        AddDescendantOperationsInRange(lambdaOrLocalFunctionCfg, firstBlockOrdinal: 0,
                            lastBlockOrdinal: lambdaOrLocalFunctionCfg.Blocks.Length - 1, operationsBuilder, cancellationToken);
                    }

                    ControlFlowGraph TryGetAnonymousFunctionControlFlowGraphInScope(IFlowAnonymousFunctionOperation flowAnonymousFunctionOperation)
                    {
                        if (_lambdaTargetsToAccessingCfgMap.TryGetValue(flowAnonymousFunctionOperation, out var lambdaAccessingCfg))
                        {
                            var anonymousFunctionCfg = lambdaAccessingCfg.GetAnonymousFunctionControlFlowGraphInScope(flowAnonymousFunctionOperation);
                            Debug.Assert(anonymousFunctionCfg != null);
                            return anonymousFunctionCfg;
                        }

                        return null;
                    }

                    ControlFlowGraph TryGetLocalFunctionControlFlowGraphInScope(IMethodSymbol localFunction)
                    {
                        Debug.Assert(localFunction.IsLocalFunction());

                        // Use the original definition of the local function for flow analysis.
                        localFunction = localFunction.OriginalDefinition;

                        if (_localFunctionTargetsToAccessingCfgMap.TryGetValue(localFunction, out var localFunctionAccessingCfg))
                        {
                            var localFunctionCfg = localFunctionAccessingCfg.GetLocalFunctionControlFlowGraphInScope(localFunction);
                            Debug.Assert(localFunctionCfg != null);
                            return localFunctionCfg;
                        }

                        return null;
                    }
                }

                /// <summary>
                /// Special handling to ensure that at start of catch/filter/finally region analysis,
                /// we mark all symbol writes from the corresponding try region as reachable in the
                /// catch/filter/finally region.
                /// </summary>
                /// <param name="basicBlock"></param>
                private void HandleCatchOrFilterOrFinallyInitialization(BasicBlock basicBlock, CancellationToken cancellationToken)
                {
                    Debug.Assert(_analysisDataByBasicBlockMap[basicBlock] != null);

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

                    // All symbol writes reachable at start of try region are considered reachable at start of catch/finally region.
                    var firstBasicBlockInOutermostRegion = ControlFlowGraph.Blocks[containingTryCatchFinallyRegion.FirstBlockOrdinal];
                    var mergedAnalysisData = _analysisDataByBasicBlockMap[basicBlock];
                    mergedAnalysisData.SetAnalysisDataFrom(GetBlockAnalysisData(firstBasicBlockInOutermostRegion));

                    // All symbol writes within the try region are considered reachable at start of catch/finally region.
                    foreach (var (symbol, write) in GetOrCreateSymbolWritesInBlockRange(containingTryCatchFinallyRegion.FirstBlockOrdinal, basicBlock.Ordinal - 1, cancellationToken))
                    {
                        mergedAnalysisData.OnWriteReferenceFound(symbol, write, maybeWritten: true);
                        SymbolsWriteBuilder[(symbol, write)] = true;
                        SymbolsReadBuilder.Add(symbol);
                    }

                    SetBlockAnalysisData(basicBlock, mergedAnalysisData);
                }

                public void SetCurrentBlockAnalysisDataFrom(BasicBlock basicBlock, CancellationToken cancellationToken)
                    => SetCurrentBlockAnalysisDataFrom(GetOrCreateBlockAnalysisData(basicBlock, cancellationToken));

                public void SetAnalysisDataOnEntryBlockStart()
                {
                    foreach (var parameter in _parameters)
                    {
                        SymbolsWriteBuilder[(parameter, null)] = false;
                        CurrentBlockAnalysisData.OnWriteReferenceFound(parameter, operation: null, maybeWritten: false);
                    }
                }

                public void SetBlockAnalysisData(BasicBlock basicBlock, BasicBlockAnalysisData data)
                    => _analysisDataByBasicBlockMap[basicBlock] = data;

                public void SetBlockAnalysisDataFrom(BasicBlock basicBlock, BasicBlockAnalysisData data, CancellationToken cancellationToken)
                    => GetOrCreateBlockAnalysisData(basicBlock, cancellationToken).SetAnalysisDataFrom(data);

                public void SetAnalysisDataOnMethodExit()
                {
                    if (SymbolsWriteBuilder.Count == 0)
                    {
                        return;
                    }

                    // Mark all reachable definitions for ref/out parameters at end of exit block as used.
                    foreach (var parameter in _parameters)
                    {
                        if (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out)
                        {
                            var currentWrites = CurrentBlockAnalysisData.GetCurrentWrites(parameter);
                            foreach (var write in currentWrites)
                            {
                                if (write != null)
                                {
                                    SymbolsWriteBuilder[(parameter, write)] = true;
                                }
                            }
                        }
                    }
                }

                public override bool IsLValueFlowCapture(CaptureId captureId)
                    => LValueFlowCapturesInGraph.ContainsKey(captureId);

                public override bool IsRValueFlowCapture(CaptureId captureId)
                    => !LValueFlowCapturesInGraph.TryGetValue(captureId, out var captureKind) || captureKind != FlowCaptureKind.LValueCapture;

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
                        foreach (var (symbol, write) in captures)
                        {
                            OnWriteReferenceFound(symbol, write, mayBeWritten);
                        }
                    }
                }

                protected override BasicBlockAnalysisData AnalyzeLocalFunctionInvocationCore(IMethodSymbol localFunction, CancellationToken cancellationToken)
                {
                    Debug.Assert(localFunction.IsLocalFunction());
                    Debug.Assert(localFunction.Equals(localFunction.OriginalDefinition));

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

                public override void SetTargetsFromSymbolForDelegate(IOperation write, ISymbol symbol)
                {
                    // Transfer reaching delegate creation targets when assigning from a local/parameter symbol
                    // that has known set of potential delegate creation targets. For example, this method will be called
                    // for definition 'y' from symbol 'x' for below code:
                    //      Action x = () => { };
                    //      Action y = x;
                    //
                    var targetsBuilder = PooledHashSet<IOperation>.GetInstance();
                    foreach (var symbolWrite in CurrentBlockAnalysisData.GetCurrentWrites(symbol))
                    {
                        if (symbolWrite == null)
                        {
                            continue;
                        }

                        if (!_reachingDelegateCreationTargets.TryGetValue(symbolWrite, out var targetsBuilderForSymbolWrite))
                        {
                            // Unable to find delegate creation targets for this symbol write.
                            // Bail out without setting targets.
                            targetsBuilder.Free();
                            return;
                        }
                        else
                        {
                            foreach (var target in targetsBuilderForSymbolWrite)
                            {
                                targetsBuilder.Add(target);
                            }
                        }
                    }

                    _reachingDelegateCreationTargets[write] = targetsBuilder;
                }

                public override void SetLambdaTargetForDelegate(IOperation write, IFlowAnonymousFunctionOperation lambdaTarget)
                {
                    // Sets a lambda delegate target for the current write.
                    // For example, this method will be called for the definition 'x' below with assigned lambda.
                    //      Action x = () => { };
                    //
                    SetReachingDelegateTargetCore(write, lambdaTarget);
                    _lambdaTargetsToAccessingCfgMap[lambdaTarget] = ControlFlowGraph;
                }

                public override void SetLocalFunctionTargetForDelegate(IOperation write, IMethodReferenceOperation localFunctionTarget)
                {
                    // Sets a local function delegate target for the current write.
                    // For example, this method will be called for the definition 'x' below with assigned LocalFunction delegate.
                    //      Action x = LocalFunction;
                    //      void LocalFunction() { }
                    //
                    Debug.Assert(localFunctionTarget.Method.IsLocalFunction());
                    SetReachingDelegateTargetCore(write, localFunctionTarget);
                    _localFunctionTargetsToAccessingCfgMap[localFunctionTarget.Method.OriginalDefinition] = ControlFlowGraph;
                }

                public override void SetEmptyInvocationTargetsForDelegate(IOperation write)
                    => SetReachingDelegateTargetCore(write, targetOpt: null);

                private void SetReachingDelegateTargetCore(IOperation write, IOperation targetOpt)
                {
                    var targetsBuilder = PooledHashSet<IOperation>.GetInstance();
                    if (targetOpt != null)
                    {
                        targetsBuilder.Add(targetOpt);
                    }

                    _reachingDelegateCreationTargets[write] = targetsBuilder;
                }

                public override bool TryGetDelegateInvocationTargets(IOperation write, out ImmutableHashSet<IOperation> targets)
                {
                    // Attempts to return potential lamba/local function delegate invocation targets for the given write.
                    if (_reachingDelegateCreationTargets.TryGetValue(write, out var targetsBuilder))
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
                        DisposeForNonLocalFunctionOrLambdaAnalysis();
                    }

                    DisposeCommon();
                    return;

                    // Local functions.
                    void DisposeForNonLocalFunctionOrLambdaAnalysis()
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
                        _analysisDataByBasicBlockMap.Free();

                        foreach (var captures in _lValueFlowCapturesMap.Values)
                        {
                            captures.Free();
                        }

                        _lValueFlowCapturesMap.Free();

                        foreach (var operations in _symbolWritesInsideBlockRangeMap.Values)
                        {
                            operations.Free();
                        }

                        _symbolWritesInsideBlockRangeMap.Free();
                    }
                }
            }
        }
    }
}
