// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.FindReferences;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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
            var definitionItems = definitionsAndReferences.Definitions;
            return definitionItems.SelectMany(d => CreateDefinitionItems(d, definitionsAndReferences))
                                  .ToList();
        }

        private IEnumerable<AbstractTreeItem> CreateDefinitionItems(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences)
        {
            // Each definition item may end up as several top nodes (because of partials).
            // Add the references to the last item actually in the list.
            var definitionTreeItems = ConvertToDefinitionTreeItems(definitionItem);
            if (!definitionTreeItems.IsEmpty)
            {
                var lastTreeItem = definitionTreeItems.Last();
                var referenceItems = CreateReferenceItems(definitionItem, definitionsAndReferences);

                lastTreeItem.Children.AddRange(referenceItems);
                lastTreeItem.SetReferenceCount(referenceItems.Count);
            }

            return definitionTreeItems;

            //    // Add on any definition locations from third party language services
            //    string filePath;
            //    int lineNumber, charOffset;
            //    if (symbolNavigationService.WouldNavigateToSymbol(definition, solution, out filePath, out lineNumber, out charOffset))
            //    {
            //        definitions.Add(new ExternalLanguageDefinitionTreeItem(filePath, lineNumber, charOffset, definition.Name, definition.GetGlyph().GetGlyphIndex(), this.ServiceProvider));
            //    }
            //}

            //return definitions;
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
            //if (!location.IsInSource)
            //{
            //    return referencedSymbol.Locations.Any()
            //        ? new MetadataDefinitionTreeItem(
            //            solution.Workspace,
            //            referencedSymbol.Definition,
            //            referencedSymbol.Locations.First().Document.Project.Id,
            //            glyph.GetGlyphIndex())
            //        : null;
            //}

            //var document = solution.GetDocument(location.SourceTree);
            //var sourceSpan = location.SourceSpan;
            //if (!IsValidSourceLocation(document, sourceSpan))
            //{
            //    return null;
            //}

            //return new SourceDefinitionTreeItem(document, sourceSpan, referencedSymbol.Definition, glyph.GetGlyphIndex());
        }

        private IList<SourceReferenceTreeItem> CreateReferenceItems(
            DefinitionItem definitionItem,
            DefinitionsAndReferences definitionsAndReferences)
        {
            var result = new List<SourceReferenceTreeItem>();

            var referenceItems = definitionsAndReferences.References.Where(r => r.Definition == definitionItem);
            foreach (var referenceItem in referenceItems)
            {
                var documentLocation = referenceItem.Location;
                result.Add(new SourceReferenceTreeItem(
                    documentLocation.Document, 
                    documentLocation.SourceSpan,
                    Glyph.Reference.GetGlyphIndex()));
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
