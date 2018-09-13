// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
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
    internal class DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> : IDataFlowAnalysisResult<TAbstractAnalysisValue>
        where TBlockAnalysisResult: AbstractBlockAnalysisResult
    {
        private readonly ImmutableDictionary<BasicBlock, TBlockAnalysisResult> _basicBlockStateMap;
        private readonly ImmutableDictionary<IOperation, TAbstractAnalysisValue> _operationStateMap;
        private readonly ImmutableDictionary<IOperation, PredicateValueKind> _predicateValueKindMap;
        private readonly ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> _interproceduralResultsMap;
        private readonly TAbstractAnalysisValue _defaultUnknownValue;

        public DataFlowAnalysisResult(
            ImmutableDictionary<BasicBlock, TBlockAnalysisResult> basicBlockStateMap,
            ImmutableDictionary<IOperation, TAbstractAnalysisValue> operationStateMap,
            ImmutableDictionary<IOperation, PredicateValueKind> predicateValueKindMap,
            (TAbstractAnalysisValue, PredicateValueKind)? returnValueAndPredicateKindOpt,
            ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> interproceduralResultsMap,
            TBlockAnalysisResult mergedStateForUnhandledThrowOperationsOpt,
            ControlFlowGraph cfg,
            TAbstractAnalysisValue defaultUnknownValue)
        {
            Debug.Assert(basicBlockStateMap != null);
            Debug.Assert(operationStateMap != null);
            Debug.Assert(predicateValueKindMap != null);
            Debug.Assert(interproceduralResultsMap != null);

            _basicBlockStateMap = basicBlockStateMap;
            _operationStateMap = operationStateMap;
            _predicateValueKindMap = predicateValueKindMap;
            ReturnValueAndPredicateKindOpt = returnValueAndPredicateKindOpt;
            _interproceduralResultsMap = interproceduralResultsMap;
            MergedStateForUnhandledThrowOperationsOpt = mergedStateForUnhandledThrowOperationsOpt;
            ControlFlowGraph = cfg;
            _defaultUnknownValue = defaultUnknownValue;
        }

        protected DataFlowAnalysisResult(DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> other)
            : this(other._basicBlockStateMap, other._operationStateMap, other._predicateValueKindMap, other.ReturnValueAndPredicateKindOpt,
                   other._interproceduralResultsMap, other.MergedStateForUnhandledThrowOperationsOpt, other.ControlFlowGraph, other._defaultUnknownValue)
        {
        }

        public TBlockAnalysisResult this[BasicBlock block] => _basicBlockStateMap[block];
        public TAbstractAnalysisValue this[IOperation operation]
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

        internal DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> GetInterproceduralResult(IOperation operation)
            => (DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>)_interproceduralResultsMap[operation];

        public ControlFlowGraph ControlFlowGraph { get; }
        public (TAbstractAnalysisValue Value, PredicateValueKind PredicateValueKind)? ReturnValueAndPredicateKindOpt { get; }
        public TBlockAnalysisResult MergedStateForUnhandledThrowOperationsOpt { get; }
        public PredicateValueKind GetPredicateKind(IOperation operation) => _predicateValueKindMap.TryGetValue(operation, out var valueKind) ? valueKind : PredicateValueKind.Unknown;
    }
}
