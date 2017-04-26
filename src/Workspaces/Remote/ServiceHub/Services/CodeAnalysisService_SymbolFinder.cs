// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteSymbolFinder
    {
        public async Task FindReferencesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

            var symbolAndProjectId = await symbolAndProjectIdArg.TryRehydrateAsync(
                solution, cancellationToken).ConfigureAwait(false);

            var progressCallback = new FindReferencesProgressCallback(this);

            if (!symbolAndProjectId.HasValue)
            {
                await progressCallback.OnStartedAsync(cancellationToken).ConfigureAwait(false);
                await progressCallback.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var documents = documentArgs?.Select(solution.GetDocument)
                                         .ToImmutableHashSet();

            await SymbolFinder.FindReferencesInCurrentProcessAsync(
                symbolAndProjectId.Value, solution, 
                progressCallback, documents, cancellationToken).ConfigureAwait(false);
        }

        public async Task FindLiteralReferencesAsync(object value, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

            var progressCallback = new FindLiteralReferencesProgressCallback(this);
            await SymbolFinder.FindLiteralReferencesInCurrentProcessAsync(
                value, solution, progressCallback, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SerializableSymbolAndProjectId[]> FindAllDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
            var project = solution.GetProject(projectId);

            using (var query = SearchQuery.Create(name, searchKind))
            {
                var result = await DeclarationFinder.FindAllDeclarationsWithNormalQueryInCurrentProcessAsync(
                    project, query, criteria, cancellationToken).ConfigureAwait(false);

                return result.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
            }
        }

        public async Task<SerializableSymbolAndProjectId[]> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
            var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

            return result.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
        }

        public async Task<SerializableSymbolAndProjectId[]> FindProjectSourceDeclarationsWithNormalQueryAsync(
            ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
            var project = solution.GetProject(projectId);

            var result = await DeclarationFinder.FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

            return result.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
        }

        public async Task<SerializableSymbolAndProjectId[]> FindProjectSourceDeclarationsWithPatternAsync(
            ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
            var project = solution.GetProject(projectId);

            var result = await DeclarationFinder.FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                project, pattern, criteria, cancellationToken).ConfigureAwait(false);

            return result.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
        }

        private class FindLiteralReferencesProgressCallback : IStreamingFindLiteralReferencesProgress
        {
            private readonly CodeAnalysisService _service;

            public FindLiteralReferencesProgressCallback(CodeAnalysisService service)
            {
                _service = service;
            }

            public Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(ReportProgressAsync), new object[] { current, maximum }, cancellationToken);

            public Task OnReferenceFoundAsync(Document document, TextSpan span, CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(OnReferenceFoundAsync), new object[] { document.Id, span }, cancellationToken);
        }

        private class FindReferencesProgressCallback : IStreamingFindReferencesProgress
        {
            private readonly CodeAnalysisService _service;

            public FindReferencesProgressCallback(CodeAnalysisService service)
            {
                _service = service;
            }

            public Task OnStartedAsync(CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(OnStartedAsync), Array.Empty<object>(), cancellationToken);

            public Task OnCompletedAsync(CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(OnCompletedAsync), Array.Empty<object>(), cancellationToken);

            public Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(ReportProgressAsync), new object[] { current, maximum }, cancellationToken);

            public Task OnFindInDocumentStartedAsync(Document document, CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(OnFindInDocumentStartedAsync), new object[] { document.Id }, cancellationToken);

            public Task OnFindInDocumentCompletedAsync(Document document, CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(OnFindInDocumentCompletedAsync), new object[] { document.Id }, cancellationToken);

            public Task OnDefinitionFoundAsync(SymbolAndProjectId definition, CancellationToken cancellationToken)
                => _service.Rpc.InvokeWithCancellationAsync(nameof(OnDefinitionFoundAsync),
                    new[] { SerializableSymbolAndProjectId.Dehydrate(definition) },
                    cancellationToken);

            public Task OnReferenceFoundAsync(
                SymbolAndProjectId definition, ReferenceLocation reference, CancellationToken cancellationToken)
            {
                return _service.Rpc.InvokeWithCancellationAsync(nameof(OnReferenceFoundAsync),
                    new object[] { SerializableSymbolAndProjectId.Dehydrate(definition), SerializableReferenceLocation.Dehydrate(reference) },
                    cancellationToken);
            }
        }
    }
}