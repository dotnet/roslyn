// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisData = DictionaryAnalysisData<AnalysisEntity, InvocationCountAnalysisValue>;
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAnalysisValue>;
    using InvocationCountAnalysisDomain = MapAbstractDomain<AnalysisEntity, InvocationCountAnalysisValue>;

    internal class InvocationCountAnalysis : ForwardDataFlowAnalysis<
        InvocationCountAnalysisData,
        InvocationCountAnalysisContext,
        InvocationCountAnalysisResult,
        InvocationCountBlockAnalysisResult,
        InvocationCountAnalysisValue>
    {
        public static readonly InvocationCountAnalysisDomain Domain = new(InvocationCountAnalysisValueDomain.Instance);

        public InvocationCountAnalysis(
            AbstractAnalysisDomain<InvocationCountAnalysisData> analysisDomain,
            DataFlowOperationVisitor<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAnalysisResult, InvocationCountAnalysisValue> operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        protected override InvocationCountAnalysisResult ToResult(InvocationCountAnalysisContext analysisContext, InvocationCountAnalysisResult dataFlowAnalysisResult)
        {
            // Use the global values map
            var operationVisitor = (InvocationCountDataFlowOperationVisitor)OperationVisitor;
            return dataFlowAnalysisResult.With(operationVisitor.GetGlobalValuesMap());
        }

        protected override InvocationCountBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, InvocationCountAnalysisData blockAnalysisData)
            => new(basicBlock, blockAnalysisData);
    }
}
