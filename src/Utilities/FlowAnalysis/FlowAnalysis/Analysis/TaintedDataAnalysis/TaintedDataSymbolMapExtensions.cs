// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis.SourceInfo;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class TaintedDataSymbolMapExtensions
    {
        /// <summary>
        /// Determines if the given method is a potential tainted data source.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool IsSourceMethod(this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap, IMethodSymbol method)
        {
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                if (sourceInfo.TaintedMethods.Keys.Contains(method.MetadataName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the given method is a tainted data source and all the arguments meet the requirement.
        /// </summary>
        /// <param name="sourceSymbolMap"></param>
        /// <param name="method"></param>
        /// <param name="argumentValues"></param>
        /// <returns></returns>
        public static bool IsSourceMethod(this TaintedDataSymbolMap<SourceInfo> sourceSymbolMap, IMethodSymbol method, IEnumerable<(string parameterName, ImmutableHashSet<object> argumentValues)> argumentValues)
        {
            foreach (SourceInfo sourceInfo in sourceSymbolMap.GetInfosForType(method.ContainingType))
            {
                if (sourceInfo.TaintedMethods.TryGetValue(method.MetadataName, out var argumentChecks))
                {
                    foreach (KeyValuePair<string, ArgumentCheck> kvp in argumentChecks)
                    {
                        if (!kvp.Value(argumentValues.FirstOrDefault(o => o.parameterName == kvp.Key).argumentValues))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
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
