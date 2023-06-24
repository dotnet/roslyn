﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal partial class NavigableItemFactory
    {
        private class SymbolLocationNavigableItem : INavigableItem
        {
            private readonly Solution _solution;
            private readonly ISymbol _symbol;
            private readonly Location _location;

            /// <summary>
            /// Lazily-initialized backing field for <see cref="Document"/>.
            /// </summary>
            /// <seealso cref="LazyInitialization.EnsureInitialized{T, U}(ref StrongBox{T}, Func{U, T}, U)"/>
            private StrongBox<INavigableItem.NavigableDocument> _lazyDocument;

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

            public INavigableItem.NavigableDocument Document
            {
                get
                {
                    return LazyInitialization.EnsureInitialized(
                        ref _lazyDocument,
                        static self =>
                        {
                            return (self._location.IsInSource && self._solution.GetDocument(self._location.SourceTree) is { } document)
                                ? INavigableItem.NavigableDocument.FromDocument(document)
                                : null;
                        },
                        this);
                }
            }

            public TextSpan SourceSpan => _location.SourceSpan;

            public bool IsStale => false;

            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;
        }
    }
}
