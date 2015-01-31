// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
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

        private IList<AbstractListItem> CreateFindReferencesItems(Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols)
        {
            var list = new List<AbstractListItem>();
            HashSet<ValueTuple<Document, TextSpan>> uniqueLocations = null;

            referencedSymbols = referencedSymbols.FilterUnreferencedSyntheticDefinitions().ToList();
            foreach (var referencedSymbol in referencedSymbols)
            {
                if (!IncludeDefinition(referencedSymbol))
                {
                    continue;
                }

                var definition = referencedSymbol.Definition;
                var locations = definition.Locations;
                uniqueLocations = AddLocations(solution, list, uniqueLocations, locations, definition.GetGlyph());
                uniqueLocations = AddLocations(solution, list, uniqueLocations, referencedSymbol.Locations.Select(loc => loc.Location), Glyph.Reference);

                string filePath;
                int lineNumber, charOffset;
                var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();

                // Add on any definition locations from third party language services
                if (symbolNavigationService.WouldNavigateToSymbol(definition, solution, out filePath, out lineNumber, out charOffset))
                {
                    list.Add(new ExternalListItem(filePath, lineNumber, charOffset, definition.Name, definition.GetGlyph().GetGlyphIndex(), this.ServiceProvider));
                }
            }

            return list;
        }

        private HashSet<ValueTuple<Document, TextSpan>> AddLocations(
            Solution solution,
            List<AbstractListItem> list,
            HashSet<ValueTuple<Document, TextSpan>> uniqueLocations,
            IEnumerable<Location> locations,
            Glyph glyph)
        {
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

                uniqueLocations = uniqueLocations ?? new HashSet<ValueTuple<Document, TextSpan>>();

                if (uniqueLocations.Add(new ValueTuple<Document, TextSpan>(document, sourceSpan)))
                {
                    list.Add(new SourceListItem(document, sourceSpan, glyph.GetGlyphIndex()));
                }
            }

            return uniqueLocations;
        }
    }
}
