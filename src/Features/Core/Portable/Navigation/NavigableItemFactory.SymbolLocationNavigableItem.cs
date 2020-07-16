// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal partial class NavigableItemFactory
    {
        private class SymbolLocationNavigableItem : INavigableItem
        {
            private readonly Solution _solution;
            private readonly ISymbol _symbol;
            private readonly Location _location;

            public SymbolLocationNavigableItem(
                Solution solution,
                ISymbol symbol,
                Location location,
                ImmutableArray<TaggedText>? displayTaggedParts)
            {
                _solution = solution;
                _symbol = symbol;
                _location = location;
                DisplayTaggedParts = displayTaggedParts.GetValueOrDefault();
            }

            public bool DisplayFileLocation => true;

            public ImmutableArray<TaggedText> DisplayTaggedParts { get; }

            public Glyph Glyph => _symbol.GetGlyph();

            public bool IsImplicitlyDeclared => _symbol.IsImplicitlyDeclared;

            public Document Document =>
                _location.IsInSource ? _solution.GetDocument(_location.SourceTree) : null;

            public TextSpan SourceSpan => _location.SourceSpan;

            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;
        }
    }
}
