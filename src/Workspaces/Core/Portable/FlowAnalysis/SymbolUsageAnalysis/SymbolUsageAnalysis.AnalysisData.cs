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
        /// <summary>
        /// Core analysis data to drive the operation <see cref="Walker"/>
        /// for operation tree based analysis OR control flow graph based analysis.
        /// </summary>
        private abstract class AnalysisData : IDisposable
        {
            /// <summary>
            /// Pooled <see cref="BasicBlockAnalysisData"/> allocated during analysis with the
            /// current <see cref="AnalysisData"/> instance, which will be freed during <see cref="Dispose"/>.
            /// </summary>
            private readonly ArrayBuilder<BasicBlockAnalysisData> _allocatedBasicBlockAnalysisDatas;

            protected AnalysisData(
                PooledDictionary<(ISymbol symbol, IOperation operation), bool> symbolWriteBuilder,
                PooledHashSet<ISymbol> symbolsRead,
                PooledHashSet<IMethodSymbol> lambdaOrLocalFunctionsBeingAnalyzed)
            {
                SymbolsWriteBuilder = symbolWriteBuilder;
                SymbolsReadBuilder = symbolsRead;
                LambdaOrLocalFunctionsBeingAnalyzed = lambdaOrLocalFunctionsBeingAnalyzed;

                _allocatedBasicBlockAnalysisDatas = ArrayBuilder<BasicBlockAnalysisData>.GetInstance();
                CurrentBlockAnalysisData = CreateBlockAnalysisData();
            }

            /// <summary>
            /// Map from each (symbol, write) to a boolean indicating if the value assigned
            /// at the write is read on some control flow path.
            /// For example, consider the following code:
            /// <code>
            ///     int x = 0;
            ///     x = 1;
            ///     Console.WriteLine(x);
            /// </code>
            /// This map will have two entries for 'x':
            ///     1. Key = (symbol: x, write: 'int x = 0')
            ///        Value = 'false', because value assigned to 'x' here **is never** read. 
            ///     2. Key = (symbol: x, write: 'x = 1')
            ///        Value = 'true', because value assigned to 'x' here **may be** read on
            ///        some control flow path.
            /// </summary>
            protected PooledDictionary<(ISymbol symbol, IOperation operation), bool> SymbolsWriteBuilder { get; }

            /// <summary>
            /// Set of locals/parameters that are read at least once.
            /// </summary>
            protected PooledHashSet<ISymbol> SymbolsReadBuilder { get; }

            /// <summary>
            /// Set of lambda/local functions whose invocations are currently being analyzed to prevent
            /// infinite recursion for analyzing code with recursive lambda/local function calls.
            /// </summary>
            protected PooledHashSet<IMethodSymbol> LambdaOrLocalFunctionsBeingAnalyzed { get; }

            /// <summary>
            /// Current block analysis data used for analysis.
            /// </summary>
            public BasicBlockAnalysisData CurrentBlockAnalysisData { get; }

            /// <summary>
            /// Creates an immutable <see cref="SymbolUsageResult"/> for the current analysis data.
            /// </summary>
            public SymbolUsageResult ToResult()
                => new SymbolUsageResult(SymbolsWriteBuilder.ToImmutableDictionary(),
                                         SymbolsReadBuilder.ToImmutableHashSet());

            public BasicBlockAnalysisData AnalyzeLocalFunctionInvocation(IMethodSymbol localFunction, CancellationToken cancellationToken)
            {
                Debug.Assert(localFunction.IsLocalFunction());

                // Use the original definition of the local function for flow analysis.
                localFunction = localFunction.OriginalDefinition;

                if (!LambdaOrLocalFunctionsBeingAnalyzed.Add(localFunction))
                {
                    ResetState();
                    return CurrentBlockAnalysisData;
                }
                else
                {
                    var result = AnalyzeLocalFunctionInvocationCore(localFunction, cancellationToken);
                    LambdaOrLocalFunctionsBeingAnalyzed.Remove(localFunction);
                    return result;
                }
            }

            public BasicBlockAnalysisData AnalyzeLambdaInvocation(IFlowAnonymousFunctionOperation lambda, CancellationToken cancellationToken)
            {
                if (!LambdaOrLocalFunctionsBeingAnalyzed.Add(lambda.Symbol))
                {
                    ResetState();
                    return CurrentBlockAnalysisData;
                }
                else
                {
                    var result = AnalyzeLambdaInvocationCore(lambda, cancellationToken);
                    LambdaOrLocalFunctionsBeingAnalyzed.Remove(lambda.Symbol);
                    return result;
                }
            }

            protected abstract BasicBlockAnalysisData AnalyzeLocalFunctionInvocationCore(IMethodSymbol localFunction, CancellationToken cancellationToken);
            protected abstract BasicBlockAnalysisData AnalyzeLambdaInvocationCore(IFlowAnonymousFunctionOperation lambda, CancellationToken cancellationToken);

            // Methods specific to flow capture analysis for CFG based dataflow analysis.
            public abstract bool IsLValueFlowCapture(CaptureId captureId);
            public abstract bool IsRValueFlowCapture(CaptureId captureId);
            public abstract void OnLValueCaptureFound(ISymbol symbol, IOperation operation, CaptureId captureId);
            public abstract void OnLValueDereferenceFound(CaptureId captureId);

            // Methods specific to delegate analysis to track potential delegate invocation targets for CFG based dataflow analysis.
            public abstract bool IsTrackingDelegateCreationTargets { get; }
            public abstract void SetTargetsFromSymbolForDelegate(IOperation write, ISymbol symbol);
            public abstract void SetLambdaTargetForDelegate(IOperation write, IFlowAnonymousFunctionOperation lambdaTarget);
            public abstract void SetLocalFunctionTargetForDelegate(IOperation write, IMethodReferenceOperation localFunctionTarget);
            public abstract void SetEmptyInvocationTargetsForDelegate(IOperation write);
            public abstract bool TryGetDelegateInvocationTargets(IOperation write, out ImmutableHashSet<IOperation> targets);

            protected static PooledDictionary<(ISymbol Symbol, IOperation Write), bool> CreateSymbolsWriteMap(
                ImmutableArray<IParameterSymbol> parameters)
            {
                var symbolsWriteMap = PooledDictionary<(ISymbol Symbol, IOperation Write), bool>.GetInstance();
                return UpdateSymbolsWriteMap(symbolsWriteMap, parameters);
            }

            protected static PooledDictionary<(ISymbol Symbol, IOperation Write), bool> UpdateSymbolsWriteMap(
                PooledDictionary<(ISymbol Symbol, IOperation Write), bool> symbolsWriteMap,
                ImmutableArray<IParameterSymbol> parameters)
            {
                // Mark parameters as being written from the value provided at the call site.
                // Note that the write operation is "null" as there is no corresponding IOperation for parameter definition.
                foreach (var parameter in parameters)
                {
                    (ISymbol, IOperation) key = (parameter, null);
                    if (!symbolsWriteMap.ContainsKey(key))
                    {
                        symbolsWriteMap.Add(key, false);
                    }
                }

                return symbolsWriteMap;
            }

            public BasicBlockAnalysisData CreateBlockAnalysisData()
            {
                var instance = BasicBlockAnalysisData.GetInstance();
                _allocatedBasicBlockAnalysisDatas.Add(instance);
                return instance;
            }

            public void OnReadReferenceFound(ISymbol symbol)
            {
                if (symbol.Kind == SymbolKind.Discard)
                {
                    return;
                }

                // Mark all the current reaching writes of symbol as read.
                if (SymbolsWriteBuilder.Count != 0)
                {
                    var currentWrites = CurrentBlockAnalysisData.GetCurrentWrites(symbol);
                    foreach (var write in currentWrites)
                    {
                        SymbolsWriteBuilder[(symbol, write)] = true;
                    }
                }

                // Mark the current symbol as read.
                SymbolsReadBuilder.Add(symbol);
            }

            public void OnWriteReferenceFound(ISymbol symbol, IOperation operation, bool maybeWritten)
            {
                var symbolAndWrite = (symbol, operation);
                if (symbol.Kind == SymbolKind.Discard)
                {
                    // Skip discard symbols and also for already processed writes (back edge from loops).
                    return;
                }

                // Add a new write for the given symbol at the given operation.
                CurrentBlockAnalysisData.OnWriteReferenceFound(symbol, operation, maybeWritten);

                // Only mark as unused write if we are processing it for the first time (not from back edge for loops)
                if (!SymbolsWriteBuilder.ContainsKey(symbolAndWrite) &&
                    !maybeWritten)
                {
                    SymbolsWriteBuilder.Add((symbol, operation), false);
                }
            }

            /// <summary>
            /// Resets all the currently tracked symbol writes to be conservatively marked as read.
            /// </summary>
            public void ResetState()
            {
                foreach (var symbol in SymbolsWriteBuilder.Keys.Select(d => d.symbol).ToArray())
                {
                    OnReadReferenceFound(symbol);
                }
            }

            public void SetCurrentBlockAnalysisDataFrom(BasicBlockAnalysisData newBlockAnalysisData)
            {
                Debug.Assert(newBlockAnalysisData != null);
                CurrentBlockAnalysisData.SetAnalysisDataFrom(newBlockAnalysisData);
            }

            public void Dispose()
            {
                DisposeAllocatedBasicBlockAnalysisData();
                DisposeCoreData();
            }

            protected virtual void DisposeCoreData()
            {
                SymbolsWriteBuilder.Free();
                SymbolsReadBuilder.Free();
                LambdaOrLocalFunctionsBeingAnalyzed.Free();
            }

            protected void DisposeAllocatedBasicBlockAnalysisData()
            {
                foreach (var instance in _allocatedBasicBlockAnalysisDatas)
                {
                    instance.Dispose();
                }

                _allocatedBasicBlockAnalysisDatas.Free();
            }
        }
    }
}
