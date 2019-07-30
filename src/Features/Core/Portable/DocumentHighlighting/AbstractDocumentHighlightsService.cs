// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal abstract partial class AbstractDocumentHighlightsService : IDocumentHighlightsService
    {
        public async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(
            Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            var (succeeded, highlights) = await GetDocumentHighlightsInRemoteProcessAsync(
                document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);

            if (succeeded)
            {
                return highlights;
            }

            return await GetDocumentHighlightsInCurrentProcessAsync(
                document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(bool succeeded, ImmutableArray<DocumentHighlights> highlights)> GetDocumentHighlightsInRemoteProcessAsync(
            Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            var result = await document.Project.Solution.TryRunCodeAnalysisRemoteAsync<IList<SerializableDocumentHighlights>>(
                nameof(IRemoteDocumentHighlights.GetDocumentHighlightsAsync),
                new object[]
                {
                    document.Id,
                    position,
                    documentsToSearch.Select(d => d.Id).ToArray()
                },
                cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return (succeeded: false, ImmutableArray<DocumentHighlights>.Empty);
            }

            return (true, result.SelectAsArray(h => h.Rehydrate(document.Project.Solution)));
        }

        private async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsInCurrentProcessAsync(
            Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            var result = await TryGetEmbeddedLanguageHighlightsAsync(
                document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);
            if (!result.IsDefaultOrEmpty)
            {
                return result;
            }

            // use speculative semantic model to see whether we are on a symbol we can do HR
            var span = new TextSpan(position, 0);
            var solution = document.Project.Solution;

            var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
                semanticModel, position, solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (symbol == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            symbol = await GetSymbolToSearchAsync(document, position, semanticModel, symbol, cancellationToken).ConfigureAwait(false);
            if (symbol == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            // Get unique tags for referenced symbols
            return await GetTagsForReferencedSymbolAsync(
                new SymbolAndProjectId(symbol, document.Project.Id),
                document, documentsToSearch, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<DocumentHighlights>> TryGetEmbeddedLanguageHighlightsAsync(
            Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            var languagesProvider = document.GetLanguageService<IEmbeddedLanguagesProvider>();
            if (languagesProvider != null)
            {
                foreach (var language in languagesProvider.Languages)
                {
                    var highlighter = (language as IEmbeddedLanguageFeatures)?.DocumentHighlightsService;
                    if (highlighter != null)
                    {
                        var highlights = await highlighter.GetDocumentHighlightsAsync(
                            document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);

                        if (!highlights.IsDefaultOrEmpty)
                        {
                            return highlights;
                        }
                    }
                }
            }

            return default;
        }

        private static async Task<ISymbol> GetSymbolToSearchAsync(Document document, int position, SemanticModel semanticModel, ISymbol symbol, CancellationToken cancellationToken)
        {
            // see whether we can use the symbol as it is
            var currentSemanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (currentSemanticModel == semanticModel)
            {
                return symbol;
            }

            // get symbols from current document again
            return await SymbolFinder.FindSymbolAtPositionAsync(currentSemanticModel, position, document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<DocumentHighlights>> GetTagsForReferencedSymbolAsync(
            SymbolAndProjectId symbolAndProjectId,
            Document document,
            IImmutableSet<Document> documentsToSearch,
            CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            Contract.ThrowIfNull(symbol);
            if (ShouldConsiderSymbol(symbol))
            {
                var progress = new StreamingProgressCollector(
                    StreamingFindReferencesProgress.Instance);

                var options = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(symbol);
                await SymbolFinder.FindReferencesAsync(
                    symbolAndProjectId, document.Project.Solution, progress,
                    documentsToSearch, options, cancellationToken).ConfigureAwait(false);

                return await FilterAndCreateSpansAsync(
                    progress.GetReferencedSymbols(), document, documentsToSearch,
                    symbol, options, cancellationToken).ConfigureAwait(false);
            }

            return ImmutableArray<DocumentHighlights>.Empty;
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
            IEnumerable<ReferencedSymbol> references, Document startingDocument,
            IImmutableSet<Document> documentsToSearch, ISymbol symbol,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            var solution = startingDocument.Project.Solution;

            references = references.FilterToItemsToShow(options);
            references = references.FilterNonMatchingMethodNames(solution, symbol);
            references = references.FilterToAliasMatches(symbol as IAliasSymbol);

            if (symbol.IsConstructor())
            {
                references = references.Where(r => r.Definition.OriginalDefinition.Equals(symbol.OriginalDefinition));
            }

            var additionalReferences = new List<Location>();

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

        protected virtual Task<ImmutableArray<Location>> GetAdditionalReferencesAsync(
            Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<Location>();
        }

        private static async Task<ImmutableArray<DocumentHighlights>> CreateSpansAsync(
            Solution solution,
            ISymbol symbol,
            IEnumerable<ReferencedSymbol> references,
            IEnumerable<Location> additionalReferences,
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
                    await AddLocationSpan(symbol.Locations.First(), solution, spanSet, tagMap, HighlightSpanKind.Definition, cancellationToken).ConfigureAwait(false);
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
                                Debug.Assert(solution.Workspace.Kind == WorkspaceKind.Interactive || solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles);
                                continue;
                            }

                            if (documentToSearch.Contains(document))
                            {
                                await AddLocationSpan(location, solution, spanSet, tagMap, HighlightSpanKind.Definition, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }

                foreach (var referenceLocation in reference.Locations)
                {
                    var referenceKind = referenceLocation.IsWrittenTo ? HighlightSpanKind.WrittenReference : HighlightSpanKind.Reference;
                    await AddLocationSpan(referenceLocation.Location, solution, spanSet, tagMap, referenceKind, cancellationToken).ConfigureAwait(false);
                }
            }

            // Add additional references
            foreach (var location in additionalReferences)
            {
                await AddLocationSpan(location, solution, spanSet, tagMap, HighlightSpanKind.Reference, cancellationToken).ConfigureAwait(false);
            }

            using var list = ArrayBuilder<DocumentHighlights>.GetInstance(tagMap.Count);
            foreach (var kvp in tagMap)
            {
                using var spans = ArrayBuilder<HighlightSpan>.GetInstance(kvp.Value.Count);
                foreach (var span in kvp.Value)
                {
                    spans.Add(span);
                }

                list.Add(new DocumentHighlights(kvp.Key, spans.ToImmutable()));
            }

            return list.ToImmutable();
        }

        private static bool ShouldIncludeDefinition(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                    return false;

                case SymbolKind.NamedType:
                    return !((INamedTypeSymbol)symbol).IsScriptClass;

                case SymbolKind.Parameter:

                    // If it's an indexer parameter, we will have also cascaded to the accessor
                    // one that actually receives the references
                    var containingProperty = symbol.ContainingSymbol as IPropertySymbol;
                    if (containingProperty != null && containingProperty.IsIndexer)
                    {
                        return false;
                    }

                    break;
            }

            return true;
        }

        private static async Task AddLocationSpan(Location location, Solution solution, HashSet<DocumentSpan> spanSet, MultiDictionary<Document, HighlightSpan> tagList, HighlightSpanKind kind, CancellationToken cancellationToken)
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

                    var document = solution.GetDocument(tree);
                    var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

                    if (syntaxFacts != null)
                    {
                        // Specify findInsideTrivia: true to ensure that we search within XML doc comments.
                        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                        var token = root.FindToken(location.SourceSpan.Start, findInsideTrivia: true);

                        return syntaxFacts.IsGenericName(token.Parent) || syntaxFacts.IsIndexerMemberCRef(token.Parent)
                            ? new DocumentSpan(document, token.Span)
                            : new DocumentSpan(document, location.SourceSpan);
                    }
                }
            }
            catch (NullReferenceException e) when (FatalError.ReportWithoutCrash(e))
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
}
