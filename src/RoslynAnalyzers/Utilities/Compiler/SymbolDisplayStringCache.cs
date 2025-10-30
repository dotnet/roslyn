// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private static readonly BoundedCacheWithFactory<Compilation, ConcurrentDictionary<SymbolDisplayFormat, SymbolDisplayStringCache>> s_byCompilationCache = new();

        /// <summary>
        /// ConcurrentDictionary key for a null SymbolDisplayFormat.
        /// </summary>
        private static readonly SymbolDisplayFormat NullSymbolDisplayFormat = new();

        /// <summary>
        /// Mapping of a symbol to its ToDisplayString().
        /// </summary>
        private readonly ConcurrentDictionary<ISymbol, string> SymbolToDisplayNames = new();

        private readonly SymbolDisplayFormat? Format;

        /// <summary>
        /// Privately constructs.
        /// </summary>
        /// <param name="format">SymbolDisplayFormat to use, or null for the default.</param>
        private SymbolDisplayStringCache(SymbolDisplayFormat? format = null)
        {
            this.Format = Object.ReferenceEquals(format, NullSymbolDisplayFormat) ? null : format;
        }

        /// <summary>
        /// Gets the symbol display string cache for the compilation.
        /// </summary>
        /// <param name="compilation">Compilation that this cache is for.</param>
        /// <param name="format">A singleton SymbolDisplayFormat to use, or null for the default.</param>
        /// <returns>A SymbolDisplayStringCache.</returns>
        public static SymbolDisplayStringCache GetOrCreate(Compilation compilation, SymbolDisplayFormat? format = null)
        {
            ConcurrentDictionary<SymbolDisplayFormat, SymbolDisplayStringCache> dict =
                s_byCompilationCache.GetOrCreateValue(compilation, CreateConcurrentDictionary);
            return dict.GetOrAdd(format ?? NullSymbolDisplayFormat, CreateSymbolDisplayStringCache);

            // Local functions
            static ConcurrentDictionary<SymbolDisplayFormat, SymbolDisplayStringCache> CreateConcurrentDictionary(Compilation compilation)
                => new();
            static SymbolDisplayStringCache CreateSymbolDisplayStringCache(SymbolDisplayFormat? format) => new(format);
        }

        /// <summary>
        /// Gets the symbol's display string.
        /// </summary>
        /// <param name="symbol">Symbol to get the display string.</param>
        /// <returns>The symbol's display string.</returns>
        public string GetDisplayString(ISymbol symbol)
        {
            return this.SymbolToDisplayNames.GetOrAdd(symbol, s => s.ToDisplayString(this.Format));
        }
    }
}
