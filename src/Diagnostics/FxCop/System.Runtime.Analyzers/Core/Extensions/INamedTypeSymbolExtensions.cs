using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Runtime.Analyzers
{
    internal static class INamedTypeSymbolExtensions
    {
        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
