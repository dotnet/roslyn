// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

// Note - these methods are called in fairly hot paths in the IDE, so we try to be responsible about allocations.
internal static class SymbolInfoExtensions
{
    public static ImmutableArray<ISymbol> GetAllSymbols(this SymbolInfo info)
        => GetAllSymbolsWorker(info).Distinct();

    private static ImmutableArray<ISymbol> GetAllSymbolsWorker(this SymbolInfo info)
        => info.Symbol == null ? info.CandidateSymbols : info.CandidateSymbols.Insert(0, info.Symbol);

    public static ISymbol? GetAnySymbol(this SymbolInfo info)
        => info.Symbol ?? info.CandidateSymbols.FirstOrDefault();

    public static ImmutableArray<ISymbol> GetBestOrAllSymbols(this SymbolInfo info)
    {
        if (info.Symbol != null)
            return [info.Symbol];

        if (info.CandidateSymbols.Contains(null!))
        {
            using var result = TemporaryArray<ISymbol>.Empty;
            foreach (var symbol in info.CandidateSymbols)
                result.AsRef().AddIfNotNull(symbol);

            return result.ToImmutableAndClear();
        }

        return info.CandidateSymbols;
    }
}
