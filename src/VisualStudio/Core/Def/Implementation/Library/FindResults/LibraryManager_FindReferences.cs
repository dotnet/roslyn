// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindReferences;
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
                        select (AbstractTreeItem)i;

            return query.ToList();
        }

        private ImmutableArray<DefinitionTreeItem> CreateDefinitionItems(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences,
            int commonPathElements)
        {
            var referenceItems = CreateReferenceItems(
                definitionItem, definitionsAndReferences, commonPathElements);

            return ConvertToDefinitionTreeItems(definitionItem, referenceItems);
        }

        private ImmutableArray<DefinitionTreeItem> ConvertToDefinitionTreeItems(
            DefinitionItem definitionItem,
            ImmutableArray<SourceReferenceTreeItem> referenceItems)
        {
            var result = ImmutableArray.CreateBuilder<DefinitionTreeItem>();

            for (int i = 0, n = definitionItem.Locations.Length; i < n; i++)
            {
                var location = definitionItem.Locations[i];

                // Each definition item may end up as several top nodes (because of partials).
                // Add the references to the last item actually in the list.
                var childItems = i == n - 1
                    ? referenceItems
                    : ImmutableArray<SourceReferenceTreeItem>.Empty;

                result.Add(new DefinitionTreeItem(definitionItem, location, childItems));
            }

            return result.ToImmutable();
        }

        private ImmutableArray<SourceReferenceTreeItem> CreateReferenceItems(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences,
            int commonPathElements)
        {
            var result = ImmutableArray.CreateBuilder<SourceReferenceTreeItem>();

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
            return result.ToImmutable();
        }
    }
}