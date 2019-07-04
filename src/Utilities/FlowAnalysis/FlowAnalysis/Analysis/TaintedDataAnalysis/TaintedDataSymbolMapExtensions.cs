// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        /// <param name="evaluateWithValueContentAnslysis"></param>
        /// <returns></returns>
        public static bool IsSourceMethod(
            this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap,
            IMethodSymbol method,
            out HashSet<IsInvocationTaintedWithPointsToAnalysis> evaluateWithPointsToAnalysis,
            out HashSet<IsInvocationTaintedWithValueContentAnalysis> evaluateWithValueContentAnslysis)
        {
            evaluateWithPointsToAnalysis = new HashSet<IsInvocationTaintedWithPointsToAnalysis>();
            evaluateWithValueContentAnslysis = new HashSet<IsInvocationTaintedWithValueContentAnalysis>(); ;
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                if (sourceInfo.TaintedMethodsNeedPointsToAnalysis.TryGetValue(method.MetadataName, out var pointsToAnalysisMethod))
                {
                    evaluateWithPointsToAnalysis.Add(pointsToAnalysisMethod);
                }
                else if (sourceInfo.TaintedMethodsNeedsValueContentAnalysis.TryGetValue(method.MetadataName, out var valueContentAnalysisMethod))
                {
                    evaluateWithValueContentAnslysis.Add(valueContentAnalysisMethod);
                }
            }

            if (evaluateWithPointsToAnalysis.Count == 0 && evaluateWithValueContentAnslysis.Count == 0)
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
        /// Determines if the given array is a tainted data source.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="arrayTypeSymbol"></param>
        /// <returns></returns>
        public static bool IsSourceArray(this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap, IArrayTypeSymbol arrayTypeSymbol)
        {
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(arrayTypeSymbol.ElementType as INamedTypeSymbol))
            {
                if (sourceInfo.TaintConstantArray)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
