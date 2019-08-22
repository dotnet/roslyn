// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis
{
    internal static partial class SymbolUsageAnalysis
    {
        /// <summary>
        /// Dataflow analysis to compute symbol usage information (i.e. reads/writes) for locals/parameters
        /// in a given control flow graph, along with the information of whether or not the writes
        /// may be read on some control flow path.
        /// </summary>
        private sealed partial class DataFlowAnalyzer : DataFlowAnalyzer<BasicBlockAnalysisData>
        {
            private readonly FlowGraphAnalysisData _analysisData;

            private DataFlowAnalyzer(ControlFlowGraph cfg, ISymbol owningSymbol)
            {
                _analysisData = FlowGraphAnalysisData.Create(cfg, owningSymbol, AnalyzeLocalFunctionOrLambdaInvocation);
            }

            private DataFlowAnalyzer(
                ControlFlowGraph cfg,
                IMethodSymbol lambdaOrLocalFunction,
                FlowGraphAnalysisData parentAnalysisData)
            {
                _analysisData = FlowGraphAnalysisData.Create(cfg, lambdaOrLocalFunction, parentAnalysisData);

                var entryBlockAnalysisData = GetEmptyAnalysisData();
                entryBlockAnalysisData.SetAnalysisDataFrom(parentAnalysisData.CurrentBlockAnalysisData);
                _analysisData.SetBlockAnalysisData(cfg.EntryBlock(), entryBlockAnalysisData);
            }

            public static SymbolUsageResult RunAnalysis(ControlFlowGraph cfg, ISymbol owningSymbol, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var analyzer = new DataFlowAnalyzer(cfg, owningSymbol))
                {
                    _ = CustomDataFlowAnalysis<BasicBlockAnalysisData>.Run(cfg, analyzer, cancellationToken);
                    return analyzer._analysisData.ToResult();
                }
            }

            public override void Dispose()
                => _analysisData.Dispose();

            private static BasicBlockAnalysisData AnalyzeLocalFunctionOrLambdaInvocation(
                IMethodSymbol localFunctionOrLambda,
                ControlFlowGraph cfg,
                AnalysisData parentAnalysisData,
                CancellationToken cancellationToken)
            {
                Debug.Assert(localFunctionOrLambda.IsLocalFunction() || localFunctionOrLambda.IsAnonymousFunction());

                cancellationToken.ThrowIfCancellationRequested();
                using (var analyzer = new DataFlowAnalyzer(cfg, localFunctionOrLambda, (FlowGraphAnalysisData)parentAnalysisData))
                {
                    var resultBlockAnalysisData = CustomDataFlowAnalysis<BasicBlockAnalysisData>.Run(cfg, analyzer, cancellationToken);
                    if (resultBlockAnalysisData == null)
                    {
                        // Unreachable exit block from lambda/local.
                        // So use our current analysis data.
                        return parentAnalysisData.CurrentBlockAnalysisData;
                    }

                    // We need to return a cloned basic block analysis data as disposing the DataFlowAnalyzer
                    // created above will dispose all basic block analysis data instances allocated by it.
                    var clonedBasicBlockData = parentAnalysisData.CreateBlockAnalysisData();
                    clonedBasicBlockData.SetAnalysisDataFrom(resultBlockAnalysisData);
                    return clonedBasicBlockData;
                }
            }

            // Don't analyze blocks which are unreachable, as any write
            // in such a block which has a read outside will be marked redundant, which will just be noise for users.
            // For example,
            //      int x;
            //      if (true)
            //          x = 0;
            //      else
            //          x = 1; // This will be marked redundant if "AnalyzeUnreachableBlocks = true"
            //      return x;
            public override bool AnalyzeUnreachableBlocks => false;

            public override BasicBlockAnalysisData AnalyzeBlock(BasicBlock basicBlock, CancellationToken cancellationToken)
            {
                BeforeBlockAnalysis();
                Walker.AnalyzeOperationsAndUpdateData(basicBlock.Operations, _analysisData, cancellationToken);
                AfterBlockAnalysis();
                return _analysisData.CurrentBlockAnalysisData;

                // Local functions.
                void BeforeBlockAnalysis()
                {
                    // Initialize current block analysis data.
                    _analysisData.SetCurrentBlockAnalysisDataFrom(basicBlock, cancellationToken);

                    // At start of entry block, handle parameter definitions from method declaration.
                    if (basicBlock.Kind == BasicBlockKind.Entry)
                    {
                        _analysisData.SetAnalysisDataOnEntryBlockStart();
                    }
                }

                void AfterBlockAnalysis()
                {
                    // If we are exiting the control flow graph, handle ref/out parameter definitions from method declaration.
                    if (basicBlock.FallThroughSuccessor?.Destination == null &&
                        basicBlock.ConditionalSuccessor?.Destination == null)
                    {
                        _analysisData.SetAnalysisDataOnMethodExit();
                    }
                }
            }

            public override BasicBlockAnalysisData AnalyzeNonConditionalBranch(
                BasicBlock basicBlock,
                BasicBlockAnalysisData currentBlockAnalysisData,
                CancellationToken cancellationToken)
                => AnalyzeBranch(basicBlock, currentBlockAnalysisData, cancellationToken);

            public override (BasicBlockAnalysisData fallThroughSuccessorData, BasicBlockAnalysisData conditionalSuccessorData) AnalyzeConditionalBranch(
                BasicBlock basicBlock,
                BasicBlockAnalysisData currentAnalysisData,
                CancellationToken cancellationToken)
            {
                var resultAnalysisData = AnalyzeBranch(basicBlock, currentAnalysisData, cancellationToken);
                return (resultAnalysisData, resultAnalysisData);
            }

            private BasicBlockAnalysisData AnalyzeBranch(
                BasicBlock basicBlock,
                BasicBlockAnalysisData currentBlockAnalysisData,
                CancellationToken cancellationToken)
            {
                // Initialize current analysis data
                _analysisData.SetCurrentBlockAnalysisDataFrom(currentBlockAnalysisData);

                // Analyze the branch value
                var operations = SpecializedCollections.SingletonEnumerable(basicBlock.BranchValue);
                Walker.AnalyzeOperationsAndUpdateData(operations, _analysisData, cancellationToken);
                return _analysisData.CurrentBlockAnalysisData;
            }

            public override BasicBlockAnalysisData GetCurrentAnalysisData(BasicBlock basicBlock)
                => _analysisData.GetBlockAnalysisData(basicBlock);

            public override BasicBlockAnalysisData GetEmptyAnalysisData()
                => _analysisData.CreateBlockAnalysisData();

            public override void SetCurrentAnalysisData(BasicBlock basicBlock, BasicBlockAnalysisData data, CancellationToken cancellationToken)
                => _analysisData.SetBlockAnalysisDataFrom(basicBlock, data, cancellationToken);

            public override bool IsEqual(BasicBlockAnalysisData analysisData1, BasicBlockAnalysisData analysisData2)
                => analysisData1 == null ? analysisData2 == null : analysisData1.Equals(analysisData2);

            public override BasicBlockAnalysisData Merge(
                BasicBlockAnalysisData analysisData1,
                BasicBlockAnalysisData analysisData2,
                CancellationToken cancellationToken)
                => BasicBlockAnalysisData.Merge(analysisData1, analysisData2, GetEmptyAnalysisData);
        }
    }
}
