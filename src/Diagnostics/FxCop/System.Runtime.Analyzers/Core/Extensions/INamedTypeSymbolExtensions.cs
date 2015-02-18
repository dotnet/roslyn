// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Runtime.Analyzers
{
    internal static class INamedTypeSymbolExtensions
    {
        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
