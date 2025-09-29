// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal abstract partial class AbstractNavigateToSearchService
{
    public async Task SearchDocumentAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument: document, onResultsFound);

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted: null, cancellationToken);
            // Don't need to sync the full solution when searching a single document.  Just sync the project that doc is in.
            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                document.Project,
                (service, solutionInfo, callbackId, cancellationToken) =>
                service.SearchDocumentAndRelatedDocumentsAsync(solutionInfo, document.Id, searchPattern, [.. kinds], callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        await SearchDocumentAndRelatedDocumentsInCurrentProcessAsync(document, searchPattern, kinds, onItemsFound, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SearchDocumentAndRelatedDocumentsInCurrentProcessAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<RoslynNavigateToItem>, VoidResult, CancellationToken, Task> onItemsFound,
        CancellationToken cancellationToken)
    {
        var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(searchPattern);
        var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

        // In parallel, search both the document requested, and any relevant 'related documents' we find for it. For the
        // original document, search the entirety of it (by passing 'null' in for the 'spans' argument).  For related
        // documents, only search the spans of the partial-types/inheriting-types that we find for the types in this
        // starting document.
        await Task.WhenAll(
            SearchDocumentsInCurrentProcessAsync([(document, spans: null)]),
            SearchRelatedDocumentsInCurrentProcessAsync()).ConfigureAwait(false);

        Task SearchDocumentsInCurrentProcessAsync(ImmutableArray<(Document document, NormalizedTextSpanCollection? spans)> documentAndSpans)
            => ProducerConsumer<RoslynNavigateToItem>.RunParallelAsync(
                documentAndSpans,
                produceItems: static async (documentAndSpan, onItemFound, args, cancellationToken) =>
                {
                    var (patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onItemsFound) = args;
                    await SearchSingleDocumentAsync(
                        documentAndSpan.document, patternName, patternContainerOpt, declaredSymbolInfoKindsSet,
                        item =>
                        {
                            // Ensure that the results found while searching the single document intersect the desired
                            // subrange of the document we're searching in.  For the primary document this will always
                            // succeed (since we're searching the full document).  But for related documents this may fail
                            // if the results is not in the span of any of the types in those files we're searching.
                            if (documentAndSpan.spans is null || documentAndSpan.spans.IntersectsWith(item.DeclaredSymbolInfo.Span))
                                onItemFound(item);
                        },
                        cancellationToken).ConfigureAwait(false);
                },
                consumeItems: static (values, args, cancellationToken) => args.onItemsFound(values, default, cancellationToken),
                args: (patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onItemsFound),
                cancellationToken);

        async Task SearchRelatedDocumentsInCurrentProcessAsync()
        {
            var relatedDocuments = await GetRelatedDocumentsAsync().ConfigureAwait(false);
            await SearchDocumentsInCurrentProcessAsync(relatedDocuments).ConfigureAwait(false);
        }

        async Task<ImmutableArray<(Document document, NormalizedTextSpanCollection? spans)>> GetRelatedDocumentsAsync()
        {
            // For C#/VB we define 'related documents' as those containing types in the inheritance chain of types in
            // the originating file (as well as all partial parts of the original and inheritance types).  This way a
            // user can search for symbols scoped to the 'current document' and still get results for the members found
            // in partial parts.

            var solution = document.Project.Solution;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var topLevelNodes);
            syntaxFacts.AddTopLevelMembers(root, topLevelNodes);

            // Keep track of all of the interesting spans in each document we find. Note: we will convert this to a
            // NormalizedTextSpanCollection before returning it.  That way the span of an outer partial type will
            // encompass the span of an inner one and we won't get duplicates for the same symbol.
            var documentToTextSpans = new MultiDictionary<Document, TextSpan>();

            foreach (var topLevelMember in topLevelNodes)
            {
                if (semanticModel.GetDeclaredSymbol(topLevelMember, cancellationToken) is not INamedTypeSymbol namedTypeSymbol)
                    continue;

                foreach (var type in namedTypeSymbol.GetBaseTypesAndThis())
                {
                    foreach (var reference in type.DeclaringSyntaxReferences)
                    {
                        var relatedDocument = solution.GetDocument(reference.SyntaxTree);
                        if (relatedDocument is null)
                            continue;

                        documentToTextSpans.Add(relatedDocument, reference.Span);
                    }
                }
            }

            // Ensure we don't search the original document we were already searching.
            documentToTextSpans.Remove(document);
            return documentToTextSpans.SelectAsArray(kvp => (kvp.Key, new NormalizedTextSpanCollection(kvp.Value)))!;
        }
    }

    public async Task SearchProjectsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        Contract.ThrowIfTrue(projects.IsEmpty);
        Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

        Debug.Assert(priorityDocuments.All(d => projects.Contains(d.Project)));
        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument, onResultsFound);

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted, cancellationToken);

            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                // Intentionally sync the full solution.   When SearchProjectAsync is called, we're searching all
                // projects (just in parallel).  So best for them all to sync and share a single solution snapshot
                // on the oop side.
                solution,
                (service, solutionInfo, callbackId, cancellationToken) =>
                    service.SearchProjectsAsync(solutionInfo, projects.SelectAsArray(p => p.Id), priorityDocumentIds, searchPattern, [.. kinds], callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        await SearchProjectsInCurrentProcessAsync(
            projects, priorityDocuments, searchPattern, kinds, onItemsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SearchProjectsInCurrentProcessAsync(
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<RoslynNavigateToItem>, VoidResult, CancellationToken, Task> onItemsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        // We're doing a real search over the fully loaded solution now.  No need to hold onto the cached map
        // of potentially stale indices.
        ClearCachedData();

        var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(searchPattern);
        var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

        using var _ = GetPooledHashSet(priorityDocuments.Select(d => d.Project), out var highPriProjects);

        // Process each project on its own.  That way we can tell the client when we are done searching it.  Put the
        // projects with priority documents ahead of those without so we can get results for those faster.
        await ProducerConsumer<RoslynNavigateToItem>.RunParallelAsync(
            Prioritize(projects, highPriProjects.Contains),
            SearchSingleProjectAsync, onItemsFound, args: default, cancellationToken).ConfigureAwait(false);
        return;

        async Task SearchSingleProjectAsync(
            Project project,
            Action<RoslynNavigateToItem> onItemFound,
            VoidResult _,
            CancellationToken cancellationToken)
        {
            using var _1 = GetPooledHashSet(priorityDocuments.Where(d => project == d.Project), out var highPriDocs);

            await Parallel.ForEachAsync(
                Prioritize(project.Documents, highPriDocs.Contains),
                cancellationToken,
                (document, cancellationToken) => SearchSingleDocumentAsync(
                    document, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onItemFound, cancellationToken)).ConfigureAwait(false);

            await onProjectCompleted().ConfigureAwait(false);
        }
    }
}
