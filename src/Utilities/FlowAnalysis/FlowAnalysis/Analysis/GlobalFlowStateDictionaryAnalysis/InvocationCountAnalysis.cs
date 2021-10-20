// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisData = DictionaryAnalysisData<AnalysisEntity, InvocationCountAnalysisValue>;
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAnalysisValue>;
    using InvocationCountAnalysisDomain = MapAbstractDomain<AnalysisEntity, InvocationCountAnalysisValue>;

    /// <summary>
    /// An analysis that tracks the state of a set of <see cref="AnalysisEntity"/>. The state is shared among the block.
    /// </summary>
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

        public static InvocationCountAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<InvocationCountAnalysisContext, InvocationCountDataFlowOperationVisitor> createOperationVisitor,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            bool pessimisticAnalysis,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, cfg, wellKnownTypeProvider.Compilation, interproceduralAnalysisKind);

            var analysisContext = new InvocationCountAnalysisContext(
                InvocationCountAnalysisValueDomain.Instance,
                wellKnownTypeProvider,
                cfg,
                owningSymbol,
                analyzerOptions,
                interproceduralAnalysisConfig,
                pessimisticAnalysis: pessimisticAnalysis,
                predicateAnalysis: false,
                exceptionPathsAnalysis: false,
                copyAnalysisResult: null,
                pointsToAnalysisResult: null,
                valueContentAnalysisResult: null,
                tryGetOrComputeAnalysisResult: c => TryGetOrComputeAnalysisResult(c, createOperationVisitor),
                interproceduralAnalysisPredicate: interproceduralAnalysisPredicate);

            return TryGetOrComputeAnalysisResult(analysisContext, createOperationVisitor);
        }

        private static InvocationCountAnalysisResult? TryGetOrComputeAnalysisResult(
            InvocationCountAnalysisContext analysisContext,
            Func<InvocationCountAnalysisContext, InvocationCountDataFlowOperationVisitor> createOperationVisitor)
        {
            var operationVisitor = createOperationVisitor(analysisContext);
            var analysis = new InvocationCountAnalysis(Domain, operationVisitor);
            return analysis.TryGetOrComputeResultCore(analysisContext, cacheResult: false);
        }

        protected override InvocationCountAnalysisResult ToResult(InvocationCountAnalysisContext analysisContext, InvocationCountAnalysisResult dataFlowAnalysisResult)
        {
            // Use the global values map. Drop the per-operation inforamation
            var operationVisitor = (InvocationCountDataFlowOperationVisitor)OperationVisitor;
            return dataFlowAnalysisResult.With(operationVisitor.GetGlobalValuesMap());
        }

        protected override InvocationCountBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, InvocationCountAnalysisData blockAnalysisData)
            => new(basicBlock, blockAnalysisData);
    }
}
