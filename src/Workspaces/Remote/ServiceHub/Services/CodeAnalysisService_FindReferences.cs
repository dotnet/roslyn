// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteSymbolFinder
    {
        public async Task FindReferencesAsync(
            SerializableSymbolAndProjectId symbolAndProjectIdArg, SerializableDocumentId[] documentArgs, 
            byte[] solutionChecksum)
        {
            var solution = await RoslynServices.SolutionService.GetSolutionAsync(
                new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);

            var symbolAndProjectId = await symbolAndProjectIdArg.RehydrateAsync(
                solution, CancellationToken).ConfigureAwait(false);
            var documents = documentArgs?.Select(a => a.Rehydrate())
                                         .Select(solution.GetDocument)
                                         .ToImmutableHashSet();

            var progressCallback = new ProgressCallback(this);
            await SymbolFinder.FindReferencesInCurrentProcessAsync(
                symbolAndProjectId, solution, progressCallback, documents, CancellationToken).ConfigureAwait(false);
        }

        private class ProgressCallback : IStreamingFindReferencesProgress
        {
            private readonly CodeAnalysisService _service;

            public ProgressCallback(CodeAnalysisService service)
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
                => _service.Rpc.InvokeAsync(nameof(OnFindInDocumentStartedAsync), 
                    SerializableDocumentId.Dehydrate(document));

            public Task OnFindInDocumentCompletedAsync(Document document)
                => _service.Rpc.InvokeAsync(nameof(OnFindInDocumentCompletedAsync), 
                    SerializableDocumentId.Dehydrate(document));

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