﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Features.RQName;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal interface IDefinitionsAndReferencesFactory : IWorkspaceService
    {
        DefinitionItem GetThirdPartyDefinitionItem(
            Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(IDefinitionsAndReferencesFactory)), Shared]
    internal class DefaultDefinitionsAndReferencesFactory : IDefinitionsAndReferencesFactory
    {
        [ImportingConstructor]
        public DefaultDefinitionsAndReferencesFactory()
        {
        }

        /// <summary>
        /// Provides an extension point that allows for other workspace layers to add additional
        /// results to the results found by the FindReferences engine.
        /// </summary>
        public virtual DefinitionItem GetThirdPartyDefinitionItem(
            Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken)
        {
            return null;
        }
    }

    internal static class DefinitionItemExtensions
    {
        public static DefinitionItem ToNonClassifiedDefinitionItem(
            this ISymbol definition,
            Project project,
            bool includeHiddenLocations)
        {
            // Because we're passing in 'false' for 'includeClassifiedSpans', this won't ever have
            // to actually do async work.  This is because the only asynchrony is when we are trying
            // to compute the classified spans for the locations of the definition.  So it's totally 
            // fine to pass in CancellationToken.None and block on the result.
            return ToDefinitionItemAsync(
                definition, project, includeHiddenLocations, includeClassifiedSpans: false,
                options: FindReferencesSearchOptions.Default, cancellationToken: CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);
        }

        public static Task<DefinitionItem> ToClassifiedDefinitionItemAsync(
            this ISymbol definition,
            Project project,
            bool includeHiddenLocations,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return ToDefinitionItemAsync(definition, project,
                includeHiddenLocations, includeClassifiedSpans: true,
                options, cancellationToken);
        }

        private static async Task<DefinitionItem> ToDefinitionItemAsync(
            this ISymbol definition,
            Project project,
            bool includeHiddenLocations,
            bool includeClassifiedSpans,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
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

            var displayParts = definition.ToDisplayParts(GetFormat(definition)).ToTaggedText();
            var nameDisplayParts = definition.ToDisplayParts(s_namePartsFormat).ToTaggedText();

            var tags = GlyphTags.GetTags(definition.GetGlyph());
            var displayIfNoReferences = definition.ShouldShowWithNoReferenceLocations(
                options, showMetadataSymbolsWithoutReferences: false);

            using var sourceLocationsDisposer = ArrayBuilder<DocumentSpan>.GetInstance(out var sourceLocations);

            var properties = GetProperties(definition);

            var displayableProperties = AbstractReferenceFinder.GetAdditionalFindUsagesProperties(definition);

            // If it's a namespace, don't create any normal location.  Namespaces
            // come from many different sources, but we'll only show a single 
            // root definition node for it.  That node won't be navigable.
            if (definition.Kind != SymbolKind.Namespace)
            {
                foreach (var location in definition.Locations)
                {
                    if (location.IsInMetadata)
                    {
                        return DefinitionItem.CreateMetadataDefinition(
                            tags, displayParts, nameDisplayParts, project,
                            definition, properties, displayIfNoReferences);
                    }
                    else if (location.IsInSource)
                    {
                        if (!location.IsVisibleSourceLocation() &&
                            !includeHiddenLocations)
                        {
                            continue;
                        }

                        var document = project.Solution.GetDocument(location.SourceTree);
                        if (document != null)
                        {
                            var documentLocation = !includeClassifiedSpans
                                ? new DocumentSpan(document, location.SourceSpan)
                                : await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(
                                    document, location.SourceSpan, cancellationToken).ConfigureAwait(false);

                            sourceLocations.Add(documentLocation);
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
                    properties, displayIfNoReferences);
            }

            return DefinitionItem.Create(
                tags, displayParts, sourceLocations.ToImmutable(),
                nameDisplayParts, properties, displayableProperties, displayIfNoReferences);
        }

        private static ImmutableDictionary<string, string> GetProperties(ISymbol definition)
        {
            var properties = ImmutableDictionary<string, string>.Empty;

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

        public static async Task<SourceReferenceItem> TryCreateSourceReferenceItemAsync(
            this ReferenceLocation referenceLocation,
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

            var documentSpan = await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(
                document, sourceSpan, cancellationToken).ConfigureAwait(false);

            return new SourceReferenceItem(definitionItem, documentSpan, referenceLocation.SymbolUsageInfo, referenceLocation.AdditionalProperties);
        }

        private static SymbolDisplayFormat GetFormat(ISymbol definition)
        {
            return definition.Kind == SymbolKind.Parameter
                ? s_parameterDefinitionFormat
                : s_definitionFormat;
        }

        private static readonly SymbolDisplayFormat s_namePartsFormat = new SymbolDisplayFormat(
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

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

        private static readonly SymbolDisplayFormat s_parameterDefinitionFormat = s_definitionFormat
            .AddParameterOptions(SymbolDisplayParameterOptions.IncludeName);
    }
}
