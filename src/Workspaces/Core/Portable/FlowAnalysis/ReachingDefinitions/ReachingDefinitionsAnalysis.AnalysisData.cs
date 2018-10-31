// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.ReachingDefinitions
{
    internal static partial class ReachingDefinitionsAnalysis
    {
        /// <summary>
        /// Core analysis data to drive the reaching definitions operation <see cref="Walker"/>
        /// for operation tree OR control flow graph based reaching definition analysis.
        /// </summary>
        private abstract class AnalysisData : IDisposable
        {
            /// <summary>
            /// Pooled <see cref="BasicBlockAnalysisData"/> allocated during analysis with the
            /// current <see cref="AnalysisData"/> instance, which will be freed during <see cref="Dispose"/>.
            /// </summary>
            private readonly ArrayBuilder<BasicBlockAnalysisData> _allocatedBasicBlockAnalysisDatas;
            
            protected AnalysisData(
                PooledDictionary<(ISymbol symbol, IOperation operation), bool> definitionUsageMap,
                PooledHashSet<ISymbol> symbolsRead,
                PooledHashSet<IMethodSymbol> lambdaOrLocalFunctionsBeingAnalyzed)
            {
                DefinitionUsageMapBuilder = definitionUsageMap;
                SymbolsReadBuilder = symbolsRead;
                LambdaOrLocalFunctionsBeingAnalyzed = lambdaOrLocalFunctionsBeingAnalyzed;

                _allocatedBasicBlockAnalysisDatas = ArrayBuilder<BasicBlockAnalysisData>.GetInstance();
                CurrentBlockAnalysisData = CreateBlockAnalysisData();
            }

            /// <summary>
            /// Map from each symbol definition to a boolean indicating if the value assinged
            /// at definition is used/read on some control flow path.
            /// </summary>
            protected PooledDictionary<(ISymbol symbol, IOperation operation), bool> DefinitionUsageMapBuilder { get; }

            /// <summary>
            /// Set of locals/parameters that have at least one use/read for one of its definitions.
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
            /// Creates an immutable <see cref="DefinitionUsageResult"/> for the current analysis data.
            /// </summary>
            public DefinitionUsageResult ToResult()
                => new DefinitionUsageResult(DefinitionUsageMapBuilder.ToImmutableDictionary(),
                                             SymbolsReadBuilder.ToImmutableHashSet());

            public BasicBlockAnalysisData AnalyzeLocalFunctionInvocation(IMethodSymbol localFunction, CancellationToken cancellationToken)
            {
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
            public abstract void OnLValueCaptureFound(ISymbol symbol, IOperation operation, CaptureId captureId);
            public abstract void OnLValueDereferenceFound(CaptureId captureId);

            // Methods specific to delegate analysis to track potential delegate invocation targets for CFG based dataflow analysis.
            public abstract bool IsTrackingDelegateCreationTargets { get; }
            public abstract void SetTargetsFromSymbolForDelegate(IOperation definition, ISymbol symbol);
            public abstract void SetLambdaTargetForDelegate(IOperation definition, IFlowAnonymousFunctionOperation lambdaTarget);
            public abstract void SetLocalFunctionTargetForDelegate(IOperation definition, IMethodReferenceOperation localFunctionTarget);
            public abstract void SetEmptyInvocationTargetsForDelegate(IOperation definition);
            public abstract bool TryGetDelegateInvocationTargets(IOperation definition, out ImmutableHashSet<IOperation> targets);

            protected static PooledDictionary<(ISymbol Symbol, IOperation Definition), bool> CreateDefinitionsUsageMap(
                ImmutableArray<IParameterSymbol> parameters)
            {
                var definitionUsageMap = PooledDictionary<(ISymbol Symbol, IOperation Definition), bool>.GetInstance();
                return UpdateDefinitionsUsageMap(definitionUsageMap, parameters);
            }

            protected static PooledDictionary<(ISymbol Symbol, IOperation Definition), bool> UpdateDefinitionsUsageMap(
                PooledDictionary<(ISymbol Symbol, IOperation Definition), bool> definitionUsageMap,
                ImmutableArray<IParameterSymbol> parameters)
            {
                foreach (var parameter in parameters)
                {
                    (ISymbol, IOperation) key = (parameter, null);
                    if (!definitionUsageMap.ContainsKey(key))
                    {
                        definitionUsageMap.Add(key, false);
                    }
                }

                return definitionUsageMap;
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

                // Mark all the current reaching definitions of symbol as used/read.
                if (DefinitionUsageMapBuilder.Count != 0)
                {
                    var currentDefinitions = CurrentBlockAnalysisData.GetCurrentDefinitions(symbol);
                    foreach (var definition in currentDefinitions)
                    {
                        DefinitionUsageMapBuilder[(symbol, definition)] = true;
                    }
                }

                // Mark the current symbol as used/read.
                SymbolsReadBuilder.Add(symbol);
            }

            public void OnWriteReferenceFound(ISymbol symbol, IOperation operation, bool maybeWritten)
            {
                var definition = (symbol, operation);
                if (symbol.Kind == SymbolKind.Discard)
                {
                    // Skip discard symbols and also for already processed writes (back edge from loops).
                    return;
                }

                // Add a new definition (write) for the given symbol at the given operation.
                CurrentBlockAnalysisData.OnWriteReferenceFound(symbol, operation, maybeWritten);

                // Only mark as unused definition if we are processing it for the first time (not from back edge for loops)
                if (!DefinitionUsageMapBuilder.ContainsKey(definition) &&
                    !maybeWritten)
                {
                    DefinitionUsageMapBuilder.Add((symbol, operation), false);
                }
            }

            /// <summary>
            /// Resets all the currently tracked symbol definitions to be conservatively marked as used.
            /// </summary>
            public void ResetState()
            {
                foreach (var symbol in DefinitionUsageMapBuilder.Keys.Select(d => d.symbol).ToArray())
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
                DefinitionUsageMapBuilder.Free();
                SymbolsReadBuilder.Free();
                LambdaOrLocalFunctionsBeingAnalyzed.Free();
            }

            protected void DisposeAllocatedBasicBlockAnalysisData()
            {
                foreach (var instance in _allocatedBasicBlockAnalysisDatas)
                {
                    instance.Free();
                }

                _allocatedBasicBlockAnalysisDatas.Free();
            }
        }
    }
}
