// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteSymbolFinder
    {
        public Task FindReferencesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectIdArg,
            DocumentId[] documentArgs,
            SerializableFindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var symbol = await symbolAndProjectIdArg.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    var progressCallback = new FindReferencesProgressCallback(solution, EndPoint, cancellationToken);

                    if (symbol == null)
                    {
                        await progressCallback.OnStartedAsync().ConfigureAwait(false);
                        await progressCallback.OnCompletedAsync().ConfigureAwait(false);
                        return;
                    }

                    // NOTE: In projection scenarios, we might get a set of documents to search
                    // that are not all the same language and might not exist in the OOP process
                    // (like the JS parts of a .cshtml file). Filter them out here.  This will
                    // need to be revisited if we someday support FAR between these languages.
                    var documents = documentArgs?.Select(solution.GetDocument)
                                                 .WhereNotNull()
                                                 .ToImmutableHashSet();

                    await SymbolFinder.FindReferencesInCurrentProcessAsync(
                        symbol, solution, progressCallback,
                        documents, options.Rehydrate(), cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task FindLiteralReferencesAsync(PinnedSolutionInfo solutionInfo, object value, TypeCode typeCode, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var convertedType = System.Convert.ChangeType(value, typeCode);
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var progressCallback = new FindLiteralReferencesProgressCallback(EndPoint, cancellationToken);
                    await SymbolFinder.FindLiteralReferencesInCurrentProcessAsync(
                        convertedType, solution, progressCallback, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private static ImmutableArray<SerializableSymbolAndProjectId> Convert(ImmutableArray<ISymbol> items, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SerializableSymbolAndProjectId>.GetInstance(out var result);

            foreach (var item in items)
                result.Add(SerializableSymbolAndProjectId.Dehydrate(solution, item, cancellationToken));

            return result.ToImmutable();
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo,
            ProjectId projectId,
            string name,
            SearchKind searchKind,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    using var query = SearchQuery.Create(name, searchKind);

                    var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
                        project, query, criteria, cancellationToken).ConfigureAwait(false);

                    return Convert(result, solution, cancellationToken);
                }
            }, cancellationToken);
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo,
            string name,
            bool ignoreCase,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                        solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

                    return Convert(result, solution, cancellationToken);
                }
            }, cancellationToken);
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo,
            ProjectId projectId,
            string name,
            bool ignoreCase,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                        project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

                    return Convert(result, solution, cancellationToken);
                }
            }, cancellationToken);
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                        solution, pattern, criteria, cancellationToken).ConfigureAwait(false);

                    return Convert(result, solution, cancellationToken);
                }
            }, cancellationToken);
        }

        public Task<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                        project, pattern, criteria, cancellationToken).ConfigureAwait(false);

                    return Convert(result, solution, cancellationToken);
                }
            }, cancellationToken);
        }

        private sealed class FindLiteralReferencesProgressCallback : IStreamingFindLiteralReferencesProgress, IStreamingProgressTracker
        {
            private readonly RemoteEndPoint _endPoint;
            private readonly CancellationToken _cancellationToken;

            public IStreamingProgressTracker ProgressTracker { get; }

            public FindLiteralReferencesProgressCallback(RemoteEndPoint endPoint, CancellationToken cancellationToken)
            {
                _endPoint = endPoint;
                _cancellationToken = cancellationToken;
                ProgressTracker = this;
            }

            public Task OnReferenceFoundAsync(Document document, TextSpan span)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindLiteralsServerCallback.OnReferenceFoundAsync), new object[] { document.Id, span }, _cancellationToken);

            public Task AddItemsAsync(int count)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindLiteralsServerCallback.AddItemsAsync), new object[] { count }, _cancellationToken);

            public Task ItemCompletedAsync()
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindLiteralsServerCallback.ItemCompletedAsync), new object[] { }, _cancellationToken);
        }

        private sealed class FindReferencesProgressCallback : IStreamingFindReferencesProgress, IStreamingProgressTracker
        {
            private readonly Solution _solution;
            private readonly RemoteEndPoint _endPoint;
            private readonly CancellationToken _cancellationToken;

            public IStreamingProgressTracker ProgressTracker { get; }

            public FindReferencesProgressCallback(Solution solution, RemoteEndPoint endPoint, CancellationToken cancellationToken)
            {
                _solution = solution;
                _endPoint = endPoint;
                _cancellationToken = cancellationToken;
                ProgressTracker = this;
            }

            public Task OnStartedAsync()
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnStartedAsync), Array.Empty<object>(), _cancellationToken);

            public Task OnCompletedAsync()
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnCompletedAsync), Array.Empty<object>(), _cancellationToken);

            public Task OnFindInDocumentStartedAsync(Document document)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnFindInDocumentStartedAsync), new object[] { document.Id }, _cancellationToken);

            public Task OnFindInDocumentCompletedAsync(Document document)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnFindInDocumentCompletedAsync), new object[] { document.Id }, _cancellationToken);

            public Task OnDefinitionFoundAsync(ISymbol definition)
                => _endPoint.InvokeAsync(
                    nameof(SymbolFinder.FindReferencesServerCallback.OnDefinitionFoundAsync),
                    new object[] { SerializableSymbolAndProjectId.Dehydrate(_solution, definition, _cancellationToken) }, _cancellationToken);

            public Task OnReferenceFoundAsync(ISymbol definition, ReferenceLocation reference)
                => _endPoint.InvokeAsync(
                    nameof(SymbolFinder.FindReferencesServerCallback.OnReferenceFoundAsync),
                    new object[]
                    {
                        SerializableSymbolAndProjectId.Dehydrate(_solution, definition, _cancellationToken),
                        SerializableReferenceLocation.Dehydrate(reference, _cancellationToken),
                    },
                    _cancellationToken);

            public Task AddItemsAsync(int count)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.AddItemsAsync), new object[] { count }, _cancellationToken);

            public Task ItemCompletedAsync()
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.ItemCompletedAsync), Array.Empty<object>(), _cancellationToken);
        }
    }
}
