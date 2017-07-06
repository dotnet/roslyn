// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Drawing;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.NavigateTo
{
    internal partial class Dev15NavigateToHostVersionService
    {
        private class Dev15ItemDisplayFactory : INavigateToItemDisplayFactory
        {
            public INavigateToItemDisplay CreateItemDisplay(NavigateToItem item)
            {
                var searchResult = (INavigateToSearchResult)item.Tag;
                return new Dev15NavigateToItemDisplay(searchResult);
            }
        }

        private class Dev15NavigateToItemDisplay : AbstractNavigateToItemDisplay, INavigateToItemDisplay3
        {
            public Dev15NavigateToItemDisplay(INavigateToSearchResult searchResult) 
                : base(searchResult)
            {
            }

            public override Icon Glyph => null;

            public ImageMoniker GlyphMoniker => SearchResult.NavigableItem.Glyph.GetImageMoniker();

            public IReadOnlyList<Span> GetAdditionalInformationMatchRuns(string searchValue)
                => SpecializedCollections.EmptyReadOnlyList<Span>();
        }
    }
}
