// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class INamedTypeSymbolExtensions
    {
        public static IEnumerable<ITypeSymbol> GetAllTypeArguments(this INamedTypeSymbol? symbol)
        {
            var stack = GetContainmentStack(symbol);
            return stack.SelectMany(n => n.TypeArguments);
        }

        private static Stack<INamedTypeSymbol> GetContainmentStack(INamedTypeSymbol? symbol)
        {
            var stack = new Stack<INamedTypeSymbol>();
            for (var current = symbol; current != null; current = current.ContainingType)
            {
                stack.Push(current);
            }

            return stack;
        }

        public static INamedTypeSymbol TryConstruct(this INamedTypeSymbol type, ITypeSymbol[] typeArguments)
        {
            return typeArguments.Length > 0 ? type.Construct(typeArguments) : type;
        }
    }
}
