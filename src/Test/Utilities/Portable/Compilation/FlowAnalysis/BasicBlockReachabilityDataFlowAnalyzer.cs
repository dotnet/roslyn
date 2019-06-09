// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
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

        // Do not analyze unreachable control flow branches and blocks.
        // This way all blocks that are called back to be analyzed in AnalyzeBlock are reachable
        // and the remaining blocks are unreachable. 
        public override bool AnalyzeUnreachableBlocks => false;

        public override bool AnalyzeBlock(BasicBlock basicBlock, CancellationToken cancellationToken)
        {
            SetCurrentAnalysisData(basicBlock, isReachable: true, cancellationToken);
            return true;
        }

        public override bool AnalyzeNonConditionalBranch(BasicBlock basicBlock, bool currentIsReachable, CancellationToken cancellationToken)
        {
            // Feasibility of control flow branches is analyzed by the core CustomDataFlowAnalysis
            // walker. If it identifies a branch as infeasible, it never invokes
            // this callback.
            // Assert that we are on a reachable control flow path, and retain the current reachability.
            Debug.Assert(currentIsReachable);

            return currentIsReachable;
        }

        public override (bool fallThroughSuccessorData, bool conditionalSuccessorData) AnalyzeConditionalBranch(
            BasicBlock basicBlock,
            bool currentIsReachable,
            CancellationToken cancellationToken)
        {
            // Feasibility of control flow branches is analyzed by the core CustomDataFlowAnalysis
            // walker. If it identifies a branch as infeasible, it never invokes
            // this callback.
            // Assert that we are on a reachable control flow path, and retain the current reachability
            // for both the destination blocks.
            Debug.Assert(currentIsReachable);

            return (currentIsReachable, currentIsReachable);
        }

        public override void SetCurrentAnalysisData(BasicBlock basicBlock, bool isReachable, CancellationToken cancellationToken)
        {
            _visited[basicBlock.Ordinal] = isReachable;
        }

        public override bool GetCurrentAnalysisData(BasicBlock basicBlock) => _visited[basicBlock.Ordinal];

        // A basic block is considered unreachable by default.
        public override bool GetEmptyAnalysisData() => false;

        // Destination block is reachable if either of the precedecessor blocks are reachable.
        public override bool Merge(bool predecessor1IsReachable, bool predecessor2IsReachable, CancellationToken cancellationToken)
            => predecessor1IsReachable || predecessor2IsReachable;

        public override bool IsEqual(bool isReachable1, bool isReachable2)
            => isReachable1 == isReachable2;
    }
}
