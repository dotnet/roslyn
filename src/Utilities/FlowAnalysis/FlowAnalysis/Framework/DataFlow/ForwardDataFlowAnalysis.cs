// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Subtype for all forward dataflow analyses.
    /// These analyses operate on the control flow graph starting from the entry block,
    /// flowing the dataflow values forward to the successor blocks until a fix point is reached.
    /// </summary>
    public abstract class ForwardDataFlowAnalysis<TAnalysisData, TAnalysisContext, TAnalysisResult, TBlockAnalysisResult, TAbstractAnalysisValue>
        : DataFlowAnalysis<TAnalysisData, TAnalysisContext, TAnalysisResult, TBlockAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>
        where TBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        protected ForwardDataFlowAnalysis(AbstractAnalysisDomain<TAnalysisData> analysisDomain, DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }
    }
}
