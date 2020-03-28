﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
