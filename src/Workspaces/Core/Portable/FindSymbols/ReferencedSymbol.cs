// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Represents a single result of the call to the synchronous
    /// IFindReferencesService.FindReferences method. Finding the references to a symbol will result
    /// in a set of definitions being returned (containing at least the symbol requested) as well as
    /// any references to those definitions in the source. Multiple definitions may be found due to
    /// how C# and VB allow a symbol to be both a definition and a reference at the same time (for
    /// example, a method which implements an interface method).
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public class ReferencedSymbol
    {
        /// <summary>
        /// The symbol definition that these are references to.
        /// </summary>
        public ISymbol Definition { get; }

        /// <summary>
        /// Same as <see cref="Locations"/> but exposed as an <see cref="ImmutableArray{T}"/> for performance.
        /// </summary>
        internal ImmutableArray<ReferenceLocation> LocationsArray { get; }

        /// <summary>
        /// The set of reference locations in the solution.
        /// </summary>
        public IEnumerable<ReferenceLocation> Locations => LocationsArray;

        internal ReferencedSymbol(
            ISymbol definition,
            ImmutableArray<ReferenceLocation> locations)
        {
            this.Definition = definition;
            this.LocationsArray = locations.NullToEmpty();
        }

        private string GetDebuggerDisplay()
        {
            var count = this.Locations.Count();
            return string.Format("{0}, {1} {2}", this.Definition.Name, count, count == 1 ? "ref" : "refs");
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor(ReferencedSymbol referencedSymbol)
        {
            internal string GetDebuggerDisplay()
                => referencedSymbol.GetDebuggerDisplay();
        }
    }
}
