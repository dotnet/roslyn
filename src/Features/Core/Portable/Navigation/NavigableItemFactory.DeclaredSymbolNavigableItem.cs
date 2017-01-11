// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal partial class NavigableItemFactory
    {
        internal class DeclaredSymbolNavigableItem : INavigableItem
        {
            public ImmutableArray<TaggedText> DisplayTaggedParts => _lazyDisplayTaggedParts.Value; 

            public Document Document { get; }
            public Glyph Glyph => Symbol.GetGlyph();
            public TextSpan SourceSpan => _declaredSymbolInfo.Span;
            public ISymbol Symbol { get; }
            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;

            public bool DisplayFileLocation => false;

            private readonly DeclaredSymbolInfo _declaredSymbolInfo;
            private readonly Lazy<ImmutableArray<TaggedText>> _lazyDisplayTaggedParts;

            /// <summary>
            /// DeclaredSymbolInfos always come from some actual declaration in source.  So they're
            /// never implicitly declared.
            /// </summary>
            public bool IsImplicitlyDeclared => false;

            public DeclaredSymbolNavigableItem(
                Document document, DeclaredSymbolInfo declaredSymbolInfo, ISymbol symbol)
            {
                Document = document;
                _declaredSymbolInfo = declaredSymbolInfo;
                this.Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));

                _lazyDisplayTaggedParts = new Lazy<ImmutableArray<TaggedText>>(() =>
                {
                    try
                    {
                        return GetSymbolDisplayTaggedParts(Document.Project, Symbol);
                    }
                    catch (Exception e) when (FatalError.Report(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                });
            }
        }
    }
}