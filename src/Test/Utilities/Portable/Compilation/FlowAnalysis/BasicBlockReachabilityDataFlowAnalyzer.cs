// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed class BasicBlockReachabilityDataFlowAnalyzer : DataFlowAnalyzer<bool>
    {
        private BitVector _visited = BitVector.Empty;
        private BasicBlockReachabilityDataFlowAnalyzer() { }

        public static BitVector Run(ControlFlowGraph controlFlowGraph)
        {
            var analyzer = new BasicBlockReachabilityDataFlowAnalyzer();
            _ = CustomDataFlowAnalysis<bool>.Run(controlFlowGraph, analyzer, CancellationToken.None);
            return analyzer._visited;
        }


        public override bool AnalyzeUnreachableBlocks => false;

        public override bool AnalyzeBlock(BasicBlock basicBlock, CancellationToken cancellationToken)
        {
            SetCurrentAnalysisData(basicBlock, isReachable: true);
            return true;
        }

        public override bool AnalyzeNonConditionalBranch(BasicBlock basicBlock, bool currentAnalysisData, CancellationToken cancellationToken)
            => currentAnalysisData;

        public override (bool fallThroughSuccessorData, bool conditionalSuccessorData) AnalyzeConditionalBranch(
            BasicBlock basicBlock,
            bool currentAnalysisData,
            CancellationToken cancellationToken)
            => (currentAnalysisData, currentAnalysisData);

        public override void SetCurrentAnalysisData(BasicBlock basicBlock, bool isReachable)
        {
            _visited[basicBlock.Ordinal] = isReachable;
        }

        public override bool GetCurrentAnalysisData(BasicBlock basicBlock) => _visited[basicBlock.Ordinal];

        public override bool GetEmptyAnalysisData() => false;

        public override bool Merge(bool analysisData1, bool analysisData2, CancellationToken cancellationToken)
            => analysisData1 || analysisData2;

        public override bool IsEqual(bool analysisData1, bool analysisData2)
            => analysisData1 == analysisData2;

        public void Dispose()
        {
        }
    }
}
