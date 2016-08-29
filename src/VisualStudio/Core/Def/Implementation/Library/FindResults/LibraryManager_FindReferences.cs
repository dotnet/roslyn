// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
            var definitionDocuments =
                definitionsAndReferences.Definitions.SelectMany(d => d.SourceSpans)
                                        .Select(loc => loc.Document);

            var referenceDocuments = 
                definitionsAndReferences.References
                                        .Select(r => r.SourceSpan.Document);

            var allDocuments = definitionDocuments.Concat(referenceDocuments).WhereNotNull().ToSet();
            var commonPathElements = CountCommonPathElements(allDocuments);

            var query =
                from d in definitionsAndReferences.Definitions
                let referenceItems = CreateReferenceItems(d, definitionsAndReferences, commonPathElements)
                select new DefinitionTreeItem(d, referenceItems);

            return query.ToList<AbstractTreeItem>();
        }

        private ImmutableArray<SourceReferenceTreeItem> CreateReferenceItems(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences,
            int commonPathElements)
        {
            var result = ImmutableArray.CreateBuilder<SourceReferenceTreeItem>();

            var definitionGlyph = definitionItem.Tags.GetGlyph();

            // Skip the first definition.  We'll present it in the definition item.
            var definitionLocationsAndGlyphs =
                from loc in definitionItem.SourceSpans.Skip(1)
                select ValueTuple.Create(loc, definitionGlyph);

            var referenceLocationsAndGlyphs =
                from r in definitionsAndReferences.References
                where r.Definition == definitionItem
                select ValueTuple.Create(r.SourceSpan, Glyph.Reference);

            var allLocationsAndGlyphs = definitionLocationsAndGlyphs.Concat(referenceLocationsAndGlyphs);

            foreach (var locationAndGlyph in allLocationsAndGlyphs)
            {
                var documentLocation = locationAndGlyph.Item1;
                var glyph = locationAndGlyph.Item2;
                result.Add(new SourceReferenceTreeItem(
                    documentLocation.Document,
                    documentLocation.SourceSpan,
                    glyph.GetGlyphIndex(),
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