// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

internal static class PythiaSymbolExtensions
{
    extension(ISymbol symbol)
    {
        public string ToNameDisplayString()
        => Shared.Extensions.ISymbolExtensions.ToNameDisplayString(symbol);

        public ITypeSymbol? GetMemberType()
            => Shared.Extensions.ISymbolExtensions.GetMemberType(symbol);

        public bool IsAccessibleWithin(ISymbol within, ITypeSymbol? throughType = null)
            => Shared.Extensions.ISymbolExtensions.IsAccessibleWithin(symbol, within, throughType);

        public bool IsExtensionMethod()
            => Shared.Extensions.ISymbolExtensions.IsExtensionMethod(symbol);
    }

    extension(ISymbol? symbol)
    {
        public ITypeSymbol? GetSymbolType()
        => Shared.Extensions.ISymbolExtensions.GetSymbolType(symbol);

        public ISymbol? GetOriginalUnreducedDefinition()
            => Shared.Extensions.ISymbolExtensions.GetOriginalUnreducedDefinition(symbol);

        public bool IsAwaitableNonDynamic(SemanticModel semanticModel, int position)
            => Shared.Extensions.ISymbolExtensions.IsAwaitableNonDynamic(symbol, semanticModel, position);
    }

    extension(SymbolInfo info)
    {
        public ISymbol? GetAnySymbol()
        => Shared.Extensions.SymbolInfoExtensions.GetAnySymbol(info);
    }

    extension<T>(ImmutableArray<T> symbols) where T : ISymbol
    {
        public ImmutableArray<T> FilterToVisibleAndBrowsableSymbols(bool hideAdvancedMembers, Compilation compilation) => Shared.Extensions.ISymbolExtensions.FilterToVisibleAndBrowsableSymbols(symbols, hideAdvancedMembers, compilation, inclusionFilter: static s => true);
    }

    extension(IMethodSymbol method1)
    {
        public bool? IsMoreSpecificThan(IMethodSymbol method2)
        => Shared.Extensions.IMethodSymbolExtensions.IsMoreSpecificThan(method1, method2);
    }
}
