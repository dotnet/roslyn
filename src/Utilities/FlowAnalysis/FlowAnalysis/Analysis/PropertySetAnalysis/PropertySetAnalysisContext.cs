// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralPropertySetAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>, PropertySetAnalysisContext, PropertySetAbstractValue>;
    using PropertySetAnalysisData = DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="PropertySetAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class PropertySetAnalysisContext : AbstractDataFlowAnalysisContext<PropertySetAnalysisData, PropertySetAnalysisContext, PropertySetAnalysisResult, PropertySetAbstractValue>
    {
        private PropertySetAnalysisContext(
            AbstractValueDomain<PropertySetAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult,
            ValueContentAnalysisResult valueContentAnalysisResultOpt,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult> tryGetOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralPropertySetAnalysisData interproceduralAnalysisDataOpt,
            ImmutableHashSet<string> typeToTrackMetadataNames,
            ConstructorMapper constructorMapper,
            PropertyMapperCollection propertyMappers,
            HazardousUsageEvaluatorCollection hazardousUsageEvaluators,
            ImmutableDictionary<(INamedTypeSymbol, bool), string> hazardousUsageTypesToNames)
            : base(
                  valueDomain,
                  wellKnownTypeProvider,
                  controlFlowGraph,
                  owningSymbol,
                  analyzerOptions,
                  interproceduralAnalysisConfig,
                  pessimisticAnalysis,
                  predicateAnalysis: false,
                  exceptionPathsAnalysis: false,
                  copyAnalysisResultOpt: null,
                  pointsToAnalysisResult,
                  valueContentAnalysisResultOpt,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt,
                  interproceduralAnalysisPredicateOpt: null)
        {
            this.TypeToTrackMetadataNames = typeToTrackMetadataNames;
            this.ConstructorMapper = constructorMapper;
            this.PropertyMappers = propertyMappers;
            this.HazardousUsageEvaluators = hazardousUsageEvaluators;
            this.HazardousUsageTypesToNames = hazardousUsageTypesToNames;
        }

        public static PropertySetAnalysisContext Create(
            AbstractValueDomain<PropertySetAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult,
            ValueContentAnalysisResult valueContentAnalysisResultOpt,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult> tryGetOrComputeAnalysisResult,
            ImmutableHashSet<string> typeToTrackMetadataNames,
            ConstructorMapper constructorMapper,
            PropertyMapperCollection propertyMappers,
            HazardousUsageEvaluatorCollection hazardousUsageEvaluators)
        {
            return new PropertySetAnalysisContext(
                valueDomain,
                wellKnownTypeProvider,
                controlFlowGraph,
                owningSymbol,
                analyzerOptions,
                interproceduralAnalysisConfig,
                pessimisticAnalysis,
                pointsToAnalysisResult,
                valueContentAnalysisResultOpt,
                tryGetOrComputeAnalysisResult,
                parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null,
                typeToTrackMetadataNames: typeToTrackMetadataNames,
                constructorMapper: constructorMapper,
                propertyMappers: propertyMappers,
                hazardousUsageEvaluators: hazardousUsageEvaluators,
                hazardousUsageTypesToNames: hazardousUsageEvaluators.GetTypeToNameMapping(wellKnownTypeProvider));
        }

        public override PropertySetAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            ValueContentAnalysisResult valueContentAnalysisResultOpt,
            InterproceduralPropertySetAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt != null);
            Debug.Assert(copyAnalysisResultOpt == null);

            return new PropertySetAnalysisContext(
                ValueDomain,
                WellKnownTypeProvider,
                invokedCfg,
                invokedMethod,
                AnalyzerOptions,
                InterproceduralAnalysisConfiguration,
                PessimisticAnalysis,
                pointsToAnalysisResultOpt,
                valueContentAnalysisResultOpt,
                TryGetOrComputeAnalysisResult,
                ControlFlowGraph,
                interproceduralAnalysisData,
                this.TypeToTrackMetadataNames,
                this.ConstructorMapper,
                this.PropertyMappers,
                this.HazardousUsageEvaluators,
                this.HazardousUsageTypesToNames);
        }

        /// <summary>
        /// Metadata names of the types to track.
        /// </summary>
        public ImmutableHashSet<string> TypeToTrackMetadataNames { get; }

        /// <summary>
        /// How constructor invocations map to <see cref="PropertySetAbstractValueKind"/>s.
        /// </summary>
        public ConstructorMapper ConstructorMapper { get; }

        /// <summary>
        /// How property assignments map to <see cref="PropertySetAbstractValueKind"/>.
        /// </summary>
        public PropertyMapperCollection PropertyMappers { get; }

        /// <summary>
        /// When and how to evaluate <see cref="PropertySetAbstractValueKind"/>s to for hazardous usages.
        /// </summary>
        public HazardousUsageEvaluatorCollection HazardousUsageEvaluators { get; }

        public ImmutableDictionary<(INamedTypeSymbol, bool), string> HazardousUsageTypesToNames { get; }

#pragma warning disable CA1307 // Specify StringComparison - string.GetHashCode(StringComparison) not available in all projects that reference this shared project
        protected override void ComputeHashCodePartsSpecific(Action<int> addPart)
        {
            addPart(TypeToTrackMetadataNames.GetHashCode());
            addPart(ConstructorMapper.GetHashCode());
            addPart(PropertyMappers.GetHashCode());
            addPart(HazardousUsageEvaluators.GetHashCode());
        }
#pragma warning restore CA1307 // Specify StringComparison
    }
}
