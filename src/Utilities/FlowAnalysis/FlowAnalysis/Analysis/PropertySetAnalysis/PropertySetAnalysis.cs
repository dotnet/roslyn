// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using PropertySetAnalysisData = DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>;
    using InterproceduralBinaryFormatterAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>, PropertySetAnalysisContext, PropertySetAbstractValue>;
    using PropertySetAnalysisDomain = MapAbstractDomain<AbstractLocation, PropertySetAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="PropertySetAbstractValue"/> of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    internal partial class PropertySetAnalysis : ForwardDataFlowAnalysis<PropertySetAnalysisData, PropertySetAnalysisContext, PropertySetAnalysisResult, PropertySetBlockAnalysisResult, PropertySetAbstractValue>
    {
        public static readonly PropertySetAnalysisDomain BinaryFormatterAnalysisDomainInstance = new PropertySetAnalysisDomain(PropertySetAbstractValueDomain.Default);

        private PropertySetAnalysis(PropertySetAnalysisDomain analysisDomain, PropertySetDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static ImmutableDictionary<(Location Location, IMethodSymbol method), PropertySetAbstractValue> GetOrComputeHazardousParameterUsages(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            string typeToTrackMetadataName,
            bool isNewInstanceFlagged,
            string propertyToSetFlag,
            bool isNullPropertyFlagged,
            ImmutableHashSet<string> methodNamesToCheckForFlaggedUsage,
            CancellationToken cancellationToken,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            uint defaultMaxInterproceduralMethodCallChain = 1, // By default, we only want to track method calls one level down.
            bool pessimisticAnalysis = false)
        {
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                   analyzerOptions, rule, interproceduralAnalysisKind, cancellationToken, defaultMaxInterproceduralMethodCallChain);
            return GetOrComputeHazardousParameterUsages(cfg, compilation, owningSymbol,
                typeToTrackMetadataName, isNewInstanceFlagged, propertyToSetFlag, isNullPropertyFlagged,
                methodNamesToCheckForFlaggedUsage, interproceduralAnalysisConfig, pessimisticAnalysis);
        }

        public static ImmutableDictionary<(Location Location, IMethodSymbol method), PropertySetAbstractValue> GetOrComputeHazardousParameterUsages(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol owningSymbol,
            string typeToTrackMetadataName,
            bool isNewInstanceFlagged,
            string propertyToSetFlag,
            bool isNullPropertyFlagged,
            ImmutableHashSet<string> methodNamesToCheckForFlaggedUsage,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis = false)
        {
            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(
                cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisConfig, pessimisticAnalysis);

            var analysisContext = PropertySetAnalysisContext.Create(PropertySetAbstractValueDomain.Default,
                wellKnownTypeProvider, cfg, owningSymbol, interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResult, GetOrComputeResultForAnalysisContext,
                typeToTrackMetadataName,
                isNewInstanceFlagged,
                propertyToSetFlag,
                isNullPropertyFlagged,
                methodNamesToCheckForFlaggedUsage);
            var result = GetOrComputeResultForAnalysisContext(analysisContext);
            return result.HazardousUsages;
        }

        private static PropertySetAnalysisResult GetOrComputeResultForAnalysisContext(PropertySetAnalysisContext analysisContext)
        {
            var operationVisitor = new PropertySetDataFlowOperationVisitor(analysisContext);
            var analysis = new PropertySetAnalysis(BinaryFormatterAnalysisDomainInstance, operationVisitor);
            return analysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override PropertySetAnalysisResult ToResult(
            PropertySetAnalysisContext analysisContext,
            DataFlowAnalysisResult<PropertySetBlockAnalysisResult, PropertySetAbstractValue> dataFlowAnalysisResult)
        {
            return new PropertySetAnalysisResult(
                dataFlowAnalysisResult,
                ((PropertySetDataFlowOperationVisitor)this.OperationVisitor).HazardousUsages);
        }

        protected override PropertySetBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, PropertySetAnalysisData blockAnalysisData) => new PropertySetBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
