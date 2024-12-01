// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

internal sealed partial class SymbolicRenameLocations
{
    private readonly struct SearchResult
    {
        public readonly ImmutableHashSet<RenameLocation> Locations;
        public readonly ImmutableArray<ReferenceLocation> ImplicitLocations;
        public readonly ImmutableArray<ISymbol> ReferencedSymbols;

        public SearchResult(
            ImmutableHashSet<RenameLocation> locations,
            ImmutableArray<ReferenceLocation> implicitLocations,
            ImmutableArray<ISymbol> referencedSymbols)
        {
            Contract.ThrowIfNull(locations);
            this.Locations = locations;
            this.ImplicitLocations = implicitLocations;
            this.ReferencedSymbols = referencedSymbols;
        }
    }
}
