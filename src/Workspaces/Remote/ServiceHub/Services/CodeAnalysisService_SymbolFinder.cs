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
        public Task FindReferencesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);

                    var symbolAndProjectId = await symbolAndProjectIdArg.TryRehydrateAsync(
                        solution, token).ConfigureAwait(false);

                    var progressCallback = new FindReferencesProgressCallback(this);

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
                        symbolAndProjectId.Value, solution,
                        progressCallback, documents, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task FindLiteralReferencesAsync(object value, TypeCode typeCode, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var convertedType = System.Convert.ChangeType(value, typeCode);
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);

                    var progressCallback = new FindLiteralReferencesProgressCallback(this);
                    await SymbolFinder.FindLiteralReferencesInCurrentProcessAsync(
                        convertedType, solution, progressCallback, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    using (var query = SearchQuery.Create(name, searchKind))
                    {
                        var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
                            project, query, criteria, token).ConfigureAwait(false);

                        return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                    }
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                        solution, name, ignoreCase, criteria, token).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                        project, name, ignoreCase, criteria, token).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
            string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                        solution, pattern, criteria, token).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var project = solution.GetProject(projectId);

                    var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                        project, pattern, criteria, token).ConfigureAwait(false);

                    return (IList<SerializableSymbolAndProjectId>)result.SelectAsArray(SerializableSymbolAndProjectId.Dehydrate);
                }
            }, cancellationToken);
        }

        private class FindLiteralReferencesProgressCallback : IStreamingFindLiteralReferencesProgress
        {
            private readonly CodeAnalysisService _service;

            public FindLiteralReferencesProgressCallback(CodeAnalysisService service)
            {
                _service = service;
            }

            public Task ReportProgressAsync(int current, int maximum)
                => _service.Rpc.InvokeAsync(nameof(ReportProgressAsync), current, maximum);

            public Task OnReferenceFoundAsync(Document document, TextSpan span)
                => _service.Rpc.InvokeAsync(nameof(OnReferenceFoundAsync), document.Id, span);
        }

        private class FindReferencesProgressCallback : IStreamingFindReferencesProgress
        {
            private readonly CodeAnalysisService _service;

            public FindReferencesProgressCallback(CodeAnalysisService service)
            {
                _service = service;
            }

            public Task OnStartedAsync()
                => _service.Rpc.InvokeAsync(nameof(OnStartedAsync));

            public Task OnCompletedAsync()
                => _service.Rpc.InvokeAsync(nameof(OnCompletedAsync));

            public Task ReportProgressAsync(int current, int maximum)
                => _service.Rpc.InvokeAsync(nameof(ReportProgressAsync), current, maximum);

            public Task OnFindInDocumentStartedAsync(Document document)
                => _service.Rpc.InvokeAsync(nameof(OnFindInDocumentStartedAsync), document.Id);

            public Task OnFindInDocumentCompletedAsync(Document document)
                => _service.Rpc.InvokeAsync(nameof(OnFindInDocumentCompletedAsync), document.Id);

            public Task OnDefinitionFoundAsync(SymbolAndProjectId definition)
                => _service.Rpc.InvokeAsync(nameof(OnDefinitionFoundAsync),
                    SerializableSymbolAndProjectId.Dehydrate(definition));

            public Task OnReferenceFoundAsync(
                SymbolAndProjectId definition, ReferenceLocation reference)
            {
                return _service.Rpc.InvokeAsync(nameof(OnReferenceFoundAsync),
                    SerializableSymbolAndProjectId.Dehydrate(definition),
                    SerializableReferenceLocation.Dehydrate(reference));
            }
        }
    }
}
