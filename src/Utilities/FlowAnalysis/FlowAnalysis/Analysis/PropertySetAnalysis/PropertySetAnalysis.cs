// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

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
        /// Analyzers should use <see cref="BatchGetOrComputeHazardousUsages"/> instead.  Gets hazardous usages of an object based on a set of its properties.
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
        /// <returns>Property set analysis result.</returns>
        internal static PropertySetAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
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

            constructorMapper.Validate(propertyMappers.PropertyValuesCount);

            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);

            PointsToAnalysisResult pointsToAnalysisResult;
            ValueContentAnalysisResult valueContentAnalysisResultOpt;
            if (!constructorMapper.RequiresValueContentAnalysis && !propertyMappers.RequiresValueContentAnalysis)
            {
                pointsToAnalysisResult = PointsToAnalysis.TryGetOrComputeResult(
                    cfg,
                    owningSymbol,
                    analyzerOptions,
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
                    analyzerOptions,
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
                analyzerOptions,
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
            return result;
        }

        /// <summary>
        /// Gets hazardous usages of an object based on a set of its properties.
        /// </summary>
        /// <param name="compilation">Compilation containing the code.</param>
        /// <param name="rootOperationsNeedingAnalysis">Root operations of code blocks to analyze.</param>
        /// <param name="typeToTrackMetadataName">Name of the type to track.</param>
        /// <param name="constructorMapper">How constructor invocations map to <see cref="PropertySetAbstractValueKind"/>s.</param>
        /// <param name="propertyMappers">How property assignments map to <see cref="PropertySetAbstractValueKind"/>.</param>
        /// <param name="hazardousUsageEvaluators">When and how to evaluate <see cref="PropertySetAbstractValueKind"/>s to for hazardous usages.</param>
        /// <param name="interproceduralAnalysisConfig">Interprocedural dataflow analysis configuration.</param>
        /// <param name="pessimisticAnalysis">Whether to be pessimistic.</param>
        /// <returns>Dictionary of <see cref="Location"/> and <see cref="IMethodSymbol"/> pairs mapping to the kind of hazardous usage (Flagged or MaybeFlagged).  The method in the key is null for return/initialization statements.</returns>
        /// <remarks>Unlike <see cref="GetOrComputeResult"/>, this overload also performs DFA on all descendant local and anonymous functions.</remarks>
        public static PooledDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> BatchGetOrComputeHazardousUsages(
            Compilation compilation,
            IEnumerable<(IOperation Operation, ISymbol ContainingSymbol)> rootOperationsNeedingAnalysis,
            AnalyzerOptions analyzerOptions,
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
                ControlFlowGraph enclosingControlFlowGraph = Operation.GetEnclosingControlFlowGraph();
                PropertySetAnalysisResult enclosingResult = InvokeDfaAndAccumulateResults(
                    enclosingControlFlowGraph,
                    ContainingSymbol);
                if (enclosingResult == null)
                {
                    continue;
                }

                // Also look at local functions and lambdas that weren't visited via interprocedural analysis.
                foreach (IMethodSymbol localFunctionSymbol in enclosingControlFlowGraph.LocalFunctions)
                {
                    if (!enclosingResult.VisitedLocalFunctions.Contains(localFunctionSymbol))
                    {
                        InvokeDfaAndAccumulateResults(
                            enclosingControlFlowGraph.GetLocalFunctionControlFlowGraph(localFunctionSymbol),
                            localFunctionSymbol);
                    }
                }

                foreach (IFlowAnonymousFunctionOperation flowAnonymousFunctionOperation in
                    enclosingControlFlowGraph.DescendantOperations<IFlowAnonymousFunctionOperation>(
                        OperationKind.FlowAnonymousFunction))
                {
                    if (!enclosingResult.VisitedLambdas.Contains(flowAnonymousFunctionOperation))
                    {
                        InvokeDfaAndAccumulateResults(
                            enclosingControlFlowGraph.GetAnonymousFunctionControlFlowGraph(flowAnonymousFunctionOperation),
                            flowAnonymousFunctionOperation.Symbol);
                    }
                }
            }

            return allResults;

            // Merges results from single PropertySet DFA invocation into allResults.
            PropertySetAnalysisResult InvokeDfaAndAccumulateResults(ControlFlowGraph cfg, ISymbol owningSymbol)
            {
                PropertySetAnalysisResult propertySetAnalysisResult =
                    PropertySetAnalysis.GetOrComputeResult(
                        cfg,
                        compilation,
                        owningSymbol,
                        analyzerOptions,
                        typeToTrackMetadataName,
                        constructorMapper,
                        propertyMappers,
                        hazardousUsageEvaluators,
                        interproceduralAnalysisConfig,
                        pessimisticAnalysis);
                if (propertySetAnalysisResult == null || propertySetAnalysisResult.HazardousUsages.IsEmpty)
                {
                    return propertySetAnalysisResult;
                }

                if (allResults == null)
                {
                    allResults = PooledDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult>.GetInstance();
                }

                foreach (KeyValuePair<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> kvp
                    in propertySetAnalysisResult.HazardousUsages)
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

                return propertySetAnalysisResult;
            }
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
            PropertySetDataFlowOperationVisitor visitor = (PropertySetDataFlowOperationVisitor)this.OperationVisitor;
            visitor.ProcessExitBlock(dataFlowAnalysisResult.ExitBlockOutput);
            return new PropertySetAnalysisResult(
                dataFlowAnalysisResult,
                visitor.HazardousUsages,
                visitor.VisitedLocalFunctions,
                visitor.VisitedLambdas);
        }

        protected override PropertySetBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, PropertySetAnalysisData blockAnalysisData)
            => new PropertySetBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
