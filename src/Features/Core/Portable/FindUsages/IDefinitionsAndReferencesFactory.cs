// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Features.RQName;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    using static FindUsagesHelpers;

    internal interface IDefinitionsAndReferencesFactory : IWorkspaceService
    {
        Task<DefinitionItem?> GetThirdPartyDefinitionItemAsync(
            Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(IDefinitionsAndReferencesFactory)), Shared]
    internal class DefaultDefinitionsAndReferencesFactory : IDefinitionsAndReferencesFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultDefinitionsAndReferencesFactory()
        {
        }

        /// <summary>
        /// Provides an extension point that allows for other workspace layers to add additional
        /// results to the results found by the FindReferences engine.
        /// </summary>
        public virtual Task<DefinitionItem?> GetThirdPartyDefinitionItemAsync(
            Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken)
        {
            return SpecializedTasks.Null<DefinitionItem>();
        }
    }

    internal static class DefinitionItemExtensions
    {
        private static readonly SymbolDisplayFormat s_namePartsFormat = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

        public static DefinitionItem ToNonClassifiedDefinitionItem(
            this ISymbol definition,
            Solution solution,
            bool includeHiddenLocations)
            => ToNonClassifiedDefinitionItem(definition, solution, FindReferencesSearchOptions.Default, includeHiddenLocations);

        public static DefinitionItem ToNonClassifiedDefinitionItem(
            this ISymbol definition,
            Solution solution,
            FindReferencesSearchOptions options,
            bool includeHiddenLocations)
            => ToNonClassifiedDefinitionItem(definition, definition.Locations, solution, options, isPrimary: false, includeHiddenLocations);

        private static DefinitionItem ToNonClassifiedDefinitionItem(
            ISymbol definition,
            ImmutableArray<Location> locations,
            Solution solution,
            FindReferencesSearchOptions options,
            bool isPrimary,
            bool includeHiddenLocations)
            => ToDefinitionItem(definition, TryGetSourceLocations(definition, solution, locations, includeHiddenLocations), solution, options, isPrimary);

        public static async ValueTask<DefinitionItem> ToClassifiedDefinitionItemAsync(
            this ISymbol definition,
            OptionsProvider<ClassificationOptions> classificationOptions,
            Solution solution,
            FindReferencesSearchOptions options,
            bool isPrimary,
            bool includeHiddenLocations,
            CancellationToken cancellationToken)
        {
            var unclassifiedSpans = TryGetSourceLocations(definition, solution, definition.Locations, includeHiddenLocations);
            var classifiedSpans = unclassifiedSpans.IsDefault ? default : await ClassifyDocumentSpansAsync(classificationOptions, unclassifiedSpans, cancellationToken).ConfigureAwait(false);

            return ToDefinitionItem(definition, unclassifiedSpans, classifiedSpans, solution, options, isPrimary);
        }

        public static async ValueTask<DefinitionItem> ToClassifiedDefinitionItemAsync(
            this SymbolGroup group, OptionsProvider<ClassificationOptions> classificationOptions, Solution solution, FindReferencesSearchOptions options, bool isPrimary, bool includeHiddenLocations, CancellationToken cancellationToken)
        {
            // Make a single definition item that knows about all the locations of all the symbols in the group.
            var definition = group.Symbols.First();

            var allLocations = group.Symbols.SelectManyAsArray(s => s.Locations);
            var unclassifiedSpans = TryGetSourceLocations(definition, solution, allLocations, includeHiddenLocations);
            var classifiedSpans = unclassifiedSpans.IsDefault ? default : await ClassifyDocumentSpansAsync(classificationOptions, unclassifiedSpans, cancellationToken).ConfigureAwait(false);

            return ToDefinitionItem(definition, unclassifiedSpans, classifiedSpans, solution, options, isPrimary);
        }

        private static DefinitionItem ToDefinitionItem(
            ISymbol definition,
            ImmutableArray<DocumentSpan> sourceLocations,
            Solution solution,
            FindReferencesSearchOptions options,
            bool isPrimary)
        {
            return ToDefinitionItem(
                definition,
                sourceLocations,
                sourceLocations.IsDefault ? default : sourceLocations.SelectAsArray(d => (ClassifiedSpansAndHighlightSpan?)null),
                solution,
                options,
                isPrimary);
        }

        private static DefinitionItem ToDefinitionItem(
            ISymbol definition,
            ImmutableArray<DocumentSpan> sourceLocations,
            ImmutableArray<ClassifiedSpansAndHighlightSpan?> classifiedSpans,
            Solution solution,
            FindReferencesSearchOptions options,
            bool isPrimary)
        {
            // Ensure we're working with the original definition for the symbol. I.e. When we're 
            // creating definition items, we want to create them for types like Dictionary<TKey,TValue>
            // not some random instantiation of that type.  
            //
            // This ensures that the type will both display properly to the user, as well as ensuring
            // that we can accurately resolve the type later on when we try to navigate to it.
            if (!definition.IsTupleField())
            {
                // In an earlier implementation of the compiler APIs, tuples and tuple fields symbols were definitions
                // We pretend this is still the case
                definition = definition.OriginalDefinition;
            }

            var displayParts = GetDisplayParts(definition);
            var nameDisplayParts = definition.ToDisplayParts(s_namePartsFormat).ToTaggedText();

            var tags = GlyphTags.GetTags(definition.GetGlyph());
            var displayIfNoReferences = definition.ShouldShowWithNoReferenceLocations(
                options, showMetadataSymbolsWithoutReferences: false);

            var properties = GetProperties(definition, isPrimary);

            if (sourceLocations.IsDefault || definition.IsTupleType())
            {
                // If the location is in metadata, then create a metadata definition.
                // A special case is the tuple type, where its locations are preserved in the original definition.
                return DefinitionItem.CreateMetadataDefinition(
                    tags, displayParts, nameDisplayParts, solution,
                    definition, properties, displayIfNoReferences);
            }

            if (sourceLocations.IsEmpty)
            {
                // If we got no definition locations, then create a sentinel one
                // that we can display but which will not allow navigation.
                return DefinitionItem.CreateNonNavigableItem(
                    tags, displayParts,
                    DefinitionItem.GetOriginationParts(definition),
                    properties, displayIfNoReferences);
            }

            var displayableProperties = AbstractReferenceFinder.GetAdditionalFindUsagesProperties(definition);

            return DefinitionItem.Create(
                tags, displayParts, sourceLocations, classifiedSpans,
                nameDisplayParts, properties, displayableProperties, displayIfNoReferences);
        }

        private static ImmutableArray<DocumentSpan> TryGetSourceLocations(ISymbol definition, Solution solution, ImmutableArray<Location> locations, bool includeHiddenLocations)
        {
            // If it's a namespace, don't create any normal location.  Namespaces
            // come from many different sources, but we'll only show a single 
            // root definition node for it.  That node won't be navigable.
            if (definition.Kind == SymbolKind.Namespace)
            {
                return [];
            }

            // If it's a namespace, don't create any normal location.  Namespaces
            // come from many different sources, but we'll only show a single 
            // root definition node for it.  That node won't be navigable.
            using var sourceLocations = TemporaryArray<DocumentSpan>.Empty;

            foreach (var location in locations)
            {
                if (location.IsInMetadata)
                {
                    return default;
                }

                if (location.IsInSource)
                {
                    if (!location.IsVisibleSourceLocation() &&
                        !includeHiddenLocations)
                    {
                        continue;
                    }

                    var document = solution.GetDocument(location.SourceTree);
                    if (document != null)
                    {
                        sourceLocations.Add(new DocumentSpan(document, location.SourceSpan));
                    }
                }
            }

            return sourceLocations.ToImmutableAndClear();
        }

        private static ValueTask<ImmutableArray<ClassifiedSpansAndHighlightSpan?>> ClassifyDocumentSpansAsync(OptionsProvider<ClassificationOptions> optionsProvider, ImmutableArray<DocumentSpan> unclassifiedSpans, CancellationToken cancellationToken)
            => unclassifiedSpans.SelectAsArrayAsync(async (documentSpan, optionsProvider, cancellationToken) =>
            {
                var options = await optionsProvider.GetOptionsAsync(documentSpan.Document.Project.Services, cancellationToken).ConfigureAwait(false);
                return (ClassifiedSpansAndHighlightSpan?)await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, classifiedSpans: null, options, cancellationToken).ConfigureAwait(false);
            }, optionsProvider, cancellationToken);

        private static ImmutableDictionary<string, string> GetProperties(ISymbol definition, bool isPrimary)
        {
            var properties = ImmutableDictionary<string, string>.Empty;

            if (isPrimary)
            {
                properties = properties.Add(DefinitionItem.Primary, "");
            }

            var rqName = RQNameInternal.From(definition);
            if (rqName != null)
            {
                properties = properties.Add(DefinitionItem.RQNameKey1, rqName);
            }

            if (definition?.IsConstructor() == true)
            {
                // If the symbol being considered is a constructor include the containing type in case
                // a third party wants to navigate to that.
                rqName = RQNameInternal.From(definition.ContainingType);
                if (rqName != null)
                {
                    properties = properties.Add(DefinitionItem.RQNameKey2, rqName);
                }
            }

            return properties;
        }

        public static async Task<SourceReferenceItem?> TryCreateSourceReferenceItemAsync(
            this ReferenceLocation referenceLocation,
            OptionsProvider<ClassificationOptions> optionsProvider,
            DefinitionItem definitionItem,
            bool includeHiddenLocations,
            CancellationToken cancellationToken)
        {
            var location = referenceLocation.Location;

            Debug.Assert(location.IsInSource);
            if (!location.IsVisibleSourceLocation() &&
                !includeHiddenLocations)
            {
                return null;
            }

            var document = referenceLocation.Document;
            var sourceSpan = location.SourceSpan;

            var options = await optionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false);

            var documentSpan = new DocumentSpan(document, sourceSpan);
            var classifiedSpans = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                documentSpan, classifiedSpans: null, options, cancellationToken).ConfigureAwait(false);

            return new SourceReferenceItem(
                definitionItem, documentSpan, classifiedSpans, referenceLocation.SymbolUsageInfo, referenceLocation.AdditionalProperties);
        }
    }
}
