// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SymbolInfoFactory
    {
        internal static SymbolInfo Create(ImmutableArray<Symbol> symbols, LookupResultKind resultKind, bool isDynamic)
            => Create(OneOrMany.Create(symbols.NullToEmpty()), resultKind, isDynamic);

        internal static SymbolInfo Create(OneOrMany<Symbol> symbols, LookupResultKind resultKind, bool isDynamic)
        {
            if (isDynamic)
            {
                if (symbols.Count == 1)
                {
                    return new SymbolInfo(symbols[0].GetPublicSymbol(), CandidateReason.LateBound);
                }
                else
                {
                    return new SymbolInfo(getPublicSymbols(symbols), CandidateReason.LateBound);
                }
            }
            else if (resultKind == LookupResultKind.Viable)
            {
                if (symbols.Count > 0)
                {
                    Debug.Assert(symbols.Count == 1);
                    return new SymbolInfo(symbols[0].GetPublicSymbol());
                }
                else
                {
                    return SymbolInfo.None;
                }
            }
            else
            {
                return new SymbolInfo(getPublicSymbols(symbols), (symbols.Count > 0) ? resultKind.ToCandidateReason() : CandidateReason.None);
            }

            static ImmutableArray<ISymbol> getPublicSymbols(OneOrMany<Symbol> symbols)
            {
                var result = ArrayBuilder<ISymbol>.GetInstance(symbols.Count);
                foreach (var symbol in symbols)
                    result.Add(symbol.GetPublicSymbol());

                return result.ToImmutableAndFree();
            }
        }
    }
}
