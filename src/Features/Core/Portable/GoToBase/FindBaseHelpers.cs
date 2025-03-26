// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;

namespace Microsoft.CodeAnalysis.GoToBase;

internal static class FindBaseHelpers
{
    public static ImmutableArray<ISymbol> FindBases(
        ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        if (symbol is INamedTypeSymbol
            {
                TypeKind: TypeKind.Class or TypeKind.Interface or TypeKind.Struct,
            } namedTypeSymbol)
        {
            var result = BaseTypeFinder.FindBaseTypesAndInterfaces(namedTypeSymbol).CastArray<ISymbol>();
            return result;
        }

        if (symbol.Kind is SymbolKind.Property or SymbolKind.Method or SymbolKind.Event)
            return BaseTypeFinder.FindOverriddenAndImplementedMembers(symbol, solution, cancellationToken);

        return [];
    }
}
