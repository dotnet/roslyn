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
                foreach (KeyValuePair<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>> methodNeedsPointsToAnalysis in sourceInfo.TaintedMethodsNeedsPointsToAnalysis)
                {
                    if (methodNeedsPointsToAnalysis.Key(method.Name, arguments))
                    {
                        foreach (KeyValuePair<PointsToCheck, ImmutableHashSet<string>> kvp in methodNeedsPointsToAnalysis.Value)
                        {
                            if (kvp.Key(pointsTos))
                            {
                                if (taintedTargets == null)
                                {
                                    taintedTargets = PooledHashSet<string>.GetInstance();
                                }

                                taintedTargets.UnionWith(kvp.Value);
                            }
                        }
                    }
                }

                foreach (KeyValuePair<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>> methodNeedsValueContentAnalysis in sourceInfo.TaintedMethodsNeedsValueContentAnalysis)
                {
                    if (methodNeedsValueContentAnalysis.Key(method.Name, arguments))
                    {
                        foreach (KeyValuePair<ValueContentCheck, ImmutableHashSet<string>> kvp in methodNeedsValueContentAnalysis.Value)
                        {
                            if (kvp.Key(pointsTos, valueContents))
                            {
                                if (taintedTargets == null)
                                {
                                    taintedTargets = PooledHashSet<string>.GetInstance();
                                }

                                taintedTargets.UnionWith(kvp.Value);
                            }
                        }
                    }
                }
            }

            return taintedTargets != null;
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
        public static bool IsSourcePasserMethod(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            ImmutableArray<IArgumentOperation> arguments,
            ImmutableArray<string> taintedParameterNames,
            out PooledHashSet<(string, string)> taintedParameterPairs)
        {
            taintedParameterPairs = null;
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                foreach (KeyValuePair<MethodMapper, ImmutableHashSet<(string source, string)>> passerMethod in sourceInfo.PasserMethods)
                {
                    if (passerMethod.Key(method.Name, arguments))
                    {
                        if (taintedParameterPairs == null)
                        {
                            taintedParameterPairs = PooledHashSet<(string, string)>.GetInstance();
                        }

                        taintedParameterPairs.UnionWith(passerMethod.Value.Where(s => taintedParameterNames.Contains(s.source)));
                    }
                }
            }

            return taintedParameterPairs != null;
        }
    }
}
