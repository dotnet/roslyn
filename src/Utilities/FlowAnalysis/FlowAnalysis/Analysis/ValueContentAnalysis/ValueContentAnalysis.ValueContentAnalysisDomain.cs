// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    public partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        /// <summary>
        /// An abstract analysis domain implementation for <see cref="ValueContentAnalysisData"/> tracked by <see cref="ValueContentAnalysis"/>.
        /// </summary>
        private sealed class ValueContentAnalysisDomain : PredicatedAnalysisDataDomain<ValueContentAnalysisData, ValueContentAbstractValue>
        {
            public static readonly ValueContentAnalysisDomain Instance = new ValueContentAnalysisDomain();

            private ValueContentAnalysisDomain()
                : base(CoreAnalysisDataDomain.Instance)
            {
            }

            public ValueContentAnalysisData MergeAnalysisDataForBackEdge(ValueContentAnalysisData forwardEdgeAnalysisData, ValueContentAnalysisData backEdgeAnalysisData)
            {
                if (!forwardEdgeAnalysisData.IsReachableBlockData && backEdgeAnalysisData.IsReachableBlockData)
                {
                    return (ValueContentAnalysisData)backEdgeAnalysisData.Clone();
                }
                else if (!backEdgeAnalysisData.IsReachableBlockData && forwardEdgeAnalysisData.IsReachableBlockData)
                {
                    return (ValueContentAnalysisData)forwardEdgeAnalysisData.Clone();
                }

                Debug.Assert(forwardEdgeAnalysisData.IsReachableBlockData == backEdgeAnalysisData.IsReachableBlockData);

                var mergedCoreAnalysisData = ((CoreAnalysisDataDomain)CoreDataAnalysisDomain).MergeAnalysisDataForBackEdge(forwardEdgeAnalysisData.CoreAnalysisData, backEdgeAnalysisData.CoreAnalysisData);
                return new ValueContentAnalysisData(mergedCoreAnalysisData, forwardEdgeAnalysisData,
                    backEdgeAnalysisData, forwardEdgeAnalysisData.IsReachableBlockData, CoreDataAnalysisDomain);
            }
        }
    }
}