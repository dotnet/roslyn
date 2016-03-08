// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Navigation
{
    internal partial class NavigableItemFactory
    {
        internal class DeclaredSymbolNavigableItem : INavigableItem
        {
            public string DisplayString => _lazyDisplayString.Value;
            public Document Document { get; }
            public Glyph Glyph => Symbol?.GetGlyph() ?? Glyph.Error;
            public TextSpan SourceSpan => _declaredSymbolInfo.Span;
            public ISymbol Symbol => _lazySymbol.Value;
            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;

            public bool DisplayFileLocation => false;

            private readonly DeclaredSymbolInfo _declaredSymbolInfo;
            private readonly Lazy<string> _lazyDisplayString;
            private readonly Lazy<ISymbol> _lazySymbol;

            public DeclaredSymbolNavigableItem(Document document, DeclaredSymbolInfo declaredSymbolInfo)
            {
                Document = document;
                _declaredSymbolInfo = declaredSymbolInfo;

                _lazySymbol = new Lazy<ISymbol>(FindSymbol);
                _lazyDisplayString = new Lazy<string>(() =>
                {
                    try
                    {
                        if (Symbol == null)
                        {
                            return null;
                        }

                        return GetSymbolDisplayString(Document.Project, Symbol);
                    }
                    catch (Exception e) when (FatalError.Report(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                });
            }

            private ISymbol FindSymbol()
            {
                // Here, we will use partial semantics. We are going to use this symbol to get a glyph, display string,
                // and potentially documentation comments. The first two should work fine even if we don't have full
                // references, and the latter will probably be fine. (It wouldn't be if you have a partial type
                // and we didn't get all the trees parsed yet to know that. In other words, an edge case we don't care about.)

                // Cancellation isn't supported when computing the various properties that depend on the symbol, hence
                // CancellationToken.None.
                var semanticModel = Document.GetPartialSemanticModelAsync(CancellationToken.None).GetAwaiter().GetResult();

                var root = semanticModel.SyntaxTree.GetRoot(CancellationToken.None);
                var node = root.FindNode(_declaredSymbolInfo.Span);
                return semanticModel.GetDeclaredSymbol(node, CancellationToken.None);
            }
        }
    }
}
