// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal static class SymbolKeyResolutionExtensions
    {
        internal static ISymbol GetAnySymbol(this SymbolKeyResolution resolution)
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

        internal static IEnumerable<TType> GetAllSymbols<TType>(this SymbolKeyResolution resolution)
            => resolution.GetAllSymbols().OfType<TType>();

        internal static TSymbol GetFirstSymbol<TSymbol>(this SymbolKeyResolution resolution)
            where TSymbol : ISymbol
        {
            return resolution.GetAllSymbols<TSymbol>().FirstOrDefault();
        }
    }
}
