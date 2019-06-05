// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using PropertySetAnalysisData = DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>;
    using PropertySetAnalysisDomain = MapAbstractDomain<AbstractLocation, PropertySetAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="PropertySetAbstractValue"/> of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    internal partial class PropertySetAnalysis : ForwardDataFlowAnalysis<PropertySetAnalysisData, PropertySetAnalysisContext, PropertySetAnalysisResult, PropertySetBlockAnalysisResult, PropertySetAbstractValue>
    {
        public static readonly PropertySetAnalysisDomain PropertySetAnalysisDomainInstance = new PropertySetAnalysisDomain(PropertySetAbstractValueDomain.Default);

        private PropertySetAnalysis(PropertySetAnalysisDomain analysisDomain, PropertySetDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        /// <summary>
        /// Gets hazardous usages of an object based on a set of its properties.
        /// </summary>
        /// <param name="cfg">Control flow graph of the code.</param>
        /// <param name="compilation">Compilation containing the code.</param>
        /// <param name="owningSymbol">Symbol of the code to examine.</param>
        /// <param name="typeToTrackMetadataName">Name of the type to track.</param>
        /// <param name="constructorMapper">How constructor invocations map to <see cref="PropertySetAbstractValueKind"/>s.</param>
        /// <param name="propertyMappers">How property assignments map to <see cref="PropertySetAbstractValueKind"/>.</param>
        /// <param name="hazardousUsageEvaluators">When and how to evaluate <see cref="PropertySetAbstractValueKind"/>s to for hazardous usages.</param>
        /// <param name="interproceduralAnalysisConfig">Interprocedural dataflow analysis configuration.</param>
        /// <param name="pessimisticAnalysis">Whether to be pessimistic.</param>
        /// <returns>Dictionary of <see cref="Location"/> and <see cref="IMethodSymbol"/> pairs mapping to the kind of hazardous usage (Flagged or MaybeFlagged).</returns>
        public static ImmutableDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> GetOrComputeHazardousUsages(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol owningSymbol,
            string typeToTrackMetadataName,
            ConstructorMapper constructorMapper,
            PropertyMapperCollection propertyMappers,
            HazardousUsageEvaluatorCollection hazardousUsageEvaluators,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis = false)
        {
            if (constructorMapper == null)
            {
                throw new ArgumentNullException(nameof(constructorMapper));
            }

            if (propertyMappers == null)
            {
                throw new ArgumentNullException(nameof(propertyMappers));
            }

            if (hazardousUsageEvaluators == null)
            {
                throw new ArgumentNullException(nameof(hazardousUsageEvaluators));
            }

            constructorMapper.Validate(propertyMappers.Count);

            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);

            PointsToAnalysisResult pointsToAnalysisResult;
            ValueContentAnalysisResult valueContentAnalysisResultOpt;
            if (!constructorMapper.RequiresValueContentAnalysis && !propertyMappers.RequiresValueContentAnalysis)
            {
                pointsToAnalysisResult = PointsToAnalysis.TryGetOrComputeResult(
                    cfg,
                    owningSymbol,
                    wellKnownTypeProvider,
                    interproceduralAnalysisConfig,
                    interproceduralAnalysisPredicateOpt: null,
                    pessimisticAnalysis,
                    performCopyAnalysis: false);
                if (pointsToAnalysisResult == null)
                {
                    return null;
                }

                valueContentAnalysisResultOpt = null;
            }
            else
            {
                valueContentAnalysisResultOpt = ValueContentAnalysis.TryGetOrComputeResult(
                    cfg,
                    owningSymbol,
                    wellKnownTypeProvider,
                    interproceduralAnalysisConfig,
                    out var copyAnalysisResult,
                    out pointsToAnalysisResult,
                    pessimisticAnalysis,
                    performCopyAnalysis: false);
                if (valueContentAnalysisResultOpt == null)
                {
                    return null;
                }
            }

            var analysisContext = PropertySetAnalysisContext.Create(
                PropertySetAbstractValueDomain.Default,
                wellKnownTypeProvider,
                cfg,
                owningSymbol,
                interproceduralAnalysisConfig,
                pessimisticAnalysis,
                pointsToAnalysisResult,
                valueContentAnalysisResultOpt,
                TryGetOrComputeResultForAnalysisContext,
                typeToTrackMetadataName,
                constructorMapper,
                propertyMappers,
                hazardousUsageEvaluators);
            var result = TryGetOrComputeResultForAnalysisContext(analysisContext);
            return result?.HazardousUsages ?? ImmutableDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult>.Empty;
        }

        public static PooledDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> BatchGetOrComputeHazardousUsages(
            Compilation compilation,
            IEnumerable<(IOperation Operation, ISymbol ContainingSymbol)> rootOperationsNeedingAnalysis,
            string typeToTrackMetadataName,
            ConstructorMapper constructorMapper,
            PropertyMapperCollection propertyMappers,
            HazardousUsageEvaluatorCollection hazardousUsageEvaluators,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis = false)
        {
            PooledDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> allResults = null;

            foreach ((IOperation Operation, ISymbol ContainingSymbol) in rootOperationsNeedingAnalysis)
            {
                ImmutableDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> dfaResult =
                    PropertySetAnalysis.GetOrComputeHazardousUsages(
                        Operation.GetEnclosingControlFlowGraph(),
                        compilation,
                        ContainingSymbol,
                        typeToTrackMetadataName,
                        constructorMapper,
                        propertyMappers,
                        hazardousUsageEvaluators,
                        interproceduralAnalysisConfig,
                        pessimisticAnalysis);
                if (dfaResult.IsEmpty)
                {
                    continue;
                }

                if (allResults == null)
                {
                    allResults = PooledDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult>.GetInstance();
                }

                foreach (KeyValuePair<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> kvp
                    in dfaResult)
                {
                    if (allResults.TryGetValue(kvp.Key, out HazardousUsageEvaluationResult existingValue))
                    {
                        allResults[kvp.Key] = PropertySetAnalysis.MergeHazardousUsageEvaluationResult(existingValue, kvp.Value);
                    }
                    else
                    {
                        allResults.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return allResults;
        }

        /// <summary>
        /// When there are multiple hazardous usage evaluations for the same exact code, this prioritizes Flagged over MaybeFlagged, and MaybeFlagged over Unflagged.
        /// </summary>
        /// <param name="r1">First evaluation result.</param>
        /// <param name="r2">Second evaluation result.</param>
        /// <returns>Prioritized result.</returns>
        public static HazardousUsageEvaluationResult MergeHazardousUsageEvaluationResult(HazardousUsageEvaluationResult r1, HazardousUsageEvaluationResult r2)
        {
            if (r1 == HazardousUsageEvaluationResult.Flagged || r2 == HazardousUsageEvaluationResult.Flagged)
            {
                return HazardousUsageEvaluationResult.Flagged;
            }
            else if (r1 == HazardousUsageEvaluationResult.MaybeFlagged || r2 == HazardousUsageEvaluationResult.MaybeFlagged)
            {
                return HazardousUsageEvaluationResult.MaybeFlagged;
            }
            else
            {
                return HazardousUsageEvaluationResult.Unflagged;
            }
        }

        /// <summary>
        /// Enumerates literal values to map to a property set abstract value.
        /// </summary>
        /// <param name="valueContentAbstractValue">Abstract value containing the literal values to examine.</param>
        /// <param name="badLiteralValuePredicate">Predicate function to determine if a literal value is bad.</param>
        /// <returns>Mapped kind.</returns>
        /// <remarks>
        /// Null is not handled by this.  Look at the <see cref="PointsToAbstractValue"/> if you need to treat null as bad.
        /// 
        /// All literal values are bad => Flagged
        /// Some but not all literal are bad => MaybeFlagged
        /// All literal values are known and none are bad => Unflagged
        /// Otherwise => Unknown
        /// </remarks>
        public static PropertySetAbstractValueKind EvaluateLiteralValues(
            ValueContentAbstractValue valueContentAbstractValue,
            Func<object, bool> badLiteralValuePredicate)
        {
            Debug.Assert(valueContentAbstractValue != null);
            Debug.Assert(badLiteralValuePredicate != null);

            switch (valueContentAbstractValue.NonLiteralState)
            {
                case ValueContainsNonLiteralState.No:
                    if (valueContentAbstractValue.LiteralValues.IsEmpty)
                    {
                        return PropertySetAbstractValueKind.Unflagged;
                    }

                    bool allValuesBad = true;
                    bool someValuesBad = false;
                    foreach (object literalValue in valueContentAbstractValue.LiteralValues)
                    {
                        if (badLiteralValuePredicate(literalValue))
                        {
                            someValuesBad = true;
                        }
                        else
                        {
                            allValuesBad = false;
                        }

                        if (!allValuesBad && someValuesBad)
                        {
                            break;
                        }
                    }

                    if (allValuesBad)
                    {
                        // We know all values are bad, so we can say Flagged.
                        return PropertySetAbstractValueKind.Flagged;
                    }
                    else if (someValuesBad)
                    {
                        // We know all values but some values are bad, so we can say MaybeFlagged.
                        return PropertySetAbstractValueKind.MaybeFlagged;
                    }
                    else
                    {
                        // We know all values are good, so we can say Unflagged.
                        return PropertySetAbstractValueKind.Unflagged;
                    }

                case ValueContainsNonLiteralState.Maybe:
                    if (valueContentAbstractValue.LiteralValues.Any(badLiteralValuePredicate))
                    {
                        // We don't know all values but know some values are bad, so we can say MaybeFlagged.
                        return PropertySetAbstractValueKind.MaybeFlagged;
                    }
                    else
                    {
                        // We don't know all values but didn't find any bad value, so we can say who knows.
                        return PropertySetAbstractValueKind.Unknown;
                    }

                default:
                    return PropertySetAbstractValueKind.Unknown;
            }
        }

        private static PropertySetAnalysisResult TryGetOrComputeResultForAnalysisContext(PropertySetAnalysisContext analysisContext)
        {
            var operationVisitor = new PropertySetDataFlowOperationVisitor(analysisContext);
            var analysis = new PropertySetAnalysis(PropertySetAnalysisDomainInstance, operationVisitor);
            return analysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override PropertySetAnalysisResult ToResult(
            PropertySetAnalysisContext analysisContext,
            DataFlowAnalysisResult<PropertySetBlockAnalysisResult, PropertySetAbstractValue> dataFlowAnalysisResult)
        {
            return new PropertySetAnalysisResult(
                dataFlowAnalysisResult,
                ((PropertySetDataFlowOperationVisitor)this.OperationVisitor).HazardousUsages);
        }

        protected override PropertySetBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, PropertySetAnalysisData blockAnalysisData)
            => new PropertySetBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
