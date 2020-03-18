// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace Roslyn.Diagnostics.Analyzers.Extensions
{
    internal static class IMethodSymbolExtensions
    {
        public static bool IsTestMethod(this IMethodSymbol method, ConcurrentDictionary<INamedTypeSymbol, bool> knownTestAttributes, INamedTypeSymbol factAttribute)
        {
            foreach (var attribute in method.GetAttributes())
            {
                if (attribute.AttributeClass.IsTestAttribute(knownTestAttributes, factAttribute))
                    return true;
            }

            return false;
        }
    }
}
