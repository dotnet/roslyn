// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {

        /// <summary>
        /// Callback object we pass to the OOP server to hear about the result 
        /// of the FindReferencesEngine as it executes there.
        /// </summary>
        private class ServerCallback
        {
            private readonly Solution _solution;
            private readonly IStreamingFindReferencesProgress _progress;
            private readonly CancellationToken _cancellationToken;

            public ServerCallback(
                Solution solution,
                IStreamingFindReferencesProgress progress,
                CancellationToken cancellationToken)
            {
                _solution = solution;
                _progress = progress;
                _cancellationToken = cancellationToken;
            }

            public Task OnStartedAsync() => _progress.OnStartedAsync();
            public Task OnCompletedAsync() => _progress.OnCompletedAsync();
            public Task ReportProgressAsync(int current, int maximum) => _progress.ReportProgressAsync(current, maximum);

            public Task OnFindInDocumentStartedAsync(SerializableDocumentId documentId)
            {
                var document = _solution.GetDocument(documentId.Rehydrate());
                return _progress.OnFindInDocumentStartedAsync(document);
            }

            public Task OnFindInDocumentCompletedAsync(SerializableDocumentId documentId)
            {
                var document = _solution.GetDocument(documentId.Rehydrate());
                return _progress.OnFindInDocumentCompletedAsync(document);
            }

            public async Task OnDefinitionFoundAsync(SerializableSymbolAndProjectId definition)
            {
                var symbolAndProjectId = await definition.RehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);
                await _progress.OnDefinitionFoundAsync(symbolAndProjectId).ConfigureAwait(false);
            }

            public async Task OnReferenceFoundAsync(
                SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference)
            {
                var symbolAndProjectId = await definition.RehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);
                var referenceLocation = await reference.RehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);

                await _progress.OnReferenceFoundAsync(symbolAndProjectId, referenceLocation).ConfigureAwait(false);
            }
        }
    }
}