// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;

namespace Microsoft.CodeAnalysis.Editor.GoToBase
{
    internal static class FindBaseHelpers
    {
        public static ValueTask<ImmutableArray<ISymbol>> FindBasesAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol &&
                (namedTypeSymbol.TypeKind == TypeKind.Class ||
                namedTypeSymbol.TypeKind == TypeKind.Interface ||
                namedTypeSymbol.TypeKind == TypeKind.Struct))
            {
                return new ValueTask<ImmutableArray<ISymbol>>(BaseTypeFinder.FindBaseTypesAndInterfaces(namedTypeSymbol));
            }
            else if (symbol.Kind == SymbolKind.Property ||
                symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Event)
            {
                return BaseTypeFinder.FindOverriddenAndImplementedMembersAsync(symbol, solution, cancellationToken);
            }
            else
            {
                return new ValueTask<ImmutableArray<ISymbol>>(ImmutableArray<ISymbol>.Empty);
            }
        }
    }
}
