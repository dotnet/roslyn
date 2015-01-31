// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal partial class LibraryManager
    {
        private IList<AbstractListItem> CreateGoToDefinitionItems(IEnumerable<INavigableItem> items)
        {
            var sourceListItems =
                from item in items
                where IsValidSourceLocation(item.Document, item.SourceSpan)
                select (AbstractListItem)new SourceListItem(item.Document, item.SourceSpan, item.Glyph.GetGlyphIndex());

            return sourceListItems.ToList();
        }

        public void PresentNavigableItems(string title, IEnumerable<INavigableItem> items)
        {
            PresentObjectList(title, new ObjectList(CreateGoToDefinitionItems(items), this));
        }
    }
}
