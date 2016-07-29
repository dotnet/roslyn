// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;

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
            var definitionDocuments =
                definitionsAndReferences.Definitions.SelectMany(d => d.AdditionalLocations)
                                        .Select(loc => loc.Document);
            var referenceDocuments =
                definitionsAndReferences.References.Select(r => r.Location.Document);

            var documents = definitionDocuments.Concat(referenceDocuments).WhereNotNull().ToSet();
            var commonPathElements = CountCommonPathElements(documents);

            return definitionsAndReferences.Definitions
                .Select(d => CreateDefinitionItem(d, definitionsAndReferences, commonPathElements))
                .ToList<AbstractTreeItem>();
        }

        private DefinitionTreeItem CreateDefinitionItem(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences,
            int commonPathElements)
        {
            var referenceItems = CreateReferenceItems(
                definitionItem, definitionsAndReferences, commonPathElements);

            return ConvertToDefinitionTreeItem(definitionItem, referenceItems, commonPathElements);
        }

        private DefinitionTreeItem ConvertToDefinitionTreeItem(
            DefinitionItem definitionItem,
            ImmutableArray<SourceReferenceTreeItem> referenceItems,
            int commonPathElements)
        {
            var finalReferenceItems = ImmutableArray.CreateBuilder<SourceReferenceTreeItem>();

            foreach (var additionalLocation in definitionItem.AdditionalLocations)
            {
                finalReferenceItems.Add(new SourceReferenceTreeItem(
                    additionalLocation.Document,
                    additionalLocation.SourceSpan,
                    definitionItem.Tags.GetGlyph().GetGlyphIndex(),
                    commonPathElements));
            }

            finalReferenceItems.AddRange(referenceItems);

            return new DefinitionTreeItem(definitionItem, finalReferenceItems.ToImmutable());
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