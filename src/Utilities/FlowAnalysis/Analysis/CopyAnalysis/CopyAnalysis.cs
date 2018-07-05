// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisDomain = PredicatedAnalysisDataDomain<CopyAnalysisData, CopyAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="AnalysisEntity"/> instances that share the same value.
    /// </summary>
    internal partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        private static readonly CopyAnalysisDomain s_AnalysisDomain = new CopyAnalysisDomain(CoreCopyAnalysisDataDomain.Instance);

        private CopyAnalysis(CopyDataFlowOperationVisitor operationVisitor)
            : base(s_AnalysisDomain, operationVisitor)
        {
        }

        public static DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResultOpt = null,
            bool pessimisticAnalysis = true)
        {
            var operationVisitor = new CopyDataFlowOperationVisitor(CopyAbstractValueDomain.Default, owningSymbol, 
                wellKnownTypeProvider, cfg, pessimisticAnalysis, pointsToAnalysisResultOpt);
            var copyAnalysis = new CopyAnalysis(operationVisitor);
            return copyAnalysis.GetOrComputeResultCore(cfg, cacheResult: true);
        }

        [Conditional("DEBUG")]
        public static void AssertValidCopyAnalysisData(CopyAnalysisData data)
        {
            data.AssertValidCopyAnalysisData();
        }

        internal override CopyBlockAnalysisResult ToResult(BasicBlock basicBlock, DataFlowAnalysisInfo<CopyAnalysisData> blockAnalysisData) => new CopyBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
