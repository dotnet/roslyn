// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Navigation
{
    internal partial class NavigableItemFactory
    {
        internal class DeclaredSymbolNavigableItem : INavigableItem
        {
            public string DisplayName => _lazyDisplayName.Value;
            public Document Document { get; }
            public Glyph Glyph => _lazySymbol.Value?.GetGlyph() ?? Glyph.Error;
            public TextSpan SourceSpan => _declaredSymbolInfo.Span;
            public ISymbol Symbol => _lazySymbol.Value;
            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;

            private readonly DeclaredSymbolInfo _declaredSymbolInfo;
            private readonly Lazy<string> _lazyDisplayName;
            private readonly Lazy<ISymbol> _lazySymbol;

            public DeclaredSymbolNavigableItem(Document document, DeclaredSymbolInfo declaredSymbolInfo)
            {
                Document = document;
                _declaredSymbolInfo = declaredSymbolInfo;

                // Cancellation isn't supported when computing the various properties that depend on the symbol, hence
                // CancellationToken.None.
                _lazySymbol = new Lazy<ISymbol>(() => declaredSymbolInfo.GetSymbolAsync(document, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult());
                _lazyDisplayName = new Lazy<string>(() =>
                {
                    try
                    {
                        if (Symbol == null)
                        {
                            return null;
                        }

                        var symbolDisplayService = Document.GetLanguageService<ISymbolDisplayService>();
                        switch (Symbol.Kind)
                        {
                            case SymbolKind.NamedType:
                                return symbolDisplayService.ToDisplayString(Symbol, s_shortFormatWithModifiers);

                            case SymbolKind.Method:
                                return Symbol.IsStaticConstructor()
                                    ? symbolDisplayService.ToDisplayString(Symbol, s_shortFormatWithModifiers)
                                    : symbolDisplayService.ToDisplayString(Symbol, s_shortFormat);

                            default:
                                return symbolDisplayService.ToDisplayString(Symbol, s_shortFormat);
                        }
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
