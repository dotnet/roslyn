// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Features.RQName;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages;

using static FindUsagesHelpers;

internal static class DefinitionItemFactory
{
    private static readonly SymbolDisplayFormat s_namePartsFormat = new(
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining);

    public static Task<DefinitionItem> ToNonClassifiedDefinitionItemAsync(
        this ISymbol definition,
        Solution solution,
        bool includeHiddenLocations,
        CancellationToken cancellationToken)
        => ToNonClassifiedDefinitionItemAsync(definition, solution, FindReferencesSearchOptions.Default, includeHiddenLocations, cancellationToken);

    public static Task<DefinitionItem> ToNonClassifiedDefinitionItemAsync(
        this ISymbol definition,
        Solution solution,
        FindReferencesSearchOptions options,
        bool includeHiddenLocations,
        CancellationToken cancellationToken)
        => ToNonClassifiedDefinitionItemAsync(definition, definition.Locations, solution, options, isPrimary: false, includeHiddenLocations, cancellationToken);

    private static async Task<DefinitionItem> ToNonClassifiedDefinitionItemAsync(
        ISymbol definition,
        ImmutableArray<Location> locations,
        Solution solution,
        FindReferencesSearchOptions options,
        bool isPrimary,
        bool includeHiddenLocations,
        CancellationToken cancellationToken)
    {
        var sourceLocations = await GetSourceLocationsAsync(
            solution, definition, locations, includeHiddenLocations, cancellationToken).ConfigureAwait(false);

        return ToDefinitionItem(
            definition,
            sourceLocations,
            sourceLocations.SelectAsArray(d => (ClassifiedSpansAndHighlightSpan?)null),
            solution,
            options,
            isPrimary);
    }

    public static async ValueTask<DefinitionItem> ToClassifiedDefinitionItemAsync(
        this ISymbol definition,
        OptionsProvider<ClassificationOptions> classificationOptions,
        Solution solution,
        FindReferencesSearchOptions options,
        bool isPrimary,
        bool includeHiddenLocations,
        CancellationToken cancellationToken)
    {
        var sourceLocations = await GetSourceLocationsAsync(solution, definition, definition.Locations, includeHiddenLocations, cancellationToken).ConfigureAwait(false);
        var classifiedSpans = await ClassifyDocumentSpansAsync(classificationOptions, sourceLocations, cancellationToken).ConfigureAwait(false);
        return ToDefinitionItem(definition, sourceLocations, classifiedSpans, solution, options, isPrimary);
    }

    public static async ValueTask<DefinitionItem> ToClassifiedDefinitionItemAsync(
        this SymbolGroup group,
        OptionsProvider<ClassificationOptions> classificationOptions,
        Solution solution,
        FindReferencesSearchOptions options,
        bool isPrimary,
        bool includeHiddenLocations,
        CancellationToken cancellationToken)
    {
        // Make a single definition item that knows about all the locations of all the symbols in the group.
        var definition = group.Symbols.First();
        var locations = group.Symbols.SelectManyAsArray(s => s.Locations);

        var sourceLocations = await GetSourceLocationsAsync(solution, definition, locations, includeHiddenLocations, cancellationToken).ConfigureAwait(false);
        var classifiedSpans = await ClassifyDocumentSpansAsync(classificationOptions, sourceLocations, cancellationToken).ConfigureAwait(false);
        return ToDefinitionItem(definition, sourceLocations, classifiedSpans, solution, options, isPrimary);
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

        var metadataLocations = GetMetadataLocations(definition, solution, out var originatingProjectId);
        if (!metadataLocations.IsEmpty)
        {
            Contract.ThrowIfNull(originatingProjectId);
            properties = properties.WithMetadataSymbolProperties(definition, originatingProjectId);
        }

        if (sourceLocations.IsEmpty && metadataLocations.IsEmpty)
        {
            // If we got no definition locations, then create a sentinel one
            // that we can display but which will not allow navigation.
            return DefinitionItem.CreateNonNavigableItem(
                tags, displayParts,
                nameDisplayParts,
                metadataLocations,
                properties, displayIfNoReferences);
        }

        var displayableProperties = AbstractReferenceFinder.GetAdditionalFindUsagesProperties(definition);

        return DefinitionItem.Create(
            tags, displayParts, sourceLocations, classifiedSpans, metadataLocations,
            nameDisplayParts, properties, displayableProperties, displayIfNoReferences);
    }

    internal static ImmutableDictionary<string, string> WithMetadataSymbolProperties(this ImmutableDictionary<string, string> properties, ISymbol symbol, ProjectId originatingProjectId)
        => properties
            .Add(DefinitionItem.MetadataSymbolKey, SymbolKey.CreateString(symbol))
            .Add(DefinitionItem.MetadataSymbolOriginatingProjectIdGuid, originatingProjectId.Id.ToString())
            .Add(DefinitionItem.MetadataSymbolOriginatingProjectIdDebugName, originatingProjectId.DebugName ?? "");

    internal static AssemblyLocation GetMetadataLocation(IAssemblySymbol assembly, Solution solution, out ProjectId originatingProjectId)
    {
        var info = solution.CompilationState.GetOriginatingProjectInfo(assembly);
        Contract.ThrowIfNull(info);
        Contract.ThrowIfNull(info.ReferencedThrough);

        originatingProjectId = info.ProjectId;
        return new AssemblyLocation(assembly.Identity.Name, assembly.Identity.Version, info.ReferencedThrough.Value.FilePath);
    }

    internal static ImmutableArray<AssemblyLocation> GetMetadataLocations(ISymbol definition, Solution solution, out ProjectId? originatingProjectId)
    {
        originatingProjectId = null;

        if (!definition.Locations.Any(static location => location.MetadataModule != null))
        {
            return [];
        }

        var assembly = definition as IAssemblySymbol ?? definition.ContainingAssembly;
        if (assembly != null)
        {
            // symbol is defined within a single metadata assembly:
            return [GetMetadataLocation(assembly, solution, out originatingProjectId)];
        }

        if (definition is INamespaceSymbol namespaceSymbol)
        {
            using var metadataLocations = TemporaryArray<AssemblyLocation>.Empty;

            // Global namespace has a metadata location for each referenced assembly.
            // It is not useful to display these locations.
            if (namespaceSymbol.IsGlobalNamespace)
            {
                return [];
            }

            // only shared namespace symbols don't have containing assembly:
            Contract.ThrowIfTrue(namespaceSymbol.ConstituentNamespaces.IsEmpty);

            foreach (var constituentNamespace in namespaceSymbol.ConstituentNamespaces)
            {
                // skip source namespace definitions:
                if (!constituentNamespace.Locations.Any(static location => location.MetadataModule != null))
                {
                    continue;
                }

                // Each constituent definition that appears in metadata has a containing metadata assembly.
                // Determine which metadata reference brought the containing assembly into the compilation
                // and display in the results the assembly name and version, and the file path of that reference.

                var containingAssembly = constituentNamespace.ContainingAssembly;
                Contract.ThrowIfNull(containingAssembly);

                var info = solution.CompilationState.GetOriginatingProjectInfo(containingAssembly);
                Contract.ThrowIfNull(info);
                Contract.ThrowIfNull(info.ReferencedThrough);
                Debug.Assert(originatingProjectId == null || originatingProjectId == info.ProjectId);

                originatingProjectId = info.ProjectId;
                metadataLocations.Add(new AssemblyLocation(containingAssembly.Identity.Name, containingAssembly.Identity.Version, info.ReferencedThrough.Value.FilePath));
            }

            return metadataLocations.ToImmutableAndClear();
        }

        return [];
    }

    private static async Task<ImmutableArray<DocumentSpan>> GetSourceLocationsAsync(
        Solution solution,
        ISymbol definition,
        ImmutableArray<Location> locations,
        bool includeHiddenLocations,
        CancellationToken cancellationToken)
    {
        // Assembly, module, global namespace and preprocessing symbol locations include all source documents; displaying all of them is not useful.
        // We could consider creating a definition item that points to the project source instead.
        if (definition is IAssemblySymbol or IModuleSymbol or INamespaceSymbol { IsGlobalNamespace: true } or IPreprocessingSymbol)
        {
            return [];
        }

        using var source = TemporaryArray<DocumentSpan>.Empty;

        foreach (var location in locations)
        {
            if (location.IsInSource &&
                solution.GetDocument(location.SourceTree) is { } document &&
                (includeHiddenLocations || document.IsRazorSourceGeneratedDocument() || location.IsVisibleSourceLocation()))
            {
                var isGeneratedCode = await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false);
                source.Add(new DocumentSpan(document, location.SourceSpan, isGeneratedCode));
            }
        }

        return source.ToImmutableAndClear();
    }

    private static ValueTask<ImmutableArray<ClassifiedSpansAndHighlightSpan?>> ClassifyDocumentSpansAsync(OptionsProvider<ClassificationOptions> optionsProvider, ImmutableArray<DocumentSpan> unclassifiedSpans, CancellationToken cancellationToken)
        => unclassifiedSpans.SelectAsArrayAsync(static async (documentSpan, optionsProvider, cancellationToken) =>
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

        // Razor has a mapping service that can map from hidden locations, or will drop results if it wants to,
        // so hidden locations aren't a problem, and are actually desirable.
        includeHiddenLocations |= referenceLocation.Document.IsRazorSourceGeneratedDocument();

        Debug.Assert(location.IsInSource);
        if (!location.IsVisibleSourceLocation() &&
            !includeHiddenLocations)
        {
            return null;
        }

        var document = referenceLocation.Document;
        var sourceSpan = location.SourceSpan;

        var options = await optionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false);

        // We don't want to classify obsolete symbols as it is very expensive, and it's not necessary for find all
        // references to strike out code in the window displaying results.
        options = options with { ClassifyObsoleteSymbols = false };

        var documentSpan = new DocumentSpan(document, sourceSpan);
        var classifiedSpans = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
            documentSpan, classifiedSpans: null, options, cancellationToken).ConfigureAwait(false);

        return new SourceReferenceItem(
            definitionItem, documentSpan, classifiedSpans, referenceLocation.SymbolUsageInfo, referenceLocation.AdditionalProperties);
    }
}
