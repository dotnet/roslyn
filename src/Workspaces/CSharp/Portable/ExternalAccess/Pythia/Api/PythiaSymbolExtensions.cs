// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaSymbolExtensions
    {
        public static string ToNameDisplayString(this ISymbol symbol)
            => Shared.Extensions.ISymbolExtensions.ToNameDisplayString(symbol);

        public static ITypeSymbol? GetMemberType(this ISymbol symbol)
            => Shared.Extensions.ISymbolExtensions.GetMemberType(symbol);

        public static ITypeSymbol? GetSymbolType(this ISymbol? symbol)
            => Shared.Extensions.ISymbolExtensions.GetSymbolType(symbol);

        public static ISymbol? GetAnySymbol(this SymbolInfo info)
            => Shared.Extensions.SymbolInfoExtensions.GetAnySymbol(info);

        public static ImmutableArray<T> FilterToVisibleAndBrowsableSymbols<T>(this ImmutableArray<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
            => Shared.Extensions.ISymbolExtensions.FilterToVisibleAndBrowsableSymbols(symbols, hideAdvancedMembers, compilation);

        public static bool IsAccessibleWithin(this ISymbol symbol, ISymbol within, ITypeSymbol? throughType = null)
            => Shared.Extensions.ISymbolExtensions.IsAccessibleWithin(symbol, within, throughType);

        public static bool? IsMoreSpecificThan(this IMethodSymbol method1, IMethodSymbol method2)
            => Shared.Extensions.IMethodSymbolExtensions.IsMoreSpecificThan(method1, method2);

        public static ISymbol? GetOriginalUnreducedDefinition(this ISymbol? symbol)
            => Shared.Extensions.ISymbolExtensions.GetOriginalUnreducedDefinition(symbol);

        public static bool IsAwaitableNonDynamic(this ISymbol? symbol, SemanticModel semanticModel, int position)
            => Shared.Extensions.ISymbolExtensions.IsAwaitableNonDynamic(symbol, semanticModel, position);

        public static bool IsExtensionMethod(this ISymbol symbol)
            => Shared.Extensions.ISymbolExtensions.IsExtensionMethod(symbol);
    }
}
