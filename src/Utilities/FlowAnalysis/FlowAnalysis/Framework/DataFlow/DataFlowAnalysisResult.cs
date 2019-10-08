// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private readonly TAbstractAnalysisValue _defaultUnknownValue;
        private readonly object _analysisDataForUnhandledThrowOperationsOpt;

        internal DataFlowAnalysisResult(
            ImmutableDictionary<BasicBlock, TBlockAnalysisResult> basicBlockStateMap,
            ImmutableDictionary<IOperation, TAbstractAnalysisValue> operationStateMap,
            ImmutableDictionary<IOperation, PredicateValueKind> predicateValueKindMap,
            (TAbstractAnalysisValue, PredicateValueKind)? returnValueAndPredicateKindOpt,
            ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> interproceduralResultsMap,
            TBlockAnalysisResult entryBlockOutput,
            TBlockAnalysisResult exitBlockOutput,
            TBlockAnalysisResult exceptionPathsExitBlockOutputOpt,
            TBlockAnalysisResult mergedStateForUnhandledThrowOperationsOpt,
            object analysisDataForUnhandledThrowOperationsOpt,
            Dictionary<PointsToAbstractValue, TAbstractAnalysisValue> taskWrappedValuesMapOpt,
            ControlFlowGraph cfg,
            TAbstractAnalysisValue defaultUnknownValue)
        {
            Debug.Assert(basicBlockStateMap != null);
            Debug.Assert(operationStateMap != null);
            Debug.Assert(predicateValueKindMap != null);
            Debug.Assert(interproceduralResultsMap != null);
            Debug.Assert(entryBlockOutput != null);
            Debug.Assert(exitBlockOutput != null);

            _basicBlockStateMap = basicBlockStateMap;
            _operationStateMap = operationStateMap;
            _predicateValueKindMap = predicateValueKindMap;
            ReturnValueAndPredicateKindOpt = returnValueAndPredicateKindOpt;
            _interproceduralResultsMap = interproceduralResultsMap;
            EntryBlockOutput = entryBlockOutput;
            ExitBlockOutput = exitBlockOutput;
            ExceptionPathsExitBlockOutputOpt = exceptionPathsExitBlockOutputOpt;
            MergedStateForUnhandledThrowOperationsOpt = mergedStateForUnhandledThrowOperationsOpt;
            _analysisDataForUnhandledThrowOperationsOpt = analysisDataForUnhandledThrowOperationsOpt;
            TaskWrappedValuesMapOpt = taskWrappedValuesMapOpt;
            ControlFlowGraph = cfg;
            _defaultUnknownValue = defaultUnknownValue;
        }

        protected DataFlowAnalysisResult(DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> other)
            : this(other._basicBlockStateMap, other._operationStateMap, other._predicateValueKindMap, other.ReturnValueAndPredicateKindOpt,
                   other._interproceduralResultsMap, other.EntryBlockOutput, other.ExitBlockOutput, other.ExceptionPathsExitBlockOutputOpt,
                   other.MergedStateForUnhandledThrowOperationsOpt, other._analysisDataForUnhandledThrowOperationsOpt, other.TaskWrappedValuesMapOpt,
                   other.ControlFlowGraph, other._defaultUnknownValue)
        {
        }

        internal DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> With(
            TBlockAnalysisResult mergedStateForUnhandledThrowOperationsOpt,
            object analysisDataForUnhandledThrowOperations)
        {
            Debug.Assert(mergedStateForUnhandledThrowOperationsOpt != null);
            Debug.Assert(analysisDataForUnhandledThrowOperations != null);

            return new DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>(
                _basicBlockStateMap, _operationStateMap, _predicateValueKindMap, ReturnValueAndPredicateKindOpt,
                _interproceduralResultsMap, EntryBlockOutput, ExitBlockOutput, ExceptionPathsExitBlockOutputOpt, mergedStateForUnhandledThrowOperationsOpt,
                analysisDataForUnhandledThrowOperations, TaskWrappedValuesMapOpt, ControlFlowGraph, _defaultUnknownValue);
        }

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public TBlockAnalysisResult this[BasicBlock block] => _basicBlockStateMap[block];
        public TAbstractAnalysisValue this[IOperation operation]
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
        {
            get
            {
                if (_operationStateMap.TryGetValue(operation, out var value))
                {
                    return value;
                }

                // We were requested for value of an operation in non-method body context (e.g. initializer), which is currently not supported.
                // See https://github.com/dotnet/roslyn-analyzers/issues/1650 (Support for dataflow analysis for non-method body executable code)
                Debug.Assert(operation.GetAncestor<IBlockOperation>(OperationKind.Block, predicateOpt: b => b.Parent == null) == null);
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

        internal DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> TryGetInterproceduralResult(IOperation operation)
        {
            if (_interproceduralResultsMap.TryGetValue(operation, out var result))
            {
                return (DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>)result;
            }

            return null;
        }

        public ControlFlowGraph ControlFlowGraph { get; }
        public (TAbstractAnalysisValue Value, PredicateValueKind PredicateValueKind)? ReturnValueAndPredicateKindOpt { get; }
        public TBlockAnalysisResult EntryBlockOutput { get; }
        public TBlockAnalysisResult ExitBlockOutput { get; }
        public TBlockAnalysisResult ExceptionPathsExitBlockOutputOpt { get; }

        object IDataFlowAnalysisResult<TAbstractAnalysisValue>.AnalysisDataForUnhandledThrowOperationsOpt
            => _analysisDataForUnhandledThrowOperationsOpt;

        object IDataFlowAnalysisResult<TAbstractAnalysisValue>.TaskWrappedValuesMapOpt
            => TaskWrappedValuesMapOpt;

        public TBlockAnalysisResult MergedStateForUnhandledThrowOperationsOpt { get; }
        public PredicateValueKind GetPredicateKind(IOperation operation) => _predicateValueKindMap.TryGetValue(operation, out var valueKind) ? valueKind : PredicateValueKind.Unknown;
        internal Dictionary<PointsToAbstractValue, TAbstractAnalysisValue> TaskWrappedValuesMapOpt { get; }
    }
}
