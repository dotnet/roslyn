// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal partial class LibraryManager
    {
        public void PresentDefinitionsAndReferences(DefinitionsAndReferences definitionsAndReferences)
        {
            var firstDefinition = definitionsAndReferences.Definitions.FirstOrDefault();
            var title = firstDefinition?.DisplayParts.JoinText();

            PresentObjectList(title, new ObjectList(CreateFindReferencesItems(definitionsAndReferences), this));
        }

        // internal for test purposes
        internal IList<AbstractTreeItem> CreateFindReferencesItems(
            DefinitionsAndReferences definitionsAndReferences)
        {
            var documents = definitionsAndReferences.References.Select(r => r.Location.Document)
                                                               .WhereNotNull()
                                                               .ToSet();
            var commonPathElements = CountCommonPathElements(documents);

            var query = from d in definitionsAndReferences.Definitions
                        from i in CreateDefinitionItems(d, definitionsAndReferences, commonPathElements)
                        select i;

            return query.ToList();
        }

        private IEnumerable<AbstractTreeItem> CreateDefinitionItems(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences,
            int commonPathElements)
        {
            // Each definition item may end up as several top nodes (because of partials).
            // Add the references to the last item actually in the list.
            var definitionTreeItems = ConvertToDefinitionTreeItems(definitionItem);
            if (!definitionTreeItems.IsEmpty)
            {
                var lastTreeItem = definitionTreeItems.Last();
                var referenceItems = CreateReferenceItems(
                    definitionItem, definitionsAndReferences, commonPathElements);

                lastTreeItem.Children.AddRange(referenceItems);
                lastTreeItem.SetReferenceCount(referenceItems.Count);
            }

            return definitionTreeItems;
        }

        private ImmutableArray<AbstractTreeItem> ConvertToDefinitionTreeItems(
            DefinitionItem definitionItem)
        {
            var result = ImmutableArray.CreateBuilder<AbstractTreeItem>();

            foreach (var location in definitionItem.Locations)
            {
                result.Add(new DefinitionTreeItem(definitionItem, location));
            }

            return result.ToImmutable();
        }

        private IList<SourceReferenceTreeItem> CreateReferenceItems(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences,
            int commonPathElements)
        {
            var result = new List<SourceReferenceTreeItem>();

            var referenceItems = definitionsAndReferences.References.Where(r => r.Definition == definitionItem);
            foreach (var referenceItem in referenceItems)
            {
                var documentLocation = referenceItem.Location;
                result.Add(new SourceReferenceTreeItem(
                    documentLocation.Document,
                    documentLocation.SourceSpan,
                    Glyph.Reference.GetGlyphIndex(),
                    commonPathElements));
            }

            var linkedReferences = result.GroupBy(r => r.DisplayText.ToLowerInvariant()).Where(g => g.Count() > 1).SelectMany(g => g);
            foreach (var linkedReference in linkedReferences)
            {
                linkedReference.AddProjectNameDisambiguator();
            }

            result.Sort();
            return result;
        }
    }
}