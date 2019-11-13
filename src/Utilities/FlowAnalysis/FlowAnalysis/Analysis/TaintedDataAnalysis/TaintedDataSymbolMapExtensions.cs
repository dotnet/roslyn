// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    internal static class TaintedDataSymbolMapExtensions
    {
        /// <summary>
        /// Determines if the given method is a tainted data source and get the tainted target set.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="method"></param>
        /// <param name="arguments"></param>
        /// <param name="pointsToFactory">If the method needs to do PointsToAnalysis, the PointsToAnalysis result will be produced by the passed value factory.</param>
        /// <param name="valueContentFactory">If the method needs to do ValueContentAnalysis, the ValueContentAnalysis result will be produced by the passed value factory.</param>
        /// <param name="allTaintedTargets"></param>
        /// <returns></returns>
        public static bool IsSourceMethod(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            ImmutableArray<IArgumentOperation> arguments,
            Lazy<PointsToAnalysisResult> pointsToFactory,
            Lazy<(PointsToAnalysisResult p, ValueContentAnalysisResult v)> valueContentFactory,
            out PooledHashSet<string> allTaintedTargets)
        {
            allTaintedTargets = null;
            PointsToAnalysisResult pointsToAnalysisResult = null;
            ValueContentAnalysisResult valueContentAnalysisResult = null;
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                foreach ((MethodMatcher methodMatcher, ImmutableHashSet<string> taintedTargets) in sourceInfo.TaintedMethods)
                {
                    if (methodMatcher(method.Name, arguments))
                    {
                        if (allTaintedTargets == null)
                        {
                            allTaintedTargets = PooledHashSet<string>.GetInstance();
                        }

                        allTaintedTargets.UnionWith(taintedTargets);
                    }
                }

                foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(PointsToCheck pointsToCheck, string)> pointsToTaintedTargets) in sourceInfo.TaintedMethodsNeedsPointsToAnalysis)
                {
                    if (pointsToTaintedTargets.Any() && methodMatcher(method.Name, arguments))
                    {
                        pointsToAnalysisResult ??= pointsToFactory.Value;
                        if (pointsToAnalysisResult == null)
                        {
                            break;
                        }

                        IEnumerable<(PointsToCheck, string target)> positivePointsToTaintedTargets = pointsToTaintedTargets.Where(s =>
                            s.pointsToCheck(
                                arguments.Select(o =>
                                    pointsToAnalysisResult[o.Kind, o.Syntax]).ToImmutableArray()));
                        if (positivePointsToTaintedTargets.Any())
                        {
                            if (allTaintedTargets == null)
                            {
                                allTaintedTargets = PooledHashSet<string>.GetInstance();
                            }

                            allTaintedTargets.UnionWith(positivePointsToTaintedTargets.Select(s => s.target));
                        }
                    }
                }

                foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(ValueContentCheck valueContentCheck, string)> valueContentTaintedTargets) in sourceInfo.TaintedMethodsNeedsValueContentAnalysis)
                {
                    if (valueContentTaintedTargets.Any() && methodMatcher(method.Name, arguments))
                    {
                        pointsToAnalysisResult ??= valueContentFactory.Value.p;
                        valueContentAnalysisResult ??= valueContentFactory.Value.v;
                        if (pointsToAnalysisResult == null || valueContentAnalysisResult == null)
                        {
                            break;
                        }

                        IEnumerable<(ValueContentCheck, string target)> positiveValueContentTaintedTargets = valueContentTaintedTargets.Where(s =>
                            s.valueContentCheck(
                                arguments.Select(o => pointsToAnalysisResult[o.Kind, o.Syntax]).ToImmutableArray(),
                                arguments.Select(o => valueContentAnalysisResult[o.Kind, o.Syntax]).ToImmutableArray()));
                        if (positiveValueContentTaintedTargets.Any())
                        {
                            if (allTaintedTargets == null)
                            {
                                allTaintedTargets = PooledHashSet<string>.GetInstance();
                            }

                            allTaintedTargets.UnionWith(positiveValueContentTaintedTargets.Select(s => s.target));
                        }
                    }
                }
            }

            return allTaintedTargets != null;
        }

        /// <summary>
        /// Determines if the given property is a tainted data source.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="propertySymbol"></param>
        /// <returns></returns>
        public static bool IsSourceProperty(this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap, IPropertySymbol propertySymbol)
        {
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(propertySymbol.ContainingType))
            {
                if (sourceInfo.TaintedProperties.Contains(propertySymbol.MetadataName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the given array can be a tainted data source when its elements are all constant.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="arrayTypeSymbol"></param>
        /// <returns></returns>
        public static bool IsSourceConstantArrayOfType(this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap, IArrayTypeSymbol arrayTypeSymbol)
        {
            if (arrayTypeSymbol.ElementType is INamedTypeSymbol elementType)
            {
                foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(elementType))
                {
                    if (sourceInfo.TaintConstantArray)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the method taints other arguments cause some arguments are tainted.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="method"></param>
        /// <param name="taintedParameterNames"></param>
        /// <param name="taintedParameterPairs">The set of parameter pairs (tainted source parameter name, tainted end parameter name).</param>
        /// <returns></returns>
        public static bool IsSourceTransferMethod(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            ImmutableArray<IArgumentOperation> arguments,
            ImmutableArray<string> taintedParameterNames,
            out PooledHashSet<(string, string)> taintedParameterPairs)
        {
            taintedParameterPairs = null;
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(string source, string end)> sourceToEnds) in sourceInfo.TransferMethods)
                {
                    if (methodMatcher(method.Name, arguments))
                    {
                        if (taintedParameterPairs == null)
                        {
                            taintedParameterPairs = PooledHashSet<(string, string)>.GetInstance();
                        }

                        taintedParameterPairs.UnionWith(sourceToEnds.Where(s => taintedParameterNames.Contains(s.source)));
                    }
                }
            }

            return taintedParameterPairs != null;
        }
    }
}
