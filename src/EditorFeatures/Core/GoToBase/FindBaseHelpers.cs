// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols.FindReferences;

namespace Microsoft.CodeAnalysis.Editor.GoToBase
{
    internal static class FindBaseHelpers
    {
        public static ImmutableArray<ISymbol> FindBases(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol &&
                (namedTypeSymbol.TypeKind == TypeKind.Class ||
                namedTypeSymbol.TypeKind == TypeKind.Interface ||
                namedTypeSymbol.TypeKind == TypeKind.Struct))
            {
                return BaseTypeFinder.FindBaseTypesAndInterfaces(namedTypeSymbol);
            }
            else if (symbol.Kind == SymbolKind.Property ||
                symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Event)
            {
                return BaseTypeFinder.FindOverriddenAndImplementedMembers(
                    symbol, project, cancellationToken);
            }
            else
            {
                return ImmutableArray<ISymbol>.Empty;
            }
        }
    }
}
