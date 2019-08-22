// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using PointsToChecksAndTargets = ImmutableDictionary<IsInvocationTaintedWithPointsToAnalysis, ImmutableHashSet<string>>;
    using ValueContentChecksAndTargets = ImmutableDictionary<IsInvocationTaintedWithValueContentAnalysis, ImmutableHashSet<string>>;

    internal static class TaintedDataSymbolMapExtensions
    {
        /// <summary>
        /// Determines if the given method is a tainted data source and get the tainted target set.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="method"></param>
        /// <param name="arguments"></param>
        /// <param name="argumentPointsTos"></param>
        /// <param name="argumentValueContents"></param>
        /// <param name="taintedTargets"></param>
        /// <returns></returns>
        public static bool IsSourceMethod(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            ImmutableArray<IArgumentOperation> arguments,
            IEnumerable<PointsToAbstractValue> argumentPointsTos,
            IEnumerable<ValueContentAbstractValue> argumentValueContents,
            out PooledHashSet<string> taintedTargets)
        {
            taintedTargets = null;
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                if (sourceInfo.TaintedMethodsNeedsPointsToAnalysis.TryGetValue(method.MetadataName, out PointsToChecksAndTargets pointsToChecksAndTargets))
                {
                    foreach (KeyValuePair<IsInvocationTaintedWithPointsToAnalysis, ImmutableHashSet<string>> kvp in pointsToChecksAndTargets)
                    {
                        if (argumentPointsTos != null && kvp.Key(arguments, argumentPointsTos))
                        {
                            if (taintedTargets == null)
                            {
                                taintedTargets = PooledHashSet<string>.GetInstance();
                            }

                            taintedTargets.UnionWith(kvp.Value);
                        }
                    }
                }

                if (sourceInfo.TaintedMethodsNeedsValueContentAnalysis.TryGetValue(method.MetadataName, out ValueContentChecksAndTargets valueContentChecksAndTargets))
                {
                    foreach (KeyValuePair<IsInvocationTaintedWithValueContentAnalysis, ImmutableHashSet<string>> kvp in valueContentChecksAndTargets)
                    {
                        if (argumentPointsTos != null && argumentValueContents != null && kvp.Key(arguments, argumentPointsTos, argumentValueContents))
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
    }
}
