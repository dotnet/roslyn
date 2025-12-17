// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentHighlighting;

internal abstract partial class AbstractDocumentHighlightsService :
    AbstractEmbeddedLanguageFeatureService<IEmbeddedLanguageDocumentHighlighter>,
    IDocumentHighlightsService
{
    protected AbstractDocumentHighlightsService(
        string languageName,
        EmbeddedLanguageInfo info,
        ISyntaxKinds syntaxKinds,
        IEnumerable<Lazy<IEmbeddedLanguageDocumentHighlighter, EmbeddedLanguageMetadata>> allServices)
        : base(languageName, info, syntaxKinds, allServices)
    {
    }

    public async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(
        Document document, int position, IImmutableSet<Document> documentsToSearch, HighlightingOptions options, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            // Call the project overload.  We don't need the full solution synchronized over to the OOP
            // in order to highlight values in this document.
            var result = await client.TryInvokeAsync<IRemoteDocumentHighlightsService, ImmutableArray<SerializableDocumentHighlights>>(
                document.Project,
                (service, solutionInfo, cancellationToken) => service.GetDocumentHighlightsAsync(solutionInfo, document.Id, position, documentsToSearch.SelectAsArray(d => d.Id), options, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
            {
                return [];
            }

            return await result.Value.SelectAsArrayAsync(h => h.RehydrateAsync(solution, cancellationToken)).ConfigureAwait(false);
        }

        return await GetDocumentHighlightsInCurrentProcessAsync(
            document, position, documentsToSearch, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsInCurrentProcessAsync(
        Document document, int position, IImmutableSet<Document> documentsToSearch, HighlightingOptions options, CancellationToken cancellationToken)
    {
        // Document highlights are not impacted by nullable analysis.  Get a semantic model with nullability disabled to
        // lower the amount of work we need to do here.
        var semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var result = TryGetEmbeddedLanguageHighlights(document, semanticModel, position, options, cancellationToken);
        if (!result.IsDefaultOrEmpty)
            return result;

        var solution = document.Project.Solution;

        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
            semanticModel, position, solution.Services, cancellationToken).ConfigureAwait(false);
        if (symbol == null)
            return [];

        // Get unique tags for referenced symbols
        var tags = await GetTagsForReferencedSymbolAsync(
            symbol, document, documentsToSearch, cancellationToken).ConfigureAwait(false);

        // Only accept these highlights if at least one of them actually intersected with the 
        // position the caller was asking for.  For example, if the user had `$$new X();` then 
        // SymbolFinder will consider that the symbol `X`. However, the doc highlights won't include
        // the `new` part, so it's not appropriate for us to highlight `X` in that case.
        if (!tags.Any(static (t, position) => t.HighlightSpans.Any(static (hs, position) => hs.TextSpan.IntersectsWith(position), position), position))
            return [];

        return tags;
    }

    private ImmutableArray<DocumentHighlights> TryGetEmbeddedLanguageHighlights(
        Document document, SemanticModel semanticModel, int position, HighlightingOptions options, CancellationToken cancellationToken)
    {
        var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        var token = root.FindToken(position);
        var (embeddedHighlightsServices, _) = this.GetServices(semanticModel, token, cancellationToken);
        foreach (var service in embeddedHighlightsServices)
        {
            var result = service.Value.GetDocumentHighlights(
                document, semanticModel, token, position, options, cancellationToken);
            if (!result.IsDefaultOrEmpty)
                return result;
        }

        return default;
    }

    private async Task<ImmutableArray<DocumentHighlights>> GetTagsForReferencedSymbolAsync(
        ISymbol symbol,
        Document document,
        IImmutableSet<Document> documentsToSearch,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(symbol);
        if (ShouldConsiderSymbol(symbol))
        {
            var progress = new StreamingProgressCollector();

            // We're running in the background.  So set us as 'Explicit = false' to avoid running in parallel and
            // using too many resources.
            var options = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(symbol) with { Explicit = false };
            await SymbolFinder.FindReferencesInDocumentsInCurrentProcessAsync(
                symbol, document.Project.Solution, progress,
                documentsToSearch, options, cancellationToken).ConfigureAwait(false);

            return await FilterAndCreateSpansAsync(
                progress.GetReferencedSymbols(), document, documentsToSearch,
                symbol, options, cancellationToken).ConfigureAwait(false);
        }

        return [];
    }

    private static bool ShouldConsiderSymbol(ISymbol symbol)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.Method:
                switch (((IMethodSymbol)symbol).MethodKind)
                {
                    case MethodKind.AnonymousFunction:
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                    case MethodKind.EventAdd:
                    case MethodKind.EventRaise:
                    case MethodKind.EventRemove:
                        return false;

                    default:
                        return true;
                }

            default:
                return true;
        }
    }

    private async Task<ImmutableArray<DocumentHighlights>> FilterAndCreateSpansAsync(
        ImmutableArray<ReferencedSymbol> references, Document startingDocument,
        IImmutableSet<Document> documentsToSearch, ISymbol symbol,
        FindReferencesSearchOptions options, CancellationToken cancellationToken)
    {
        var solution = startingDocument.Project.Solution;
        references = references.FilterToItemsToShow(options);
        references = references.FilterNonMatchingMethodNames(solution, symbol);
        references = references.FilterToAliasMatches(symbol as IAliasSymbol);

        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
        {
            var constructorParts1 = constructor.OriginalDefinition.GetAllMethodSymbolsOfPartialParts();
            references = references.WhereAsArray(r =>
            {
                var constructorParts2 = ((IMethodSymbol)r.Definition).GetAllMethodSymbolsOfPartialParts();
                return constructorParts1.Intersect(constructorParts2).Any();
            });
        }

        using var _ = ArrayBuilder<Location>.GetInstance(out var additionalReferences);

        foreach (var currentDocument in documentsToSearch)
        {
            // 'documentsToSearch' may contain documents from languages other than our own
            // (for example cshtml files when we're searching the cs document).  Since we're
            // delegating to a virtual method for this language type, we have to make sure
            // we only process the document if it's also our language.
            if (currentDocument.Project.Language == startingDocument.Project.Language)
            {
                additionalReferences.AddRange(await GetAdditionalReferencesAsync(currentDocument, symbol, cancellationToken).ConfigureAwait(false));
            }
        }

        return await CreateSpansAsync(
            solution, symbol, references, additionalReferences,
            documentsToSearch, cancellationToken).ConfigureAwait(false);
    }

    protected virtual async Task<ImmutableArray<Location>> GetAdditionalReferencesAsync(
        Document document, ISymbol symbol, CancellationToken cancellationToken)
    {
        return [];
    }

    private static async Task<ImmutableArray<DocumentHighlights>> CreateSpansAsync(
        Solution solution,
        ISymbol symbol,
        IEnumerable<ReferencedSymbol> references,
        ArrayBuilder<Location> additionalReferences,
        IImmutableSet<Document> documentToSearch,
        CancellationToken cancellationToken)
    {
        var spanSet = new HashSet<DocumentSpan>();
        var tagMap = new MultiDictionary<Document, HighlightSpan>();
        var addAllDefinitions = true;

        // Add definitions
        // Filter out definitions that cannot be highlighted. e.g: alias symbols defined via project property pages.
        if (symbol.Kind == SymbolKind.Alias &&
            symbol.Locations.Length > 0)
        {
            addAllDefinitions = false;

            if (symbol.Locations.First().IsInSource)
            {
                // For alias symbol we want to get the tag only for the alias definition, not the target symbol's definition.
                await AddLocationSpanAsync(symbol.Locations.First(), solution, spanSet, tagMap, HighlightSpanKind.Definition, cancellationToken).ConfigureAwait(false);
            }
        }

        // Add references and definitions
        foreach (var reference in references)
        {
            if (addAllDefinitions && ShouldIncludeDefinition(reference.Definition))
            {
                foreach (var location in reference.Definition.Locations)
                {
                    if (location.IsInSource)
                    {
                        var document = solution.GetDocument(location.SourceTree);

                        // GetDocument will return null for locations in #load'ed trees.
                        // TODO:  Remove this check and add logic to fetch the #load'ed tree's
                        // Document once https://github.com/dotnet/roslyn/issues/5260 is fixed.
                        if (document == null)
                        {
                            Debug.Assert(solution.WorkspaceKind is WorkspaceKind.Interactive or WorkspaceKind.MiscellaneousFiles);
                            continue;
                        }

                        if (documentToSearch.Contains(document))
                        {
                            await AddLocationSpanAsync(location, solution, spanSet, tagMap, HighlightSpanKind.Definition, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            foreach (var referenceLocation in reference.Locations)
            {
                var referenceKind = referenceLocation.IsWrittenTo ? HighlightSpanKind.WrittenReference : HighlightSpanKind.Reference;
                await AddLocationSpanAsync(referenceLocation.Location, solution, spanSet, tagMap, referenceKind, cancellationToken).ConfigureAwait(false);
            }
        }

        // Add additional references
        foreach (var location in additionalReferences)
        {
            await AddLocationSpanAsync(location, solution, spanSet, tagMap, HighlightSpanKind.Reference, cancellationToken).ConfigureAwait(false);
        }

        var list = new FixedSizeArrayBuilder<DocumentHighlights>(tagMap.Count);
        foreach (var kvp in tagMap)
            list.Add(new DocumentHighlights(kvp.Key, [.. kvp.Value]));

        return list.MoveToImmutable();
    }

    private static bool ShouldIncludeDefinition(ISymbol symbol)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.Namespace:
                return false;

            case SymbolKind.NamedType:
                return !((INamedTypeSymbol)symbol).IsScriptClass;
        }

        return true;
    }

    private static async Task AddLocationSpanAsync(Location location, Solution solution, HashSet<DocumentSpan> spanSet, MultiDictionary<Document, HighlightSpan> tagList, HighlightSpanKind kind, CancellationToken cancellationToken)
    {
        var span = await GetLocationSpanAsync(solution, location, cancellationToken).ConfigureAwait(false);
        if (span != null && !spanSet.Contains(span.Value))
        {
            spanSet.Add(span.Value);
            tagList.Add(span.Value.Document, new HighlightSpan(span.Value.SourceSpan, kind));
        }
    }

    private static async Task<DocumentSpan?> GetLocationSpanAsync(
        Solution solution, Location location, CancellationToken cancellationToken)
    {
        try
        {
            if (location != null && location.IsInSource)
            {
                var tree = location.SourceTree;

                var document = solution.GetRequiredDocument(tree);
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

                if (syntaxFacts != null)
                {
                    // Specify findInsideTrivia: true to ensure that we search within XML doc comments.
                    var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    var token = root.FindToken(location.SourceSpan.Start, findInsideTrivia: true);

                    return syntaxFacts.IsGenericName(token.Parent) || syntaxFacts.IsIndexerMemberCref(token.Parent)
                        ? new DocumentSpan(document, token.Span)
                        : new DocumentSpan(document, location.SourceSpan);
                }
            }
        }
        catch (NullReferenceException e) when (FatalError.ReportAndCatch(e))
        {
            // We currently are seeing a strange null references crash in this code.  We have
            // a strong belief that this is recoverable, but we'd like to know why it is 
            // happening.  This exception filter allows us to report the issue and continue
            // without damaging the user experience.  Once we get more crash reports, we
            // can figure out the root cause and address appropriately.  This is preferable
            // to just using conditionl access operators to be resilient (as we won't actually
            // know why this is happening).
        }

        return null;
    }
}
