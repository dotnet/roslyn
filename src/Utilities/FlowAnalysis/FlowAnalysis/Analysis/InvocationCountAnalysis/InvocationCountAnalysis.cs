// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAbstractValue>;

    internal sealed class InvocationCountAnalysis : ForwardDataFlowAnalysis<
        InvocationCountAnalysisData,
        InvocationCountAnalysisContext,
        InvocationCountAnalysisResult,
        InvocationCountBlockAnalysisResult,
        InvocationCountAbstractValue>
    {
        public static readonly SymbolDisplayFormat MethodFullyQualifiedNameFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

        private InvocationCountAnalysis(
            AbstractAnalysisDomain<InvocationCountAnalysisData> analysisDomain,
            DataFlowOperationVisitor<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAnalysisResult, InvocationCountAbstractValue> operationVisitor) : base(analysisDomain, operationVisitor)
        {
        }

        public static InvocationCountAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            bool pessimisticAnalysis,
            ImmutableArray<string> trackingMethodNames,
            CancellationToken cancellationToken)
        {
            // TODO: Add Inter-procedural ability.
            // TODO: PointToAnalysis ability should be added here.
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, cfg, wellKnownTypeProvider.Compilation, InterproceduralAnalysisKind.None, cancellationToken);

            var context = new InvocationCountAnalysisContext(
                InvocationCountValueDomain.Instance,
                wellKnownTypeProvider,
                cfg,
                owningSymbol,
                analyzerOptions,
                interproceduralAnalysisConfig,
                pessimisticAnalysis,
                predicateAnalysis: true,
                exceptionPathsAnalysis: true,
                tryGetOrComputeAnalysisResult: analysisContext => TryGetOrComputeAnalysisResult(analysisContext, trackingMethodNames));

            return TryGetOrComputeAnalysisResult(context, trackingMethodNames);
        }

        private static InvocationCountAnalysisResult? TryGetOrComputeAnalysisResult(
            InvocationCountAnalysisContext context,
            ImmutableArray<string> trackingMethodNames)
        {
            var operationVisitor = new InvocationCountDataFlowOperationVisitor(context, trackingMethodNames);
            var analysis = new InvocationCountAnalysis(
                new InvocationCountAnalysisDomain(new MapAbstractDomain<AnalysisEntity, InvocationCountAbstractValue>(InvocationCountValueDomain.Instance)),
                operationVisitor);
            return analysis.TryGetOrComputeResultCore(context, cacheResult: false);
        }

        protected override InvocationCountAnalysisResult ToResult(
            InvocationCountAnalysisContext analysisContext,
            InvocationCountAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;

        protected override InvocationCountBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, InvocationCountAnalysisData blockAnalysisData)
        {
            return new(blockAnalysisData, basicBlock);
        }
    }
}