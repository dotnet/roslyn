// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralPropertySetAnalysisData? interproceduralAnalysisData,
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
                  copyAnalysisResult: null,
                  pointsToAnalysisResult,
                  valueContentAnalysisResult,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraph,
                  interproceduralAnalysisData,
                  interproceduralAnalysisPredicate: null)
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
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult?> tryGetOrComputeAnalysisResult,
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
                valueContentAnalysisResult,
                tryGetOrComputeAnalysisResult,
                parentControlFlowGraph: null,
                interproceduralAnalysisData: null,
                typeToTrackMetadataNames: typeToTrackMetadataNames,
                constructorMapper: constructorMapper,
                propertyMappers: propertyMappers,
                hazardousUsageEvaluators: hazardousUsageEvaluators,
                hazardousUsageTypesToNames: hazardousUsageEvaluators.GetTypeToNameMapping(wellKnownTypeProvider));
        }

        public override PropertySetAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralPropertySetAnalysisData? interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResult != null);
            Debug.Assert(copyAnalysisResult == null);

            return new PropertySetAnalysisContext(
                ValueDomain,
                WellKnownTypeProvider,
                invokedCfg,
                invokedMethod,
                AnalyzerOptions,
                InterproceduralAnalysisConfiguration,
                PessimisticAnalysis,
                pointsToAnalysisResult,
                valueContentAnalysisResult,
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

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
            hashCode.Add(TypeToTrackMetadataNames.GetHashCode());
            hashCode.Add(ConstructorMapper.GetHashCode());
            hashCode.Add(PropertyMappers.GetHashCode());
            hashCode.Add(HazardousUsageEvaluators.GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<PropertySetAnalysisData, PropertySetAnalysisContext, PropertySetAnalysisResult, PropertySetAbstractValue> obj)
        {
            var other = (PropertySetAnalysisContext)obj;
            return TypeToTrackMetadataNames.GetHashCode() == other.TypeToTrackMetadataNames.GetHashCode()
                && ConstructorMapper.GetHashCode() == other.ConstructorMapper.GetHashCode()
                && PropertyMappers.GetHashCode() == other.PropertyMappers.GetHashCode()
                && HazardousUsageEvaluators.GetHashCode() == other.HazardousUsageEvaluators.GetHashCode();
        }
    }
}
