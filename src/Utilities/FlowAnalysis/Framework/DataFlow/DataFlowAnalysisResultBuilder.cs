// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Used by <see cref="DataFlowAnalysis"/> to store intermediate dataflow results while executing data flow analysis
    /// and also to compute the final <see cref="DataFlowAnalysisResult{TAnalysisResult, TAbstractAnalysisValue}"/> exposed as the result.
    /// </summary>
    internal class DataFlowAnalysisResultBuilder<TAnalysisData>
        where TAnalysisData: class
    {
        private readonly IDictionary<BasicBlock, DataFlowAnalysisInfo<TAnalysisData>> _info;

        public DataFlowAnalysisResultBuilder()
        {
            _info = new Dictionary<BasicBlock, DataFlowAnalysisInfo<TAnalysisData>>();
        }

        public DataFlowAnalysisInfo<TAnalysisData> this[BasicBlock block] => _info[block];

        internal void Add(BasicBlock block)
        {
            _info.Add(block, new DataFlowAnalysisInfo<TAnalysisData>(null, null));
        }

        internal void Update(BasicBlock block, DataFlowAnalysisInfo<TAnalysisData> newData)
        {
            _info[block] = newData;
        }

        public DataFlowAnalysisResult<TAnalysisResult, TAbstractAnalysisValue> ToResult<TAnalysisResult, TAbstractAnalysisValue>(
            Func<BasicBlock, DataFlowAnalysisInfo<TAnalysisData>, TAnalysisResult> getResult,
            ImmutableDictionary<IOperation, TAbstractAnalysisValue> stateMap,
            ImmutableDictionary<IOperation, PredicateValueKind> predicateValueKindMap,
            TAnalysisData mergedDataForUnhandledThrowOperations,
            ControlFlowGraph cfg,
            TAbstractAnalysisValue defaultUnknownValue)
            where TAnalysisResult: class
        {
            var resultBuilder = ImmutableDictionary.CreateBuilder<BasicBlock, TAnalysisResult>();
            foreach (var kvp in _info)
            {
                var block = kvp.Key;
                var blockAnalysisData = kvp.Value;
                var result = getResult(block, blockAnalysisData);
                resultBuilder.Add(block, result);
            }

            TAnalysisResult mergedStateForUnhandledThrowOperations = null;
            if (mergedDataForUnhandledThrowOperations != null)
            {
                var info = new DataFlowAnalysisInfo<TAnalysisData>(mergedDataForUnhandledThrowOperations, mergedDataForUnhandledThrowOperations);
                mergedStateForUnhandledThrowOperations = getResult(cfg.GetExit(), info);
            }

            return new DataFlowAnalysisResult<TAnalysisResult, TAbstractAnalysisValue>(resultBuilder.ToImmutable(), stateMap,
                predicateValueKindMap, mergedStateForUnhandledThrowOperations, cfg, defaultUnknownValue);
        }
    }
}
