// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
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
            public Glyph Glyph => this.SymbolOpt?.GetGlyph() ?? Glyph.Error;
            public TextSpan SourceSpan => _declaredSymbolInfo.Span;
            public ISymbol SymbolOpt => _lazySymbolOpt.Value;
            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;

            public bool DisplayFileLocation => false;

            private readonly DeclaredSymbolInfo _declaredSymbolInfo;
            private readonly Lazy<ImmutableArray<TaggedText>> _lazyDisplayTaggedParts;
            private readonly Lazy<ISymbol> _lazySymbolOpt;

            /// <summary>
            /// DeclaredSymbolInfos always come from some actual declaration in source.  So they're
            /// never implicitly declared.
            /// </summary>
            public bool IsImplicitlyDeclared => false;

            public DeclaredSymbolNavigableItem(Document document, DeclaredSymbolInfo declaredSymbolInfo)
            {
                Document = document;
                _declaredSymbolInfo = declaredSymbolInfo;

                _lazySymbolOpt = new Lazy<ISymbol>(TryFindSymbol);

                _lazyDisplayTaggedParts = new Lazy<ImmutableArray<TaggedText>>(() =>
                {
                    try
                    {
                        if (this.SymbolOpt == null)
                        {
                            return default(ImmutableArray<TaggedText>);
                        }

                        return GetSymbolDisplayTaggedParts(Document.Project, this.SymbolOpt);
                    }
                    catch (Exception e) when (FatalError.Report(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                });
            }

            private ISymbol TryFindSymbol()
            {
                // Here, we will use partial semantics. We are going to use this symbol to get a glyph, display string,
                // and potentially documentation comments. The first two should work fine even if we don't have full
                // references, and the latter will probably be fine. (It wouldn't be if you have a partial type
                // and we didn't get all the trees parsed yet to know that. In other words, an edge case we don't care about.)

                // Cancellation isn't supported when computing the various properties that depend on the symbol, hence
                // CancellationToken.None.
                var semanticModel = Document.GetPartialSemanticModelAsync(CancellationToken.None).GetAwaiter().GetResult();

                return _declaredSymbolInfo.TryResolve(semanticModel, CancellationToken.None);
            }
        }
    }
}
