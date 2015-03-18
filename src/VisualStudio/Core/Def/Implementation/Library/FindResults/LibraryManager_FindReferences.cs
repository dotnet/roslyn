// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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
        private bool IncludeDefinition(ReferencedSymbol reference)
        {
            var definition = reference.Definition;

            // Don't include parameters to property accessors
            if (definition is IParameterSymbol &&
                definition.ContainingSymbol is IMethodSymbol &&
                ((IMethodSymbol)definition.ContainingSymbol).AssociatedSymbol is IPropertySymbol)
            {
                return false;
            }

            return true;
        }

        public void PresentReferencedSymbols(string title, Solution solution, IEnumerable<ReferencedSymbol> items)
        {
            PresentObjectList(title, new ObjectList(CreateFindReferencesItems(solution, items), this));
        }

        private IList<AbstractTreeItem> CreateFindReferencesItems(Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols)
        {
            var definitions = new List<AbstractTreeItem>();
            var uniqueLocations = new HashSet<ValueTuple<Document, TextSpan>>();
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();

            referencedSymbols = referencedSymbols.FilterUnreferencedSyntheticDefinitions().ToList();

            foreach (var referencedSymbol in referencedSymbols)
            {
                if (!IncludeDefinition(referencedSymbol))
                {
                    continue;
                }

                var definition = referencedSymbol.Definition;
                var locations = definition.Locations;

                foreach (var definitionLocation in definition.Locations)
                {
                    var definitionItem = ConvertToDefinitionItem(solution, referencedSymbol, uniqueLocations, definitionLocation, definition.GetGlyph());
                    if (definitionItem != null)
                    {
                        definitions.Add(definitionItem);
                        var referenceItems = CreateReferenceItems(solution, uniqueLocations, referencedSymbol.Locations.Select(loc => loc.Location), Glyph.Reference);
                        definitionItem.Children.AddRange(referenceItems);
                        definitionItem.SetReferenceCount(referenceItems.Count);
                    }
                }

                // Add on any definition locations from third party language services
                string filePath;
                int lineNumber, charOffset;
                if (symbolNavigationService.WouldNavigateToSymbol(definition, solution, out filePath, out lineNumber, out charOffset))
                {
                    definitions.Add(new ExternalLanguageDefinitionTreeItem(filePath, lineNumber, charOffset, definition.Name, definition.GetGlyph().GetGlyphIndex(), this.ServiceProvider));
                }
            }

            return definitions;
        }

        private AbstractTreeItem ConvertToDefinitionItem(
            Solution solution,
            ReferencedSymbol referencedSymbol,
            HashSet<ValueTuple<Document, TextSpan>> uniqueLocations,
            Location location,
            Glyph glyph)
        {
            if (!location.IsInSource)
            {
                return referencedSymbol.Locations.Any()
                    ? new MetadataDefinitionTreeItem(
                        solution.Workspace,
                        referencedSymbol.Definition,
                        referencedSymbol.Locations.First().Document.Project.Id,
                        glyph.GetGlyphIndex())
                    : null;
            }

            var document = solution.GetDocument(location.SourceTree);
            var sourceSpan = location.SourceSpan;
            if (!IsValidSourceLocation(document, sourceSpan) ||
                !uniqueLocations.Add(new ValueTuple<Document, TextSpan>(document, sourceSpan)))
            {
                return null;
            }

            return new SourceDefinitionTreeItem(document, sourceSpan, referencedSymbol.Definition, glyph.GetGlyphIndex());
        }

        private IList<SourceReferenceTreeItem> CreateReferenceItems(Solution solution, HashSet<ValueTuple<Document, TextSpan>> uniqueLocations, IEnumerable<Location> locations, Glyph glyph)
        {
            var referenceItems = new List<SourceReferenceTreeItem>();

            foreach (var location in locations)
            {
                if (!location.IsInSource)
                {
                    continue;
                }

                var document = solution.GetDocument(location.SourceTree);
                var sourceSpan = location.SourceSpan;
                if (!IsValidSourceLocation(document, sourceSpan))
                {
                    continue;
                }

                if (uniqueLocations.Add(new ValueTuple<Document, TextSpan>(document, sourceSpan)))
                {
                    referenceItems.Add(new SourceReferenceTreeItem(document, sourceSpan, glyph.GetGlyphIndex()));
                }
            }

            var linkedReferences = referenceItems.GroupBy(r => r.DisplayText.ToLowerInvariant()).Where(g => g.Count() > 1).SelectMany(g => g);
            foreach (var linkedReference in linkedReferences)
            {
                linkedReference.AddProjectNameDisambiguator();
            }

            referenceItems.Sort();
            return referenceItems;
        }
    }
}
