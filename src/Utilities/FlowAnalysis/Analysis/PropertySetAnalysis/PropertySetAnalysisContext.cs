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

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralBinaryFormatterAnalysisData = InterproceduralAnalysisData<IDictionary<AbstractLocation, PropertySetAbstractValue>, PropertySetAnalysisContext, PropertySetAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;
    using PropertySetAnalysisData = IDictionary<AbstractLocation, PropertySetAbstractValue>;

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
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult> getOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralBinaryFormatterAnalysisData interproceduralAnalysisDataOpt,
            string typeToTrackMetadataName,
            bool isNewInstanceFlagged,
            string propertyToSetFlag,
            bool isNullPropertyFlagged,
            ImmutableHashSet<string> methodNamesToCheckForFlaggedUsage)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis,
                  predicateAnalysis: false, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResult: getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt: interproceduralAnalysisDataOpt)
        {
            this.TypeToTrackMetadataName = typeToTrackMetadataName;
            this.IsNewInstanceFlagged = isNewInstanceFlagged;
            this.PropertyToSetFlag = propertyToSetFlag;
            this.IsNullPropertyFlagged = isNullPropertyFlagged;
            this.MethodNamesToCheckForFlaggedUsage = methodNamesToCheckForFlaggedUsage;
        }

        public static PropertySetAnalysisContext Create(
            AbstractValueDomain<PropertySetAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<PropertySetAnalysisContext, PropertySetAnalysisResult> getOrComputeAnalysisResult,
            string typeToTrackMetadataName,
            bool isNewInstanceFlagged,
            string propertyToSetFlag,
            bool isNullPropertyFlagged,
            ImmutableHashSet<string> methodNamesToCheckForFlaggedUsage)

        {
            return new PropertySetAnalysisContext(
                valueDomain,
                wellKnownTypeProvider, 
                controlFlowGraph,
                owningSymbol,
                interproceduralAnalysisKind,
                pessimisticAnalysis,
                pointsToAnalysisResultOpt, 
                getOrComputeAnalysisResult, 
                parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null,
                typeToTrackMetadataName: typeToTrackMetadataName,
                isNewInstanceFlagged: isNewInstanceFlagged,
                propertyToSetFlag: propertyToSetFlag,
                isNullPropertyFlagged: isNullPropertyFlagged,
                methodNamesToCheckForFlaggedUsage: methodNamesToCheckForFlaggedUsage);
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
                ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, InterproceduralAnalysisKind,
                PessimisticAnalysis, pointsToAnalysisResultOpt, GetOrComputeAnalysisResult, ControlFlowGraph,
                interproceduralAnalysisData,
                this.TypeToTrackMetadataName,
                this.IsNewInstanceFlagged,
                this.PropertyToSetFlag,
                this.IsNullPropertyFlagged,
                this.MethodNamesToCheckForFlaggedUsage);
        }

        /// <summary>
        /// Metadata name of the type to track.
        /// </summary>
        public string TypeToTrackMetadataName { get; }

        /// <summary>
        /// How newly created instances should be considered: flagged or unflagged.
        /// </summary>
        public bool IsNewInstanceFlagged { get; }

        /// <summary>
        /// Name of the property that when assigned to, may change the abstract value.
        /// </summary>
        public string PropertyToSetFlag { get; }

        /// <summary>
        /// Whether to change the abstract value of the instance to flagged or not flagged,
        /// when the <see cref="PropertyToSetFlag"/> property is set to null or non-null.
        /// </summary>
        public bool IsNullPropertyFlagged { get; }

        /// <summary>
        /// Method names for invocations that check whether the instance is flagged or maybe flagged.
        /// </summary>
        public ImmutableHashSet<string> MethodNamesToCheckForFlaggedUsage { get; }

        protected override int GetHashCode(int hashCode) =>
            HashUtilities.Combine(this.TypeToTrackMetadataName.GetHashCode(),
                HashUtilities.Combine(this.IsNewInstanceFlagged.GetHashCode(),
                    HashUtilities.Combine(this.PropertyToSetFlag.GetHashCode(),
                        HashUtilities.Combine(this.IsNullPropertyFlagged.GetHashCode(),
                            HashUtilities.Combine(this.MethodNamesToCheckForFlaggedUsage,
                                hashCode)))));
    }
}
