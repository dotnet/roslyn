// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class TaintedDataSymbolMapExtensions
    {
        /// <summary>
        /// Determines if the given method is a tainted data source and get the tainted target set.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="method"></param>
        /// <param name="arguments"></param>
        /// <param name="pointsTos"></param>
        /// <param name="valueContents"></param>
        /// <param name="taintedTargets"></param>
        /// <returns></returns>
        public static bool IsSourceMethod(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            ImmutableArray<IArgumentOperation> arguments,
            ImmutableArray<PointsToAbstractValue> pointsTos,
            ImmutableArray<ValueContentAbstractValue> valueContents,
            out PooledHashSet<string> taintedTargets)
        {
            taintedTargets = null;
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(PointsToCheck pointsToCheck, string)> pointsToTaintedTargets) in sourceInfo.TaintedMethodsNeedsPointsToAnalysis)
                {
                    if (methodMatcher(method.Name, arguments))
                    {
                        IEnumerable<(PointsToCheck, string target)> positivePointsToTaintedTargets = pointsToTaintedTargets.Where(s => s.pointsToCheck(pointsTos));
                        if (positivePointsToTaintedTargets != null)
                        {
                            if (taintedTargets == null)
                            {
                                taintedTargets = PooledHashSet<string>.GetInstance();
                            }

                            taintedTargets.UnionWith(positivePointsToTaintedTargets.Select(s => s.target));
                        }
                    }
                }

                foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(ValueContentCheck valueContentCheck, string)> valueContentTaintedTargets) in sourceInfo.TaintedMethodsNeedsValueContentAnalysis)
                {
                    if (methodMatcher(method.Name, arguments))
                    {
                        IEnumerable<(ValueContentCheck, string target)> positiveValueContentTaintedTargets = valueContentTaintedTargets.Where(s => s.valueContentCheck(pointsTos, valueContents));
                        if (positiveValueContentTaintedTargets != null)
                        {
                            if (taintedTargets == null)
                            {
                                taintedTargets = PooledHashSet<string>.GetInstance();
                            }

                            taintedTargets.UnionWith(positiveValueContentTaintedTargets.Select(s => s.target));
                        }
                    }
                }
            }

            return taintedTargets != null;
        }

        /// <summary>
        /// Faster IsSourceMethod(), before using PointsToAnalysis or ValueContentAnalysis.
        /// </summary>
        /// <param name="sourceSymbolMap">SourceInfos.</param>
        /// <param name="method">Invoked ethod to be evaluated.</param>
        /// <param name="arguments">Arguments of the method.</param>
        /// <param name="isSourceMethod">Indicates that the invoked method is definitely a tainted data source.</param>
        /// <param name="requiresPointsTo">Indicates that the invoked method requires PointsToAnalysis for further
        /// evaluation.</param>
        /// <param name="requiresValueContent">Indicates that the invoked method requires ValueContentAnalysis for further
        /// evaluation.</param>
        /// <returns>True if the invoked method is potentially a tainted data source.</returns>
        public static bool IsSourceMethodFast(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            ImmutableArray<IArgumentOperation> arguments,
            out bool isSourceMethod,
            out bool requiresPointsTo,
            out bool requiresValueContent)
        {
            isSourceMethod = false;
            requiresPointsTo = false;
            requiresValueContent = false;

            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                if (!(requiresPointsTo && isSourceMethod))
                {
                    foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(PointsToCheck pointsToCheck, string)> pointsToTaintedTargets) in sourceInfo.TaintedMethodsNeedsPointsToAnalysis)
                    {
                        if (methodMatcher(method.Name, arguments))
                        {
                            foreach ((PointsToCheck pointsToCheck, string) p in pointsToTaintedTargets)
                            {
                                if (p.pointsToCheck == SourceInfo.AlwaysTruePointsToCheck)
                                {
                                    isSourceMethod = true;
                                }
                                else
                                {
                                    requiresPointsTo = true;
                                }
                            }
                        }
                    }
                }

                if (!requiresValueContent)
                {
                    foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(ValueContentCheck valueContentCheck, string)> valueContentTaintedTargets) in sourceInfo.TaintedMethodsNeedsValueContentAnalysis)
                    {
                        if (methodMatcher(method.Name, arguments))
                        {
                            requiresValueContent = true;
                        }
                    }
                }

                if (requiresPointsTo && requiresValueContent && isSourceMethod)
                {
                    break;
                }
            }

            return isSourceMethod || requiresPointsTo || requiresValueContent;
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
