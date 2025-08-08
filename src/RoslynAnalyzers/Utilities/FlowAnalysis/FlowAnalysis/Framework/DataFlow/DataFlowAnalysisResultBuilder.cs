// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Used by <see cref="DataFlowAnalysis"/> to store intermediate dataflow results while executing data flow analysis
    /// and also to compute the final <see cref="DataFlowAnalysisResult{TAnalysisResult, TAbstractAnalysisValue}"/> exposed as the result.
    /// </summary>
    internal sealed class DataFlowAnalysisResultBuilder<TAnalysisData> : IDisposable
        where TAnalysisData : AbstractAnalysisData
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly PooledDictionary<BasicBlock, TAnalysisData?> _info;
#pragma warning restore

        public DataFlowAnalysisResultBuilder()
        {
            _info = PooledDictionary<BasicBlock, TAnalysisData?>.GetInstance();
        }

        public TAnalysisData? this[BasicBlock block] => _info[block];
        public TAnalysisData? EntryBlockOutputData { get; set; }
        public TAnalysisData? ExitBlockOutputData { get; set; }

        internal void Add(BasicBlock block)
        {
            _info.Add(block, null);
        }

        internal void Update(BasicBlock block, TAnalysisData newData)
        {
            _info[block] = newData;
        }

        public DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> ToResult<TBlockAnalysisResult, TAbstractAnalysisValue>(
            Func<BasicBlock, TAnalysisData, TBlockAnalysisResult> getBlockResult,
            ImmutableDictionary<IOperation, TAbstractAnalysisValue> stateMap,
            ImmutableDictionary<IOperation, PredicateValueKind> predicateValueKindMap,
            (TAbstractAnalysisValue, PredicateValueKind)? returnValueAndPredicateKind,
            ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> interproceduralResultsMap,
            ImmutableDictionary<IMethodSymbol, IDataFlowAnalysisResult<TAbstractAnalysisValue>> standaloneLocalFunctionAnalysisResultsMap,
            LambdaAndLocalFunctionAnalysisInfo lambdaAndLocalFunctionAnalysisInfo,
            TAnalysisData entryBlockOutputData,
            TAnalysisData exitBlockData,
            TAnalysisData? exceptionPathsExitBlockData,
            TAnalysisData? mergedDataForUnhandledThrowOperations,
            Dictionary<ThrownExceptionInfo, TAnalysisData>? analysisDataForUnhandledThrowOperations,
            Dictionary<PointsToAbstractValue, TAbstractAnalysisValue>? taskWrappedValuesMap,
            ControlFlowGraph cfg,
            TAbstractAnalysisValue defaultUnknownValue)
            where TBlockAnalysisResult : AbstractBlockAnalysisResult
        {
            var resultBuilder = PooledDictionary<BasicBlock, TBlockAnalysisResult>.GetInstance();
            foreach (var kvp in _info)
            {
                var block = kvp.Key;
                var blockAnalysisData = kvp.Value;
                var result = getBlockResult(block, blockAnalysisData!);
                resultBuilder.Add(block, result);
            }

            var mergedStateForUnhandledThrowOperations = mergedDataForUnhandledThrowOperations != null ?
                getBlockResult(cfg.GetExit(), mergedDataForUnhandledThrowOperations) :
                null;

            var entryBlockOutputResult = getBlockResult(cfg.GetEntry(), entryBlockOutputData);
            var exitBlockOutputResult = getBlockResult(cfg.GetExit(), exitBlockData);
            var exceptionPathsExitBlockOutputResult = exceptionPathsExitBlockData != null ?
                getBlockResult(cfg.GetExit(), exceptionPathsExitBlockData) :
                null;

            return new DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>(resultBuilder.ToImmutableDictionaryAndFree(), stateMap,
                predicateValueKindMap, returnValueAndPredicateKind, interproceduralResultsMap,
                standaloneLocalFunctionAnalysisResultsMap, lambdaAndLocalFunctionAnalysisInfo,
                entryBlockOutputResult, exitBlockOutputResult, exceptionPathsExitBlockOutputResult,
                mergedStateForUnhandledThrowOperations, analysisDataForUnhandledThrowOperations,
                taskWrappedValuesMap, cfg, defaultUnknownValue);
        }

        public void Dispose()
        {
            _info.Values.Dispose();
            _info.Free();
        }
    }
}
