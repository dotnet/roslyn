// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.GlobalFlowStateDictionaryAnalysis
{
    using GlobalFlowStateDictionaryAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateDictionaryAnalysisValue>;
    using GlobalFlowStateDictionaryAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateDictionaryBlockAnalysisResult, GlobalFlowStateDictionaryAnalysisValue>;
    using GlobalFlowStateDictionaryAnalysisDomain = MapAbstractDomain<AnalysisEntity, GlobalFlowStateDictionaryAnalysisValue>;

    /// <summary>
    /// An analysis that tracks the state of a set of <see cref="AnalysisEntity"/>. The state is shared among the block.
    /// </summary>
    internal class GlobalFlowStateDictionaryAnalysis : ForwardDataFlowAnalysis<
        GlobalFlowStateDictionaryAnalysisData,
        GlobalFlowStateDictionaryAnalysisContext,
        GlobalFlowStateDictionaryAnalysisResult,
        GlobalFlowStateDictionaryBlockAnalysisResult,
        GlobalFlowStateDictionaryAnalysisValue>
    {
        public static readonly GlobalFlowStateDictionaryAnalysisDomain Domain = new(GlobalFlowStateDictionaryAnalysisValueDomain.Instance);

        public GlobalFlowStateDictionaryAnalysis(
            AbstractAnalysisDomain<GlobalFlowStateDictionaryAnalysisData> analysisDomain,
            DataFlowOperationVisitor<GlobalFlowStateDictionaryAnalysisData, GlobalFlowStateDictionaryAnalysisContext, GlobalFlowStateDictionaryAnalysisResult, GlobalFlowStateDictionaryAnalysisValue> operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static GlobalFlowStateDictionaryAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<GlobalFlowStateDictionaryAnalysisContext, GlobalFlowStateDictionaryFlowOperationVisitor> createOperationVisitor,
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

            var analysisContext = new GlobalFlowStateDictionaryAnalysisContext(
                GlobalFlowStateDictionaryAnalysisValueDomain.Instance,
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

        private static GlobalFlowStateDictionaryAnalysisResult? TryGetOrComputeAnalysisResult(
            GlobalFlowStateDictionaryAnalysisContext analysisContext,
            Func<GlobalFlowStateDictionaryAnalysisContext, GlobalFlowStateDictionaryFlowOperationVisitor> createOperationVisitor)
        {
            var operationVisitor = createOperationVisitor(analysisContext);
            var analysis = new GlobalFlowStateDictionaryAnalysis(Domain, operationVisitor);
            return analysis.TryGetOrComputeResultCore(analysisContext, cacheResult: false);
        }

        protected override GlobalFlowStateDictionaryAnalysisResult ToResult(GlobalFlowStateDictionaryAnalysisContext analysisContext, GlobalFlowStateDictionaryAnalysisResult dataFlowAnalysisResult)
        {
            // Use the global values map. Drop the per-operation inforamation
            var operationVisitor = (GlobalFlowStateDictionaryFlowOperationVisitor)OperationVisitor;
            return dataFlowAnalysisResult.With(operationVisitor.GetGlobalValuesMap());
        }

        protected override GlobalFlowStateDictionaryBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, GlobalFlowStateDictionaryAnalysisData blockAnalysisData)
            => new(basicBlock, blockAnalysisData);
    }
}
