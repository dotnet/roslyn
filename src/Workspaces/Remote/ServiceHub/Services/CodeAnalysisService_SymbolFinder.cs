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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteSymbolFinder
    {
        public Task FindReferencesAsync(
            SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs,
            SerializableFindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                    var symbolAndProjectId = await symbolAndProjectIdArg.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    var progressCallback = new FindReferencesProgressCallback(EndPoint, cancellationToken);

                    if (!symbolAndProjectId.HasValue)
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
                        symbolAndProjectId.Value, solution, progressCallback,
                        documents, options.Rehydrate(), cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task FindLiteralReferencesAsync(object value, TypeCode typeCode, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var convertedType = System.Convert.ChangeType(value, typeCode);
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                    var progressCallback = new FindLiteralReferencesProgressCallback(EndPoint, cancellationToken);
                    await SymbolFinder.FindLiteralReferencesInCurrentProcessAsync(
                        convertedType, solution, progressCallback, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    using var query = SearchQuery.Create(name, searchKind);

                    var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
                        project, query, criteria, cancellationToken).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                        solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                        project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
            string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                        solution, pattern, criteria, cancellationToken).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                        project, pattern, criteria, cancellationToken).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        private sealed class FindLiteralReferencesProgressCallback : IStreamingFindLiteralReferencesProgress
        {
            private readonly RemoteEndPoint _endPoint;
            private readonly CancellationToken _cancellationToken;

            public FindLiteralReferencesProgressCallback(RemoteEndPoint endPoint, CancellationToken cancellationToken)
            {
                _endPoint = endPoint;
                _cancellationToken = cancellationToken;
            }

            public Task ReportProgressAsync(int current, int maximum)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindLiteralsServerCallback.ReportProgressAsync), new object[] { current, maximum }, _cancellationToken);

            public Task OnReferenceFoundAsync(Document document, TextSpan span)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindLiteralsServerCallback.OnReferenceFoundAsync), new object[] { document.Id, span }, _cancellationToken);
        }

        private sealed class FindReferencesProgressCallback : IStreamingFindReferencesProgress
        {
            private readonly RemoteEndPoint _endPoint;
            private readonly CancellationToken _cancellationToken;

            public FindReferencesProgressCallback(RemoteEndPoint endPoint, CancellationToken cancellationToken)
            {
                _endPoint = endPoint;
                _cancellationToken = cancellationToken;
            }

            public Task OnStartedAsync()
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnStartedAsync), Array.Empty<object>(), _cancellationToken);

            public Task OnCompletedAsync()
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnCompletedAsync), Array.Empty<object>(), _cancellationToken);

            public Task ReportProgressAsync(int current, int maximum)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.ReportProgressAsync), new object[] { current, maximum }, _cancellationToken);

            public Task OnFindInDocumentStartedAsync(Document document)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnFindInDocumentStartedAsync), new object[] { document.Id }, _cancellationToken);

            public Task OnFindInDocumentCompletedAsync(Document document)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnFindInDocumentCompletedAsync), new object[] { document.Id }, _cancellationToken);

            public Task OnDefinitionFoundAsync(SymbolAndProjectId definition)
                => _endPoint.InvokeAsync(nameof(SymbolFinder.FindReferencesServerCallback.OnDefinitionFoundAsync), new object[] { SerializableSymbolAndProjectId.Dehydrate(definition) }, _cancellationToken);

            public Task OnReferenceFoundAsync(SymbolAndProjectId definition, ReferenceLocation reference)
            {
                return _endPoint.InvokeAsync(
                    nameof(SymbolFinder.FindReferencesServerCallback.OnReferenceFoundAsync),
                    new object[] { SerializableSymbolAndProjectId.Dehydrate(definition), SerializableReferenceLocation.Dehydrate(reference) },
                    _cancellationToken);
            }
        }
    }
}
