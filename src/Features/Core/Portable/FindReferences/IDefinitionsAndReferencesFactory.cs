// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindReferences
{
    internal interface IDefinitionsAndReferencesFactory : IWorkspaceService
    {
        DefinitionsAndReferences CreateDefinitionsAndReferences(
            Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols);

        DefinitionItem GetThirdPartyDefinitionItem(Solution solution, ISymbol definition);
    }

    [ExportWorkspaceService(typeof(IDefinitionsAndReferencesFactory)), Shared]
    internal class DefaultDefinitionsAndReferencesFactory : IDefinitionsAndReferencesFactory
    {
        public DefinitionsAndReferences CreateDefinitionsAndReferences(
            Solution solution, IEnumerable<ReferencedSymbol> referencedSymbols)
        {
            var definitions = ArrayBuilder<DefinitionItem>.GetInstance();
            var references = ArrayBuilder<SourceReferenceItem>.GetInstance();

            var uniqueLocations = new HashSet<DocumentSpan>();

            // Order the symbols by precedence, then create the appropriate
            // definition item per symbol and all refernece items for its
            // reference locations.
            foreach (var referencedSymbol in referencedSymbols.OrderBy(GetPrecedence))
            {
                ProcessReferencedSymbol(
                    solution, referencedSymbol, definitions, references, uniqueLocations);
            }

            return new DefinitionsAndReferences(
                definitions.ToImmutableAndFree(), references.ToImmutableAndFree());
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
            ArrayBuilder<DefinitionItem> definitions,
            ArrayBuilder<SourceReferenceItem> references,
            HashSet<DocumentSpan> uniqueSpans)
        {
            // See if this is a symbol we even want to present to the user.  If not,
            // ignore it entirely (including all its reference locations).
            if (!referencedSymbol.ShouldShow())
            {
                return;
            }

            var definitionItem = referencedSymbol.Definition.ToDefinitionItem(solution, uniqueSpans);
            definitions.Add(definitionItem);

            // Now, create the SourceReferenceItems for all the reference locations
            // for this definition.
            CreateReferences(referencedSymbol, references, definitionItem, uniqueSpans);

            // Finally, see if there are any third parties that want to add their
            // own result to our collection.
            var thirdPartyItem = GetThirdPartyDefinitionItem(solution, referencedSymbol.Definition);
            if (thirdPartyItem != null)
            {
                definitions.Add(thirdPartyItem);
            }
        }

        /// <summary>
        /// Provides an extension point that allows for other workspace layers to add additional
        /// results to the results found by the FindReferences engine.
        /// </summary>
        public virtual DefinitionItem GetThirdPartyDefinitionItem(
            Solution solution, ISymbol definition)
        {
            return null;
        }

        private static void CreateReferences(
            ReferencedSymbol referencedSymbol,
            ArrayBuilder<SourceReferenceItem> references,
            DefinitionItem definitionItem,
            HashSet<DocumentSpan> uniqueSpans)
        {
            foreach (var referenceLocation in referencedSymbol.Locations)
            {
                var sourceReferenceItem = referenceLocation.TryCreateSourceReferenceItem(definitionItem);
                if (sourceReferenceItem == null)
                {
                    continue;
                }

                if (uniqueSpans.Add(sourceReferenceItem.SourceSpan))
                {
                    references.Add(sourceReferenceItem);
                }
            }
        }
    }

    internal static class DefinitionItemExtensions
    {
        public static DefinitionItem ToDefinitionItem(
            this ISymbol definition,
            Solution solution,
            HashSet<DocumentSpan> uniqueSpans = null)
        {
            var displayParts = definition.ToDisplayParts(GetFormat(definition)).ToTaggedText();

            var tags = GlyphTags.GetTags(definition.GetGlyph());
            var displayIfNoReferences = definition.ShouldShowWithNoReferenceLocations(
                showMetadataSymbolsWithoutReferences: false);

            var sourceLocations = ArrayBuilder<DocumentSpan>.GetInstance();

            // If it's a namespace, don't create any normal lcoation.  Namespaces
            // come from many different sources, but we'll only show a single 
            // root definition node for it.  That node won't be navigable.
            if (definition.Kind != SymbolKind.Namespace)
            {
                foreach (var location in definition.Locations)
                {
                    if (location.IsInMetadata)
                    {
                        return DefinitionItem.CreateMetadataDefinition(
                            tags, displayParts, solution, definition, displayIfNoReferences);
                    }
                    else if (location.IsVisibleSourceLocation())
                    {
                        var document = solution.GetDocument(location.SourceTree);
                        if (document != null)
                        {
                            var documentLocation = new DocumentSpan(document, location.SourceSpan);
                            if (sourceLocations.Count == 0)
                            {
                                sourceLocations.Add(documentLocation);
                            }
                            else
                            {
                                if (uniqueSpans == null ||
                                    uniqueSpans.Add(documentLocation))
                                {
                                    sourceLocations.Add(documentLocation);
                                }
                            }
                        }
                    }
                }
            }

            if (sourceLocations.Count == 0)
            {
                // If we got no definition locations, then create a sentinel one
                // that we can display but which will not allow navigation.
                return DefinitionItem.CreateNonNavigableItem(
                    tags, displayParts,
                    DefinitionItem.GetOriginationParts(definition),
                    displayIfNoReferences);
            }

            return DefinitionItem.Create(
                tags, displayParts, sourceLocations.ToImmutableAndFree(), displayIfNoReferences);
        }

        public static SourceReferenceItem TryCreateSourceReferenceItem(
            this ReferenceLocation referenceLocation,
            DefinitionItem definitionItem)
        {
            var location = referenceLocation.Location;

            Debug.Assert(location.IsInSource);
            if (!location.IsVisibleSourceLocation())
            {
                return null;
            }

            return new SourceReferenceItem(definitionItem, 
                new DocumentSpan(referenceLocation.Document, location.SourceSpan));
        }

        private static SymbolDisplayFormat GetFormat(ISymbol definition)
        {
            return definition.Kind == SymbolKind.Parameter
                ? s_parameterDefinitionFormat
                : s_definitionFormat;
        }

        private static readonly SymbolDisplayFormat s_definitionFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword | SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
                localOptions: SymbolDisplayLocalOptions.IncludeType,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static SymbolDisplayFormat s_parameterDefinitionFormat = s_definitionFormat
            .AddParameterOptions(SymbolDisplayParameterOptions.IncludeName);
    }
}