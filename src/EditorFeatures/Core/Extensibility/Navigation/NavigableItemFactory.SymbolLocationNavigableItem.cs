// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Navigation
{
    internal partial class NavigableItemFactory
    {
        private class SymbolLocationNavigableItem : INavigableItem
        {
            private readonly Solution _solution;
            private readonly ISymbol _symbol;
            private readonly Location _location;
            private readonly string _displayString;

            public SymbolLocationNavigableItem(
                Solution solution,
                ISymbol symbol,
                Location location,
                string displayString)
            {
                _solution = solution;
                _symbol = symbol;
                _location = location;
                _displayString = displayString;
            }

            public bool DisplayFileLocation => true;

            public string DisplayString => _displayString;

            public Glyph Glyph => _symbol.GetGlyph();

            public bool IsImplicitlyDeclared => _symbol.IsImplicitlyDeclared;

            public Document Document
            {
                get
                {
                    return _location.IsInSource ? _solution.GetDocument(_location.SourceTree) : null;
                }
            }

            public TextSpan SourceSpan => _location.SourceSpan;

            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;
        }
    }
}
