// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Drawing;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class Dev14NavigateToHostVersionService
    {
        private class Dev14NavigateToItemDisplay : AbstractNavigateToItemDisplay
        {
            private readonly NavigateToIconFactory _iconFactory;

            private Icon _glyph;

            public Dev14NavigateToItemDisplay(INavigateToSearchResult searchResult, NavigateToIconFactory iconFactory)
                : base(searchResult)
            {
                _iconFactory = iconFactory;
            }

            public override Icon Glyph
            {
                get
                {
                    if (_glyph == null)
                    {
                        _glyph = _iconFactory.GetIcon(SearchResult.NavigableItem.Glyph);
                    }

                    return _glyph;
                }
            }
        }
    }
}
