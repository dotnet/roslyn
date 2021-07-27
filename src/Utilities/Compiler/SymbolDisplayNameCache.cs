// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Cache ISymbol.ToDisplayName() results, to avoid performance concerns.
    /// </summary>
    public sealed class SymbolDisplayNameCache
    {
        private static readonly BoundedCacheWithFactory<Compilation, SymbolDisplayNameCache> s_byCompilationCache =
            new BoundedCacheWithFactory<Compilation, SymbolDisplayNameCache>();

        /// <summary>
        /// Mapping of a symbol to its ToDisplayString().
        /// </summary>
        private readonly ConcurrentDictionary<ISymbol, string> SymbolToDisplayNames =
            new ConcurrentDictionary<ISymbol, string>();


        private SymbolDisplayNameCache()
        {
        }

        /// <summary>
        /// Gets the symbol display string cache for the compilation.
        /// </summary>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public static SymbolDisplayNameCache GetOrCreate(Compilation compilation)
        {
            return s_byCompilationCache.GetOrCreateValue(compilation, CreateSymbolDisplayNameCache);

            // Local functions
            static SymbolDisplayNameCache CreateSymbolDisplayNameCache(Compilation compilation)
                => new SymbolDisplayNameCache();
        }

        /// <summary>
        /// Gets the symbol's display string.
        /// </summary>
        /// <param name="symbol">Symbol to get the display string.</param>
        /// <returns>The symbol's display string.</returns>
        public string GetDisplayString(ISymbol symbol)
        {
            return this.SymbolToDisplayNames.GetOrAdd(symbol, s => s.ToDisplayString());
        }
    }
}
