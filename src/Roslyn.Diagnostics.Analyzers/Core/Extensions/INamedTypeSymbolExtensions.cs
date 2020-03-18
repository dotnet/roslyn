// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Roslyn.Diagnostics.Analyzers.Extensions
{
    internal static class INamedTypeSymbolExtensions
    {
        public static bool IsTestAttribute(this INamedTypeSymbol attributeClass, ConcurrentDictionary<INamedTypeSymbol, bool> knownTestAttributes, INamedTypeSymbol factAttribute)
        {
            if (knownTestAttributes.TryGetValue(attributeClass, out var isTest))
                return isTest;

            return knownTestAttributes.GetOrAdd(attributeClass, ExtendsFactAttribute(attributeClass, factAttribute));
        }

        private static bool ExtendsFactAttribute(INamedTypeSymbol namedType, INamedTypeSymbol factAttribute)
        {
            Debug.Assert(factAttribute is object);
            for (var current = namedType; current is object; current = current.BaseType)
            {
                if (Equals(current, factAttribute))
                    return true;
            }

            return false;
        }
    }
}
