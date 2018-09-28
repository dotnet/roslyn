// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        internal sealed class ReachabilityAnalyzer : IDataFlowAnalyzer<bool>
        {
            private BitVector _visited = BitVector.Empty;
            private ReachabilityAnalyzer() { }

            public static void Run(ArrayBuilder<BasicBlockBuilder> blocks, CancellationToken cancellationToken)
                => CustomDataFlowAnalysis<ReachabilityAnalyzer, bool>.Run(blocks.ToImmutable(), new ReachabilityAnalyzer(), cancellationToken);

            public bool AnalyzeUnreachableBlocks => false;

            public bool AnalyzeBlock(BasicBlockBuilder basicBlock, CancellationToken cancellationToken)
            {
                SetCurrentAnalysisData(basicBlock, isReachable: true);
                return true;
            }

            public bool AnalyzeNonConditionalBranch(BasicBlockBuilder basicBlock, bool currentAnalysisData, CancellationToken cancellationToken)
                => currentAnalysisData;

            public (bool fallThroughSuccessorData, bool conditionalSuccessorData) AnalyzeConditionalBranch(
                BasicBlockBuilder basicBlock,
                bool currentAnalysisData,
                CancellationToken cancellationToken)
                => (currentAnalysisData, currentAnalysisData);

            public void SetCurrentAnalysisData(BasicBlockBuilder basicBlock, bool isReachable)
            {
                _visited[basicBlock.Ordinal] = isReachable;
                basicBlock.IsReachable = isReachable;
            }

            public bool GetCurrentAnalysisData(BasicBlockBuilder basicBlock) => _visited[basicBlock.Ordinal];

            public bool GetEmptyAnalysisData() => false;

            public bool Merge(bool analysisData1, bool analysisData2, CancellationToken cancellationToken)
                => analysisData1 || analysisData2;

            public bool IsEqual(bool analysisData1, bool analysisData2)
                => analysisData1 == analysisData2;

            public void Dispose()
            {
            }
        }
    }
}
