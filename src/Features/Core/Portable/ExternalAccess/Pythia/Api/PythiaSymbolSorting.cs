// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaSymbolSorting
    {
        public static ImmutableArray<TSymbol> Sort<TSymbol>(
            ImmutableArray<TSymbol> symbols,
            ISymbolDisplayService symbolDisplayService,
            SemanticModel semanticModel,
            int position)
            where TSymbol : ISymbol
            => Shared.Extensions.ISymbolExtensions2.Sort(symbols, symbolDisplayService, semanticModel, position);
    }
}
