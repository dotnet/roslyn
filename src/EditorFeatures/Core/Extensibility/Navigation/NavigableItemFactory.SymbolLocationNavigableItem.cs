// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServices;
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
            private readonly Lazy<string> _lazyDisplayName;

            public SymbolLocationNavigableItem(
                Solution solution,
                ISymbol symbol,
                Location location)
            {
                _solution = solution;
                _symbol = symbol;
                _location = location;

                _lazyDisplayName = new Lazy<string>(() =>
                {
                    var symbolDisplayService = this.Document.Project.LanguageServices.GetService<ISymbolDisplayService>();

                    switch (symbol.Kind)
                    {
                        case SymbolKind.NamedType:
                            return symbolDisplayService.ToDisplayString(_symbol, s_shortFormatWithModifiers);

                        case SymbolKind.Method:
                            return _symbol.IsStaticConstructor()
                                ? symbolDisplayService.ToDisplayString(_symbol, s_shortFormatWithModifiers)
                                : symbolDisplayService.ToDisplayString(_symbol, s_shortFormat);

                        default:
                            return symbolDisplayService.ToDisplayString(_symbol, s_shortFormat);
                    }
                });
            }

            public string DisplayName
            {
                get
                {
                    return _lazyDisplayName.Value;
                }
            }

            public Glyph Glyph
            {
                get
                {
                    return _symbol.GetGlyph();
                }
            }

            public Document Document
            {
                get
                {
                    return _location.IsInSource ? _solution.GetDocument(_location.SourceTree) : null;
                }
            }

            public TextSpan SourceSpan
            {
                get
                {
                    return _location.SourceSpan;
                }
            }

            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;
        }
    }
}
