// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaSymbolSorting
    {
#pragma warning disable IDE0060 // Remove unused parameter - Avoid breaking change for ExternalAccess API.
        public static ImmutableArray<TSymbol> Sort<TSymbol>(
            ImmutableArray<TSymbol> symbols,
            ISymbolDisplayService symbolDisplayService,
            SemanticModel semanticModel,
            int position)
            where TSymbol : ISymbol
            => Shared.Extensions.ISymbolExtensions2.Sort(symbols, semanticModel, position);
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
