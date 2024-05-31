// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectIdArg,
            ImmutableArray<DocumentId> documentArgs,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var symbol = await symbolAndProjectIdArg.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);

                var progressCallback = new FindReferencesProgressCallback(solution, _callback, callbackId);

                if (symbol == null)
                {
                    await progressCallback.OnStartedAsync(cancellationToken).ConfigureAwait(false);
                    await progressCallback.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
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
            }, cancellationToken);
        }

        public ValueTask FindLiteralReferencesAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, object value, TypeCode typeCode, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var convertedType = System.Convert.ChangeType(value, typeCode);

                var progressCallback = new FindLiteralReferencesProgressCallback(_callback, callbackId);
                await SymbolFinder.FindLiteralReferencesInCurrentProcessAsync(
                    convertedType, solution, progressCallback, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private static ImmutableArray<SerializableSymbolAndProjectId> Convert(ImmutableArray<ISymbol> items, Solution solution, CancellationToken cancellationToken)
        {
            var result = new FixedSizeArrayBuilder<SerializableSymbolAndProjectId>(items.Length);

            foreach (var item in items)
                result.Add(SerializableSymbolAndProjectId.Dehydrate(solution, item, cancellationToken));

            return result.MoveToImmutable();
        }

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            string name,
            SearchKind searchKind,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var project = solution.GetRequiredProject(projectId);

                using var query = SearchQuery.Create(name, searchKind);

                var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
                    project, query, criteria, cancellationToken).ConfigureAwait(false);

                return Convert(result, solution, cancellationToken);
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            Checksum solutionChecksum,
            string name,
            bool ignoreCase,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                    solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

                return Convert(result, solution, cancellationToken);
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            string name,
            bool ignoreCase,
            SymbolFilter criteria,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var project = solution.GetRequiredProject(projectId);

                var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                    project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

                return Convert(result, solution, cancellationToken);
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
            Checksum solutionChecksum, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                    solution, pattern, criteria, cancellationToken).ConfigureAwait(false);

                return Convert(result, solution, cancellationToken);
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            Checksum solutionChecksum, ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var project = solution.GetRequiredProject(projectId);

                var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                    project, pattern, criteria, cancellationToken).ConfigureAwait(false);

                return Convert(result, solution, cancellationToken);
            }, cancellationToken);
        }

        private sealed class FindLiteralReferencesProgressCallback : IStreamingFindLiteralReferencesProgress, IStreamingProgressTracker
        {
            private readonly RemoteCallback<IRemoteSymbolFinderService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;

            public IStreamingProgressTracker ProgressTracker { get; }

            public FindLiteralReferencesProgressCallback(RemoteCallback<IRemoteSymbolFinderService.ICallback> callback, RemoteServiceCallbackId callbackId)
            {
                _callback = callback;
                _callbackId = callbackId;
                ProgressTracker = this;
            }

            public ValueTask OnReferenceFoundAsync(Document document, TextSpan span, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnLiteralReferenceFoundAsync(_callbackId, document.Id, span, cancellationToken), cancellationToken);

            public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.AddLiteralItemsAsync(_callbackId, count, cancellationToken), cancellationToken);

            public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.LiteralItemsCompletedAsync(_callbackId, count, cancellationToken), cancellationToken);
        }

        private sealed class FindReferencesProgressCallback : IStreamingFindReferencesProgress, IStreamingProgressTracker
        {
            private readonly Solution _solution;
            private readonly RemoteCallback<IRemoteSymbolFinderService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;

            public IStreamingProgressTracker ProgressTracker { get; }

            public FindReferencesProgressCallback(Solution solution, RemoteCallback<IRemoteSymbolFinderService.ICallback> callback, RemoteServiceCallbackId callbackId)
            {
                _solution = solution;
                _callback = callback;
                _callbackId = callbackId;
                ProgressTracker = this;
            }

            public ValueTask OnStartedAsync(CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnStartedAsync(_callbackId, cancellationToken), cancellationToken);

            public ValueTask OnCompletedAsync(CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.OnCompletedAsync(_callbackId, cancellationToken), cancellationToken);

            public ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken)
            {
                var dehydratedGroup = SerializableSymbolGroup.Dehydrate(_solution, group, cancellationToken);
                return _callback.InvokeAsync(
                    (callback, cancellationToken) => callback.OnDefinitionFoundAsync(_callbackId, dehydratedGroup, cancellationToken), cancellationToken);
            }

            public async ValueTask OnReferencesFoundAsync(
                ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)> references,
                CancellationToken cancellationToken)
            {
                var dehydrated = references.SelectAsArray(
                    static (reference, tuple) => (
                        SerializableSymbolGroup.Dehydrate(tuple.solution, reference.group, tuple.cancellationToken),
                        SerializableSymbolAndProjectId.Dehydrate(tuple.solution, reference.symbol, tuple.cancellationToken),
                        SerializableReferenceLocation.Dehydrate(reference.location, tuple.cancellationToken)),
                    (solution: _solution, cancellationToken));

                await _callback.InvokeAsync(
                    (callback, cancellationToken) => callback.OnReferencesFoundAsync(
                        _callbackId, dehydrated, cancellationToken), cancellationToken).ConfigureAwait(false);
            }

            public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.AddReferenceItemsAsync(_callbackId, count, cancellationToken), cancellationToken);

            public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.ReferenceItemsCompletedAsync(_callbackId, count, cancellationToken), cancellationToken);
        }
    }
}
