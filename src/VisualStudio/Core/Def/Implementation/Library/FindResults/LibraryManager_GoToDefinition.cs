// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal partial class LibraryManager
    {
        private IList<AbstractTreeItem> CreateGoToDefinitionItems(IEnumerable<INavigableItem> items)
        {
            var sourceListItems =
                from item in items
                where IsValidSourceLocation(item.Document, item.SourceSpan)
                select CreateTreeItem(item);

            return sourceListItems.ToList();
        }

        private AbstractTreeItem CreateTreeItem(INavigableItem item)
        {
            var displayText = !item.ChildItems.IsEmpty ? item.DisplayName : null;
            var result = new SourceReferenceTreeItem(item.Document, item.SourceSpan, item.Glyph.GetGlyphIndex(), displayText);

            if (!item.ChildItems.IsEmpty)
            {
                var childItems = CreateGoToDefinitionItems(item.ChildItems);
                result.Children.AddRange(childItems);
                result.SetReferenceCount(childItems.Count);
            }

            return result;
        }

        public void PresentNavigableItems(string title, IEnumerable<INavigableItem> items)
        {
            PresentObjectList(title, new ObjectList(CreateGoToDefinitionItems(items), this));
        }
    }
}
