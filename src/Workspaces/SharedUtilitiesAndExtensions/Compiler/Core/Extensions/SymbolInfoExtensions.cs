// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    // Note - these methods are called in fairly hot paths in the IDE, so we try to be responsible about allocations.
    internal static class SymbolInfoExtensions
    {
        public static ImmutableArray<ISymbol> GetAllSymbols(this SymbolInfo info)
            => GetAllSymbolsWorker(info).Distinct();

        private static ImmutableArray<ISymbol> GetAllSymbolsWorker(this SymbolInfo info)
        {
            if (info.Symbol == null)
            {
                return info.CandidateSymbols;
            }
            else
            {
                var builder = ArrayBuilder<ISymbol>.GetInstance(info.CandidateSymbols.Length + 1);
                builder.Add(info.Symbol);
                builder.AddRange(info.CandidateSymbols);
                return builder.ToImmutableAndFree();
            }
        }

        public static ISymbol? GetAnySymbol(this SymbolInfo info)
            => info.Symbol ?? info.CandidateSymbols.FirstOrDefault();

        public static ImmutableArray<ISymbol> GetBestOrAllSymbols(this SymbolInfo info)
        {
            if (info.Symbol != null)
            {
                return ImmutableArray.Create(info.Symbol);
            }
            else if (info.CandidateSymbols.Length > 0)
            {
                return info.CandidateSymbols;
            }

            return ImmutableArray<ISymbol>.Empty;
        }
    }
}
