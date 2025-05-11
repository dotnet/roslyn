// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using InterlockedOperations = Roslyn.Utilities.InterlockedOperations;

namespace Microsoft.CodeAnalysis.Navigation;

internal static partial class NavigableItemFactory
{
    private sealed class SymbolLocationNavigableItem(
        Solution solution,
        ISymbol symbol,
        Location location,
        ImmutableArray<TaggedText>? displayTaggedParts) : INavigableItem
    {
        private readonly Solution _solution = solution;
        private readonly ISymbol _symbol = symbol;
        private readonly Location _location = location;

        /// <summary>
        /// Lazily-initialized backing field for <see cref="Document"/>.
        /// </summary>
        /// <seealso cref="InterlockedOperations.Initialize{T, U}(ref StrongBox{T}, Func{U, T}, U)"/>
        private StrongBox<INavigableItem.NavigableDocument> _lazyDocument;

        public bool DisplayFileLocation => true;

        public ImmutableArray<TaggedText> DisplayTaggedParts { get; } = displayTaggedParts.GetValueOrDefault();

        public Glyph Glyph => _symbol.GetGlyph();

        public bool IsImplicitlyDeclared => _symbol.IsImplicitlyDeclared;

        public INavigableItem.NavigableDocument Document
        {
            get
            {
                return InterlockedOperations.Initialize(
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

        public ImmutableArray<INavigableItem> ChildItems => [];
    }
}
