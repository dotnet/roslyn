// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class TaintedDataSymbolMapExtensions
    {
        /// <summary>
        /// Determines if the given method is a potential tainted data source and get the related argument check methods.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="method"></param>
        /// <param name="evaluateWithPointsToAnalysis"></param>
        /// <param name="evaluateWithValueContentAnalysis"></param>
        /// <returns></returns>
        public static bool IsSourceMethod(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            out PooledHashSet<IsInvocationTaintedWithPointsToAnalysis> evaluateWithPointsToAnalysis,
            out PooledHashSet<IsInvocationTaintedWithValueContentAnalysis> evaluateWithValueContentAnalysis)
        {
            evaluateWithPointsToAnalysis = null;
            evaluateWithValueContentAnalysis = null;
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                if (sourceInfo.TaintedMethodsNeedPointsToAnalysis.TryGetValue(method.MetadataName, out IsInvocationTaintedWithPointsToAnalysis pointsToAnalysisMethod))
                {
                    if (evaluateWithPointsToAnalysis == null)
                    {
                        evaluateWithPointsToAnalysis = PooledHashSet<IsInvocationTaintedWithPointsToAnalysis>.GetInstance();
                    }

                    evaluateWithPointsToAnalysis.Add(pointsToAnalysisMethod);
                }
                else if (sourceInfo.TaintedMethodsNeedsValueContentAnalysis.TryGetValue(method.MetadataName, out IsInvocationTaintedWithValueContentAnalysis valueContentAnalysisMethod))
                {
                    if (evaluateWithValueContentAnalysis == null)
                    {
                        evaluateWithValueContentAnalysis = PooledHashSet<IsInvocationTaintedWithValueContentAnalysis>.GetInstance();
                    }

                    evaluateWithValueContentAnalysis.Add(valueContentAnalysisMethod);
                }
            }

            if (evaluateWithPointsToAnalysis == null && evaluateWithValueContentAnalysis == null)
            {
                return false;
            }
            else
            {
                return true;
            }
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
