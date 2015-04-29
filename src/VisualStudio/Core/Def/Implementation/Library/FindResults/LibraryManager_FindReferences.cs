// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        // internal for test purposes
        internal IList<AbstractTreeItem> CreateFindReferencesItems(Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols)
        {
            var definitions = new List<AbstractTreeItem>();
            var uniqueLocations = new HashSet<ValueTuple<Document, TextSpan>>();
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();

            referencedSymbols = referencedSymbols.FilterUnreferencedSyntheticDefinitions().ToList();

            foreach (var referencedSymbol in referencedSymbols.OrderBy(GetDefinitionPrecedence))
            {
                if (!IncludeDefinition(referencedSymbol))
                {
                    continue;
                }

                var definition = referencedSymbol.Definition;
                var locations = definition.Locations;

                // When finding references of a namespace, the data provided by the ReferenceFinder
                // will include one definition location for each of its exact namespace
                // declarations and each declaration of its children namespaces that mention
                // its name (e.g. definitions of A.B will include "namespace A.B.C"). The list of
                // reference locations includes both these namespace declarations and their
                // references in usings or fully qualified names. Instead of showing many top-level
                // declaration nodes (one of which will contain the full list of references
                // including declarations, the rest of which will say "0 references" due to
                // reference deduplication and there being no meaningful way to partition them),
                // we pick a single declaration to use as the top-level definition and nest all of
                // the declarations & references underneath.
                var definitionLocations = definition.IsKind(SymbolKind.Namespace)
                    ? SpecializedCollections.SingletonEnumerable(definition.Locations.First())
                    : definition.Locations;

                foreach (var definitionLocation in definitionLocations)
                {
                    var definitionItem = ConvertToDefinitionItem(solution, referencedSymbol, definitionLocation, definition.GetGlyph());
                    if (definitionItem != null)
                    {
                        definitions.Add(definitionItem);
                        var referenceItems = CreateReferenceItems(solution, uniqueLocations, referencedSymbol.Locations.Select(loc => loc.Location));
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

        /// <summary>
        /// Reference locations are deduplicated across the entire find references result set
        /// Order the definitions so that references to multiple definitions appear under the
        /// desired definition (e.g. constructor references should prefer the constructor method
        /// over the type definition). Note that this does not change the order in which
        /// definitions are displayed in Find Symbol Results, it only changes which definition
        /// a given reference should appear under when its location is a reference to multiple
        /// definitions.
        /// </summary>
        private int GetDefinitionPrecedence(ReferencedSymbol referencedSymbol)
        {
            switch (referencedSymbol.Definition.Kind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Label:
                case SymbolKind.Local:
                case SymbolKind.Method:
                case SymbolKind.Parameter:
                case SymbolKind.Property:
                case SymbolKind.RangeVariable:
                    return 0;

                case SymbolKind.ArrayType:
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                case SymbolKind.PointerType:
                    return 1;

                default:
                    return 2;
            }
        }

        private AbstractTreeItem ConvertToDefinitionItem(
            Solution solution,
            ReferencedSymbol referencedSymbol,
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
            if (!IsValidSourceLocation(document, sourceSpan))
            {
                return null;
            }

            return new SourceDefinitionTreeItem(document, sourceSpan, referencedSymbol.Definition, glyph.GetGlyphIndex());
        }

        private IList<SourceReferenceTreeItem> CreateReferenceItems(Solution solution, HashSet<ValueTuple<Document, TextSpan>> uniqueLocations, IEnumerable<Location> locations)
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
                    referenceItems.Add(new SourceReferenceTreeItem(document, sourceSpan, Glyph.Reference.GetGlyphIndex()));
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
