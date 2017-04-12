// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteSymbolFinder
    {
        public async Task FindReferencesAsync(SerializableSymbolAndProjectId symbolAndProjectIdArg, DocumentId[] documentArgs)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);

            var symbolAndProjectId = await symbolAndProjectIdArg.RehydrateAsync(
                solution, CancellationToken).ConfigureAwait(false);
            var documents = documentArgs?.Select(solution.GetDocument)
                                         .ToImmutableHashSet();

            var progressCallback = new FindReferencesProgressCallback(this);
            await SymbolFinder.FindReferencesInCurrentProcessAsync(
                symbolAndProjectId, solution, progressCallback, documents, CancellationToken).ConfigureAwait(false);
        }

        public async Task FindLiteralReferencesAsync(object value)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);

            var progressCallback = new FindLiteralReferencesProgressCallback(this);
            await SymbolFinder.FindLiteralReferencesInCurrentProcessAsync(
                value, solution, progressCallback, CancellationToken).ConfigureAwait(false);
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