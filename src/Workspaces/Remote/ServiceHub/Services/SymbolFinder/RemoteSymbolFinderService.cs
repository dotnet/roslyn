// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal sealed class RemoteSymbolFinderService : BrokeredServiceBase, IRemoteSymbolFinderService
    {
        internal sealed class Factory : FactoryBase<IRemoteSymbolFinderService, IRemoteSymbolFinderService.ICallback>
        {
            protected override IRemoteSymbolFinderService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteSymbolFinderService.ICallback> callback)
                => new RemoteSymbolFinderService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteSymbolFinderService.ICallback> _callback;

        public RemoteSymbolFinderService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteSymbolFinderService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        public ValueTask FindReferencesAsync(
            PinnedSolutionInfo solutionInfo,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectIdArg,
            ImmutableArray<DocumentId> documentArgs,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var symbol = await symbolAndProjectIdArg.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    var progressCallback = new FindReferencesProgressCallback(solution, _callback, callbackId, cancellationToken);

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
                    var documents = documentArgs.IsDefault ? null :
                        documentArgs.Select(solution.GetDocument).WhereNotNull().ToImmutableHashSet();

                    await SymbolFinder.FindReferencesInCurrentProcessAsync(
                        symbol, solution, progressCallback,
                        documents, options, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask FindLiteralReferencesAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, object value, TypeCode typeCode, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (UserOperationBooster.Boost())
                {
                    var convertedType = System.Convert.ChangeType(value, typeCode);
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var progressCallback = new FindLiteralReferencesProgressCallback(_callback, callbackId, cancellationToken);
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

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo,
            ProjectId projectId,
            string name,
            SearchKind searchKind,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
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

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo,
            string name,
            bool ignoreCase,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
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

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo,
            ProjectId projectId,
            string name,
            bool ignoreCase,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
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

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
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

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
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
            private readonly RemoteCallback<IRemoteSymbolFinderService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;
            private readonly CancellationToken _cancellationToken;

            public IStreamingProgressTracker ProgressTracker { get; }

            public FindLiteralReferencesProgressCallback(RemoteCallback<IRemoteSymbolFinderService.ICallback> callback, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
            {
                _callback = callback;
                _callbackId = callbackId;
                _cancellationToken = cancellationToken;
                ProgressTracker = this;
            }

            public ValueTask OnReferenceFoundAsync(Document document, TextSpan span)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnLiteralReferenceFoundAsync(_callbackId, document.Id, span), _cancellationToken);

            public ValueTask AddItemsAsync(int count)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.AddLiteralItemsAsync(_callbackId, count), _cancellationToken);

            public ValueTask ItemCompletedAsync()
                => _callback.InvokeAsync((callback, cancellationToken) => callback.LiteralItemCompletedAsync(_callbackId), _cancellationToken);
        }

        private sealed class FindReferencesProgressCallback : IStreamingFindReferencesProgress, IStreamingProgressTracker
        {
            private readonly Solution _solution;
            private readonly RemoteCallback<IRemoteSymbolFinderService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;
            private readonly CancellationToken _cancellationToken;

            public IStreamingProgressTracker ProgressTracker { get; }

            public FindReferencesProgressCallback(Solution solution, RemoteCallback<IRemoteSymbolFinderService.ICallback> callback, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
            {
                _solution = solution;
                _callback = callback;
                _callbackId = callbackId;
                _cancellationToken = cancellationToken;
                ProgressTracker = this;
            }

            public ValueTask OnStartedAsync()
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnStartedAsync(_callbackId), _cancellationToken);

            public ValueTask OnCompletedAsync()
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnCompletedAsync(_callbackId), _cancellationToken);

            public ValueTask OnFindInDocumentStartedAsync(Document document)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnFindInDocumentStartedAsync(_callbackId, document.Id), _cancellationToken);

            public ValueTask OnFindInDocumentCompletedAsync(Document document)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnFindInDocumentCompletedAsync(_callbackId, document.Id), _cancellationToken);

            public ValueTask OnDefinitionFoundAsync(ISymbol definition)
            {
                var dehydratedDefinition = SerializableSymbolAndProjectId.Dehydrate(_solution, definition, _cancellationToken);
                return _callback.InvokeAsync((callback, cancellationToken) => callback.OnDefinitionFoundAsync(_callbackId, dehydratedDefinition), _cancellationToken);
            }

            public ValueTask OnReferenceFoundAsync(ISymbol definition, ReferenceLocation reference)
            {
                var dehydratedDefinition = SerializableSymbolAndProjectId.Dehydrate(_solution, definition, _cancellationToken);
                var dehydratedReference = SerializableReferenceLocation.Dehydrate(reference, _cancellationToken);

                return _callback.InvokeAsync((callback, cancellationToken) => callback.OnReferenceFoundAsync(_callbackId, dehydratedDefinition, dehydratedReference), _cancellationToken);
            }

            public ValueTask AddItemsAsync(int count)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.AddReferenceItemsAsync(_callbackId, count), _cancellationToken);

            public ValueTask ItemCompletedAsync()
                => _callback.InvokeAsync((callback, cancellationToken) => callback.ReferenceItemCompletedAsync(_callbackId), _cancellationToken);
        }
    }
}
