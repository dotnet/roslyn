// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal static class ResolvedSymbolInfoExtensions
    {
        internal static ISymbol GetAnySymbol(this ResolvedSymbolInfo resolution)
        {
            if (resolution.Symbol != null)
            {
                return resolution.Symbol;
            }

            if (resolution.CandidateSymbols.Length > 0)
            {
                return resolution.CandidateSymbols[0];
            }

            return null;
        }

        internal static ImmutableArray<TType> GetAllSymbols<TType>(this ResolvedSymbolInfo resolution)
        {
            var result = ImmutableArray.CreateBuilder<TType>();

            foreach (var symbol in resolution.GetAllSymbols())
            {
                if (symbol is TType typedSymbol)
                {
                    result.Add(typedSymbol);
                }
            }

            return result.ToImmutable();
        }

        internal static TSymbol GetFirstSymbol<TSymbol>(this ResolvedSymbolInfo resolution)
            where TSymbol : ISymbol
        {
            return resolution.GetAllSymbols<TSymbol>().FirstOrDefault();
        }
    }
}
