// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal interface IDefinitionsAndReferencesFactory : IWorkspaceService
    {
        DefinitionsAndReferences CreateDefinitionsAndReferences(
            Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols);
    }

    [ExportWorkspaceService(typeof(IDefinitionsAndReferencesFactory)), Shared]
    internal class DefaultDefinitionsAndReferencesFactory : IDefinitionsAndReferencesFactory
    {
        public DefinitionsAndReferences CreateDefinitionsAndReferences(
            Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols)
        {
            var definitions = ImmutableArray.CreateBuilder<DefinitionItem>();
            var references = ImmutableArray.CreateBuilder<SourceReferenceItem>();

            foreach (var referencedSymbol in referencedSymbols.OrderBy(GetPrecedence))
            {
                ProcessReferencedSymbol(solution, referencedSymbol, definitions, references);
            }

            return new DefinitionsAndReferences(definitions.ToImmutable(), references.ToImmutable());
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
        private static int GetPrecedence(ReferencedSymbol referencedSymbol)
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

        private void ProcessReferencedSymbol(
            Solution solution,
            ReferencedSymbol referencedSymbol,
            ImmutableArray<DefinitionItem>.Builder definitions,
            ImmutableArray<SourceReferenceItem>.Builder references)
        {
            if (!referencedSymbol.ShouldShow())
            {
                return;
            }

            var definitionItem = CreateDefinitionItem(solution, referencedSymbol);
            if (definitionItem == null)
            {
                return;
            }

            definitions.Add(definitionItem);
            CreateReferences(referencedSymbol, references, definitionItem);

            var thirdPartyItem = GetThirdPartyDefinitionItem(solution, referencedSymbol.Definition);
            if (thirdPartyItem != null)
            {
                definitions.Add(thirdPartyItem);
            }
        }

        protected virtual DefinitionItem GetThirdPartyDefinitionItem(
            Solution solution, ISymbol definition)
        {
            return null;
        }

        private static DefinitionItem CreateDefinitionItem(
            Solution solution, ReferencedSymbol referencedSymbol)
        {
            var definition = referencedSymbol.Definition;

            var locations = FilterDefinitionLocations(definition);
            var definitionLocations = ConvertLocations(solution, referencedSymbol, locations);
            if (definitionLocations.IsEmpty)
            {
                return null;
            }

            var displayParts = definition.ToDisplayParts(FindReferencesUtilities.DefinitionDisplayFormat).ToTaggedText();

            return new DefinitionItem(
                GlyphTags.GetTags(definition.GetGlyph()),
                displayParts,
                definitionLocations,
                definition.ShouldShowWithNoReferenceLocations());
        }

        private static ImmutableArray<DefinitionLocation> ConvertLocations(
            Solution solution, ReferencedSymbol referencedSymbol, ImmutableArray<Location> locations)
        {
            var definition = referencedSymbol.Definition;
            var result = ImmutableArray.CreateBuilder<DefinitionLocation>();

            foreach (var location in locations)
            {
                if (location.IsInMetadata)
                {
                    var firstSourceReferenceLocation = referencedSymbol.Locations.FirstOrDefault();
                    if (firstSourceReferenceLocation != null)
                    {
                        result.Add(DefinitionLocation.CreateSymbolLocation(
                            definition, firstSourceReferenceLocation.Document.Project));
                    }
                    else
                    {
                        result.Add(DefinitionLocation.CreateNonNavigatingLocation(
                            DefinitionLocation.GetOriginationParts(definition)));
                    }
                }
                else if (location.IsInSource)
                {
                    var document = solution.GetDocument(location.SourceTree);
                    if (document != null)
                    {
                        var documentLocation = new DocumentLocation(document, location.SourceSpan);
                        if (documentLocation.CanNavigateTo())
                        {
                            result.Add(DefinitionLocation.CreateDocumentLocation(documentLocation));
                        }
                    }
                }
            }

            return result.ToImmutable();
        }

        private static ImmutableArray<Location> FilterDefinitionLocations(ISymbol definition)
        {
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
            if (definition.IsKind(SymbolKind.Namespace))
            {
                // Prefer source location over metadata.
                var firstLocation = definition.Locations.FirstOrDefault(loc => loc.IsInSource) ?? definition.Locations.First();
                return ImmutableArray.Create(firstLocation);
            }

            return definition.Locations;
        }

        private static void CreateReferences(
            ReferencedSymbol referencedSymbol,
            ImmutableArray<SourceReferenceItem>.Builder references,
            DefinitionItem definitionItem)
        {
            foreach (var referenceLocation in referencedSymbol.Locations)
            {
                var location = referenceLocation.Location;
                if (!location.IsInSource)
                {
                    continue;
                }

                var documentLocation = new DocumentLocation(
                    referenceLocation.Document, referenceLocation.Location.SourceSpan);
                if (!documentLocation.CanNavigateTo())
                {
                    continue;
                }

                var referenceItem = new SourceReferenceItem(definitionItem, documentLocation);
                references.Add(referenceItem);
            }
        }
    }
}