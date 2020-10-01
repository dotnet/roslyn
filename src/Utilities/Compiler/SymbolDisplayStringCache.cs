// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Cache ISymbol.ToDisplayName() results, to avoid performance concerns.
    /// </summary>
    internal sealed class SymbolDisplayStringCache
    {
        /// <summary>
        /// Caches by compilation.
        /// </summary>
        private static readonly BoundedCacheWithFactory<Compilation, SymbolDisplayStringCache> s_byCompilationCache =
            new BoundedCacheWithFactory<Compilation, SymbolDisplayStringCache>();

        /// <summary>
        /// Mapping of a symbol to its ToDisplayString().
        /// </summary>
        private readonly ConcurrentDictionary<ISymbol, string> SymbolToDisplayNames =
            new ConcurrentDictionary<ISymbol, string>();

        /// <summary>
        /// Doesn't construct.
        /// </summary>
        private SymbolDisplayStringCache()
        {
        }

        /// <summary>
        /// Gets the symbol display string cache for the compilation.
        /// </summary>
        /// <param name="compilation">Compilation that this cache is for.</param>
        /// <returns>A SymbolDisplayStringCache.</returns>
        public static SymbolDisplayStringCache GetOrCreate(Compilation compilation)
        {
            return s_byCompilationCache.GetOrCreateValue(compilation, CreateSymbolDisplayNameCache);

            // Local functions
            static SymbolDisplayStringCache CreateSymbolDisplayNameCache(Compilation compilation)
                => new SymbolDisplayStringCache();
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
