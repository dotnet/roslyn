// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralBinaryFormatterAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>, PropertySetAnalysisContext, PropertySetAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;
    using PropertySetAnalysisData = DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>;

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
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult> getOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralBinaryFormatterAnalysisData interproceduralAnalysisDataOpt,
            string typeToTrackMetadataName,
            ConstructorMapper constructorMapper,
            PropertyMapperCollection propertyMappers,
            HazardousUsageEvaluatorCollection hazardousUsageEvaluators)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: false, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResult: getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt: interproceduralAnalysisDataOpt)
        {
            this.TypeToTrackMetadataName = typeToTrackMetadataName;
            this.ConstructorMapper = constructorMapper;
            this.PropertyMappers = propertyMappers;
            this.HazardousUsageEvaluators = hazardousUsageEvaluators;
        }

        public static PropertySetAnalysisContext Create(
            AbstractValueDomain<PropertySetAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult> getOrComputeAnalysisResult,
            string typeToTrackMetadataName,
            ConstructorMapper constructorMapper,
            PropertyMapperCollection propertyMappers,
            HazardousUsageEvaluatorCollection hazardousUsageEvaluators)
        {
            return new PropertySetAnalysisContext(
                valueDomain,
                wellKnownTypeProvider,
                controlFlowGraph,
                owningSymbol,
                interproceduralAnalysisConfig,
                pessimisticAnalysis,
                pointsToAnalysisResultOpt,
                getOrComputeAnalysisResult,
                parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null,
                typeToTrackMetadataName: typeToTrackMetadataName,
                constructorMapper: constructorMapper,
                propertyMappers: propertyMappers,
                hazardousUsageEvaluators: hazardousUsageEvaluators);
        }

        public override PropertySetAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralBinaryFormatterAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt != null);
            Debug.Assert(copyAnalysisResultOpt == null);

            return new PropertySetAnalysisContext(
                ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, pointsToAnalysisResultOpt, GetOrComputeAnalysisResult, ControlFlowGraph,
                interproceduralAnalysisData,
                this.TypeToTrackMetadataName,
                this.ConstructorMapper,
                this.PropertyMappers,
                this.HazardousUsageEvaluators);
        }

        /// <summary>
        /// Metadata name of the type to track.
        /// </summary>
        public string TypeToTrackMetadataName { get; }

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

#pragma warning disable CA1307 // Specify StringComparison - string.GetHashCode(StringComparison) not available in all projects that reference this shared project
        protected override void ComputeHashCodePartsSpecific(ArrayBuilder<int> builder)
        {
            builder.Add(TypeToTrackMetadataName.GetHashCode());
            builder.Add(ConstructorMapper.GetHashCode());
            builder.Add(PropertyMappers.GetHashCode());
            builder.Add(HazardousUsageEvaluators.GetHashCode());
        }
#pragma warning restore CA1307 // Specify StringComparison
    }
}
