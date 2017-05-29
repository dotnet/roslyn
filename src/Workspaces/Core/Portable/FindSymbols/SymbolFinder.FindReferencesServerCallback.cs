// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        private class FindReferencesServerCallback
        {
            private readonly Solution _solution;
            private readonly IStreamingFindReferencesProgress _progress;

            private readonly object _gate = new object();
            private readonly Dictionary<SerializableSymbolAndProjectId, SymbolAndProjectId> _definitionMap =
                new Dictionary<SerializableSymbolAndProjectId, SymbolAndProjectId>();

            public FindReferencesServerCallback(
                Solution solution,
                IStreamingFindReferencesProgress progress)
            {
                _solution = solution;
                _progress = progress;
            }

            public Task OnStartedAsync(CancellationToken cancellationToken) => _progress.OnStartedAsync(cancellationToken);
            public Task OnCompletedAsync(CancellationToken cancellationToken) => _progress.OnCompletedAsync(cancellationToken);
            public Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken) => _progress.ReportProgressAsync(current, maximum, cancellationToken);

            public Task OnFindInDocumentStartedAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                var document = _solution.GetDocument(documentId);
                return _progress.OnFindInDocumentStartedAsync(document, cancellationToken);
            }

            public Task OnFindInDocumentCompletedAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                var document = _solution.GetDocument(documentId);
                return _progress.OnFindInDocumentCompletedAsync(document, cancellationToken);
            }

            public async Task OnDefinitionFoundAsync(SerializableSymbolAndProjectId definition, CancellationToken cancellationToken)
            {
                var symbolAndProjectId = await definition.TryRehydrateAsync(
                    _solution, cancellationToken).ConfigureAwait(false);

                if (!symbolAndProjectId.HasValue)
                {
                    return;
                }

                lock (_gate)
                {
                    _definitionMap[definition] = symbolAndProjectId.Value;
                }

                await _progress.OnDefinitionFoundAsync(symbolAndProjectId.Value, cancellationToken).ConfigureAwait(false);
            }

            public async Task OnReferenceFoundAsync(
                SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference, CancellationToken cancellationToken)
            {
                SymbolAndProjectId symbolAndProjectId;
                lock (_gate)
                {
                    symbolAndProjectId = _definitionMap[definition];
                }

                var referenceLocation = await reference.RehydrateAsync(
                    _solution, cancellationToken).ConfigureAwait(false);

                await _progress.OnReferenceFoundAsync(symbolAndProjectId, referenceLocation, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}