// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// <see cref="IComparer{T}"/> for <see cref="ITypeSymbol"/>s sorted by display strings.
    /// </summary>
#pragma warning disable CA1812 // Is too instantiated.
    internal class SymbolByDisplayStringComparer : IComparer<ITypeSymbol>
#pragma warning restore CA1812
    {
        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="compilation">The compilation containing the types to be compared.</param>
        public SymbolByDisplayStringComparer(Compilation compilation)
            : this(SymbolDisplayStringCache.GetOrCreate(compilation))
        {
        }

        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="symbolDisplayStringCache">The cache display strings to use.</param>
        public SymbolByDisplayStringComparer(SymbolDisplayStringCache symbolDisplayStringCache)
        {
            this.SymbolDisplayStringCache = symbolDisplayStringCache ?? throw new ArgumentNullException(nameof(symbolDisplayStringCache));
        }

        /// <summary>
        /// Cache of symbol display strings.
        /// </summary>
        public SymbolDisplayStringCache SymbolDisplayStringCache { get; }

        /// <summary>
        /// Compares two type symbols by their display strings.
        /// </summary>
        /// <param name="x">First type symbol to compare.</param>
        /// <param name="y">Second type symbol to compare.</param>
        /// <returns>Less than 0 if <paramref name="x"/> is before <paramref name="y"/>, 0 if equal, greater than 0 if
        /// <paramref name="x"/> is after <paramref name="y"/>.</returns>
        public int Compare([AllowNull] ITypeSymbol x, [AllowNull] ITypeSymbol y)
        {
            RoslynDebug.Assert(x != null);
            RoslynDebug.Assert(y != null);

            return StringComparer.Ordinal.Compare(
                this.SymbolDisplayStringCache.GetDisplayString(x),
                this.SymbolDisplayStringCache.GetDisplayString(y));
        }
    }
}
