// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Result from execution of a <see cref="DataFlowAnalysis"/> on a control flow graph.
    /// It stores:
    ///  (1) Analysis values for all operations in the graph and
    ///  (2) <see cref="AbstractBlockAnalysisResult"/> for every basic block in the graph.
    ///  (3) Merged analysis state for all the unhandled throw operations in the graph.
    /// </summary>
    public class DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> : IDataFlowAnalysisResult<TAbstractAnalysisValue>
        where TBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        private readonly ImmutableDictionary<BasicBlock, TBlockAnalysisResult> _basicBlockStateMap;
        private readonly ImmutableDictionary<IOperation, TAbstractAnalysisValue> _operationStateMap;
        private readonly ImmutableDictionary<IOperation, PredicateValueKind> _predicateValueKindMap;
        private readonly ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> _interproceduralResultsMap;
        private readonly ImmutableDictionary<IMethodSymbol, IDataFlowAnalysisResult<TAbstractAnalysisValue>> _standaloneLocalFunctionAnalysisResultsMap;
        private readonly TAbstractAnalysisValue _defaultUnknownValue;
        private readonly object? _analysisDataForUnhandledThrowOperations;

        internal DataFlowAnalysisResult(
            ImmutableDictionary<BasicBlock, TBlockAnalysisResult> basicBlockStateMap,
            ImmutableDictionary<IOperation, TAbstractAnalysisValue> operationStateMap,
            ImmutableDictionary<IOperation, PredicateValueKind> predicateValueKindMap,
            (TAbstractAnalysisValue, PredicateValueKind)? returnValueAndPredicateKind,
            ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> interproceduralResultsMap,
            ImmutableDictionary<IMethodSymbol, IDataFlowAnalysisResult<TAbstractAnalysisValue>> standaloneLocalFunctionAnalysisResultsMap,
            LambdaAndLocalFunctionAnalysisInfo lambdaAndLocalFunctionAnalysisInfo,
            TBlockAnalysisResult entryBlockOutput,
            TBlockAnalysisResult exitBlockOutput,
            TBlockAnalysisResult? exceptionPathsExitBlockOutput,
            TBlockAnalysisResult? mergedStateForUnhandledThrowOperations,
            object? analysisDataForUnhandledThrowOperations,
            Dictionary<PointsToAbstractValue, TAbstractAnalysisValue>? taskWrappedValuesMap,
            ControlFlowGraph cfg,
            TAbstractAnalysisValue defaultUnknownValue)
        {
            _basicBlockStateMap = basicBlockStateMap;
            _operationStateMap = operationStateMap;
            _predicateValueKindMap = predicateValueKindMap;
            ReturnValueAndPredicateKind = returnValueAndPredicateKind;
            _interproceduralResultsMap = interproceduralResultsMap;
            _standaloneLocalFunctionAnalysisResultsMap = standaloneLocalFunctionAnalysisResultsMap;
            LambdaAndLocalFunctionAnalysisInfo = lambdaAndLocalFunctionAnalysisInfo;
            EntryBlockOutput = entryBlockOutput;
            ExitBlockOutput = exitBlockOutput;
            ExceptionPathsExitBlockOutput = exceptionPathsExitBlockOutput;
            MergedStateForUnhandledThrowOperations = mergedStateForUnhandledThrowOperations;
            _analysisDataForUnhandledThrowOperations = analysisDataForUnhandledThrowOperations;
            TaskWrappedValuesMap = taskWrappedValuesMap;
            ControlFlowGraph = cfg;
            _defaultUnknownValue = defaultUnknownValue;
        }

        protected DataFlowAnalysisResult(DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> other)
            : this(other._basicBlockStateMap, other._operationStateMap, other._predicateValueKindMap, other.ReturnValueAndPredicateKind,
                   other._interproceduralResultsMap, other._standaloneLocalFunctionAnalysisResultsMap, other.LambdaAndLocalFunctionAnalysisInfo,
                   other.EntryBlockOutput, other.ExitBlockOutput, other.ExceptionPathsExitBlockOutput,
                   other.MergedStateForUnhandledThrowOperations, other._analysisDataForUnhandledThrowOperations, other.TaskWrappedValuesMap,
                   other.ControlFlowGraph, other._defaultUnknownValue)
        {
        }

        internal DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> With(
            TBlockAnalysisResult mergedStateForUnhandledThrowOperationsOpt,
            object analysisDataForUnhandledThrowOperations)
        {
            return new DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>(
                _basicBlockStateMap, _operationStateMap, _predicateValueKindMap, ReturnValueAndPredicateKind,
                _interproceduralResultsMap, _standaloneLocalFunctionAnalysisResultsMap, LambdaAndLocalFunctionAnalysisInfo,
                EntryBlockOutput, ExitBlockOutput, ExceptionPathsExitBlockOutput, mergedStateForUnhandledThrowOperationsOpt,
                analysisDataForUnhandledThrowOperations, TaskWrappedValuesMap, ControlFlowGraph, _defaultUnknownValue);
        }

        internal DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> With(ImmutableDictionary<IOperation, TAbstractAnalysisValue> operationStateMap)
        {
            return new DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>(
                _basicBlockStateMap, operationStateMap, _predicateValueKindMap, ReturnValueAndPredicateKind,
                _interproceduralResultsMap, _standaloneLocalFunctionAnalysisResultsMap, LambdaAndLocalFunctionAnalysisInfo,
                EntryBlockOutput, ExitBlockOutput, ExceptionPathsExitBlockOutput, MergedStateForUnhandledThrowOperations,
                _analysisDataForUnhandledThrowOperations, TaskWrappedValuesMap, ControlFlowGraph, _defaultUnknownValue);
        }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public TBlockAnalysisResult this[BasicBlock block] => _basicBlockStateMap[block];
        public TAbstractAnalysisValue this[IOperation operation]
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
        {
            get
            {
                // This accessor is only meant for use by the DFA analysis for operations within the CFG,
                // which have a completely different operation tree.
                // Make sure the analyzers don't invoke this accessor with operations from the original operation tree
                // They should instead by invoking the accessor 'this[OperationKind operationKind, SyntaxNode syntax]'
                // with operation's kind and syntax as arguments.
                Debug.Assert(operation.GetRoot() != ControlFlowGraph.OriginalOperation,
                    "Did you mean to invoke the accessor that takes operation's kind and syntax as arguments?");

                if (_operationStateMap.TryGetValue(operation, out var value))
                {
                    return value;
                }

                // We were requested for value of an operation in non-method body context (e.g. initializer), which is currently not supported.
                // See https://github.com/dotnet/roslyn-analyzers/issues/1650 (Support for dataflow analysis for non-method body executable code)
                Debug.Assert(operation.GetAncestor<IBlockOperation>(OperationKind.Block, predicate: b => b.Parent == null) == null);
                return _defaultUnknownValue;
            }
        }

        public TAbstractAnalysisValue this[OperationKind operationKind, SyntaxNode syntax]
        {
            get
            {
                var value = _defaultUnknownValue;
                foreach (var kvp in _operationStateMap)
                {
                    if (kvp.Key.Kind == operationKind && kvp.Key.Syntax == syntax)
                    {
                        if (!kvp.Key.IsImplicit)
                        {
                            return kvp.Value;
                        }
                        else
                        {
                            value = kvp.Value;
                        }
                    }
                }

                return value;
            }
        }

        internal DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>? TryGetInterproceduralResult(IOperation operation)
        {
            if (_interproceduralResultsMap.TryGetValue(operation, out var result))
            {
                return (DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>)result;
            }

            return null;
        }

        internal DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>? TryGetStandaloneLocalFunctionAnalysisResult(IMethodSymbol localFunction)
        {
            Debug.Assert(localFunction.MethodKind == MethodKind.LocalFunction);
            if (_standaloneLocalFunctionAnalysisResultsMap.TryGetValue(localFunction, out var result))
            {
                return (DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>)result;
            }

            return null;
        }

        internal IEnumerable<DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>> TryGetLambdaOrLocalFunctionResults(IOperation lambdaOrLocalFunctionOperation)
        {
            Debug.Assert(lambdaOrLocalFunctionOperation.Kind is OperationKind.AnonymousFunction or OperationKind.LocalFunction);

            var isNestedLambdaOrLocalFunction = lambdaOrLocalFunctionOperation.IsWithinLambdaOrLocalFunction(out _);

            foreach (DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> result in _interproceduralResultsMap.Values.Cast<DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>>())
            {
                if (result.ControlFlowGraph.OriginalOperation == lambdaOrLocalFunctionOperation)
                {
                    yield return result;
                }
                else if (isNestedLambdaOrLocalFunction)
                {
                    foreach (var nestedResult in result.TryGetLambdaOrLocalFunctionResults(lambdaOrLocalFunctionOperation))
                    {
                        yield return nestedResult;
                    }
                }
            }

            if (lambdaOrLocalFunctionOperation.Kind == OperationKind.LocalFunction)
            {
                foreach (DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> result in _standaloneLocalFunctionAnalysisResultsMap.Values.Cast<DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>>())
                {
                    if (result.ControlFlowGraph.OriginalOperation == lambdaOrLocalFunctionOperation)
                    {
                        yield return result;
                    }
                    else if (isNestedLambdaOrLocalFunction)
                    {
                        foreach (var nestedResult in result.TryGetLambdaOrLocalFunctionResults(lambdaOrLocalFunctionOperation))
                        {
                            yield return nestedResult;
                        }
                    }
                }
            }
        }

        public ControlFlowGraph ControlFlowGraph { get; }
        public (TAbstractAnalysisValue Value, PredicateValueKind PredicateValueKind)? ReturnValueAndPredicateKind { get; }
        public TBlockAnalysisResult EntryBlockOutput { get; }
        public TBlockAnalysisResult ExitBlockOutput { get; }
        public TBlockAnalysisResult? ExceptionPathsExitBlockOutput { get; }
        public LambdaAndLocalFunctionAnalysisInfo LambdaAndLocalFunctionAnalysisInfo { get; }

        object? IDataFlowAnalysisResult<TAbstractAnalysisValue>.AnalysisDataForUnhandledThrowOperations
            => _analysisDataForUnhandledThrowOperations;

        object? IDataFlowAnalysisResult<TAbstractAnalysisValue>.TaskWrappedValuesMap
            => TaskWrappedValuesMap;

        public TBlockAnalysisResult? MergedStateForUnhandledThrowOperations { get; }
        public PredicateValueKind GetPredicateKind(IOperation operation) => _predicateValueKindMap.TryGetValue(operation, out var valueKind) ? valueKind : PredicateValueKind.Unknown;
        internal Dictionary<PointsToAbstractValue, TAbstractAnalysisValue>? TaskWrappedValuesMap { get; }
    }
}
