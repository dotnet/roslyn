// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal partial class LibraryManager
    {
        private IList<AbstractTreeItem> CreateNavigableItemTreeItems(IEnumerable<INavigableItem> items)
        {
            var itemsList = items.ToList();
            if (itemsList.Count == 0)
            {
                return new List<AbstractTreeItem>();
            }

            // Collect all the documents in the list of items we're presenting.  Then determine
            // what number of common path elements they have in common.  We can avoid showing
            // these common locations, thus presenting a much cleaner result view to the user.
            var documents = new HashSet<Document>();
            CollectDocuments(itemsList, documents);

            var commonPathElements = CountCommonPathElements(documents);

            return CreateNavigableItemTreeItems(itemsList, commonPathElements);
        }

        private int CountCommonPathElements(HashSet<Document> documents)
        {
            Debug.Assert(documents.Count > 0);
            var commonPathElements = 0;
            for (var index = 0; ; index++)
            {
                var pathPortion = GetPathPortion(documents.First(), index);
                if (pathPortion == null)
                {
                    return commonPathElements;
                }

                foreach (var document in documents)
                {
                    if (GetPathPortion(document, index) != pathPortion)
                    {
                        return commonPathElements;
                    }
                }

                commonPathElements++;
            }
        }

        private string GetPathPortion(Document document, int index)
        {
            if (index == 0)
            {
                return document.Project.Name;
            }

            index--;
            if (index < document.Folders.Count)
            {
                return document.Folders[index];
            }

            return null;
        }

        private void CollectDocuments(IEnumerable<INavigableItem> items, HashSet<Document> documents)
        {
            foreach (var item in items)
            {
                documents.Add(item.Document);

                CollectDocuments(item.ChildItems, documents);
            }
        }

        private IList<AbstractTreeItem> CreateNavigableItemTreeItems(IEnumerable<INavigableItem> items, int commonPathElements)
        {
            var sourceListItems =
                from item in items
                where IsValidSourceLocation(item.Document, item.SourceSpan)
                select CreateTreeItem(item, commonPathElements);

            return sourceListItems.ToList();
        }

        private AbstractTreeItem CreateTreeItem(INavigableItem item, int commonPathElements)
        {
            var result = new SourceReferenceTreeItem(item.Document, item.SourceSpan, item.Glyph.GetGlyphIndex(), commonPathElements, displayText: item.DisplayString, includeFileLocation: item.DisplayFileLocation);

            if (!item.ChildItems.IsEmpty)
            {
                var childItems = CreateNavigableItemTreeItems(item.ChildItems, commonPathElements);
                result.Children.AddRange(childItems);
                result.SetReferenceCount(childItems.Count);
            }

            return result;
        }

        public void PresentNavigableItems(string title, IEnumerable<INavigableItem> items)
        {
            PresentObjectList(title, new ObjectList(CreateNavigableItemTreeItems(items), this));
        }
    }
}
