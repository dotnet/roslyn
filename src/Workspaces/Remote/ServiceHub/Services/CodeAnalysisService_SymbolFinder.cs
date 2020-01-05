// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                    var progressCallback = new FindReferencesProgressCallback(this, cancellationToken);

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

                    var progressCallback = new FindLiteralReferencesProgressCallback(this, cancellationToken);
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

        private class FindLiteralReferencesProgressCallback : IStreamingFindLiteralReferencesProgress
        {
            private readonly CodeAnalysisService _service;
            private readonly CancellationToken _cancellationToken;

            public FindLiteralReferencesProgressCallback(
                CodeAnalysisService service,
                CancellationToken cancellationToken)
            {
                _service = service;
                _cancellationToken = cancellationToken;
            }

            public Task ReportProgressAsync(int current, int maximum)
                => _service.InvokeAsync(nameof(ReportProgressAsync), new object[] { current, maximum }, _cancellationToken);

            public Task OnReferenceFoundAsync(Document document, TextSpan span)
                => _service.InvokeAsync(nameof(OnReferenceFoundAsync), new object[] { document.Id, span }, _cancellationToken);
        }

        private class FindReferencesProgressCallback : IStreamingFindReferencesProgress
        {
            private readonly CodeAnalysisService _service;
            private readonly CancellationToken _cancellationToken;

            public FindReferencesProgressCallback(
                CodeAnalysisService service, CancellationToken cancellationToken)
            {
                _service = service;
                _cancellationToken = cancellationToken;
            }

            public Task OnStartedAsync()
                => _service.InvokeAsync(nameof(OnStartedAsync), _cancellationToken);

            public Task OnCompletedAsync()
                => _service.InvokeAsync(nameof(OnCompletedAsync), _cancellationToken);

            public Task ReportProgressAsync(int current, int maximum)
                => _service.InvokeAsync(nameof(ReportProgressAsync), new object[] { current, maximum }, _cancellationToken);

            public Task OnFindInDocumentStartedAsync(Document document)
                => _service.InvokeAsync(nameof(OnFindInDocumentStartedAsync), new object[] { document.Id }, _cancellationToken);

            public Task OnFindInDocumentCompletedAsync(Document document)
                => _service.InvokeAsync(nameof(OnFindInDocumentCompletedAsync), new object[] { document.Id }, _cancellationToken);

            public Task OnDefinitionFoundAsync(SymbolAndProjectId definition)
                => _service.InvokeAsync(nameof(OnDefinitionFoundAsync), new object[] { SerializableSymbolAndProjectId.Dehydrate(definition) }, _cancellationToken);

            public Task OnReferenceFoundAsync(
                SymbolAndProjectId definition, ReferenceLocation reference)
            {
                return _service.InvokeAsync(nameof(OnReferenceFoundAsync),
                    new object[] { SerializableSymbolAndProjectId.Dehydrate(definition), SerializableReferenceLocation.Dehydrate(reference) },
                    _cancellationToken);
            }
        }
    }
}
