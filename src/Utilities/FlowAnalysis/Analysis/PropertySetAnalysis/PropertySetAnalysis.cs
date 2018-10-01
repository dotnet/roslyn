// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using PropertySetAnalysisData = IDictionary<AbstractLocation, PropertySetAbstractValue>;
    using InterproceduralBinaryFormatterAnalysisData = InterproceduralAnalysisData<IDictionary<AbstractLocation, PropertySetAbstractValue>, PropertySetAnalysisContext, PropertySetAbstractValue>;
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

        public static ImmutableDictionary<IInvocationOperation, PropertySetAbstractValue> GetOrComputeHazardousParameterUsages(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol owningSymbol,
            string typeToTrackMetadataName,
            bool isNewInstanceFlagged,
            string propertyToSetFlag,
            bool isNullPropertyFlagged,
            ImmutableHashSet<string> methodNamesToCheckForFlaggedUsage,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            bool pessimisticAnalysis = false)
        {
            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(
                cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, pessimisticAnalysis);

            var analysisContext = PropertySetAnalysisContext.Create(PropertySetAbstractValueDomain.Default,
                wellKnownTypeProvider, cfg, owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis, pointsToAnalysisResult, GetOrComputeResultForAnalysisContext,
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

        internal override PropertySetAnalysisResult ToResult(
            PropertySetAnalysisContext analysisContext,
            DataFlowAnalysisResult<PropertySetBlockAnalysisResult, PropertySetAbstractValue> dataFlowAnalysisResult)
        {
            //analysisContext = analysisContext.WithTrackHazardousParameterUsages();
            //var newOperationVisitor = new BinaryFormatterDataFlowOperationVisitor(analysisContext);
            //var resultBuilder = new DataFlowAnalysisResultBuilder<BinaryFormatterAnalysisData>();
            //foreach (var block in analysisContext.ControlFlowGraph.Blocks)
            //{
            //    var data = BinaryFormatterAnalysisDomainInstance.Clone(dataFlowAnalysisResult[block].InputData);
            //    data = Flow(newOperationVisitor, block, data);
            //}

            //return new BinaryFormatterAnalysisResult(dataFlowAnalysisResult, newOperationVisitor.HazardousUsages);

            // Hey Manish, is it okay to just look at this.OperationVisitor?

            return new PropertySetAnalysisResult(
                dataFlowAnalysisResult,
                ((PropertySetDataFlowOperationVisitor) this.OperationVisitor).HazardousUsages);
        }

        internal override PropertySetBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<PropertySetAnalysisData> blockAnalysisData) => new PropertySetBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
