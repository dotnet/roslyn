// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal sealed class FindReferencesServerCallback
        {
            private readonly Solution _solution;
            private readonly IStreamingFindReferencesProgress _progress;
            private readonly CancellationToken _cancellationToken;

            private readonly object _gate = new object();
            private readonly Dictionary<SerializableSymbolAndProjectId, SymbolAndProjectId> _definitionMap =
                new Dictionary<SerializableSymbolAndProjectId, SymbolAndProjectId>();

            public FindReferencesServerCallback(
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

            public Task OnFindInDocumentStartedAsync(DocumentId documentId)
            {
                var document = _solution.GetDocument(documentId);
                return _progress.OnFindInDocumentStartedAsync(document);
            }

            public Task OnFindInDocumentCompletedAsync(DocumentId documentId)
            {
                var document = _solution.GetDocument(documentId);
                return _progress.OnFindInDocumentCompletedAsync(document);
            }

            public async Task OnDefinitionFoundAsync(SerializableSymbolAndProjectId definition)
            {
                var symbolAndProjectId = await definition.TryRehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);

                if (!symbolAndProjectId.HasValue)
                {
                    return;
                }

                lock (_gate)
                {
                    _definitionMap[definition] = symbolAndProjectId.Value;
                }

                await _progress.OnDefinitionFoundAsync(symbolAndProjectId.Value).ConfigureAwait(false);
            }

            public async Task OnReferenceFoundAsync(
                SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference)
            {
                SymbolAndProjectId symbolAndProjectId;
                lock (_gate)
                {
                    symbolAndProjectId = _definitionMap[definition];
                }

                var referenceLocation = await reference.RehydrateAsync(
                    _solution, _cancellationToken).ConfigureAwait(false);

                await _progress.OnReferenceFoundAsync(symbolAndProjectId, referenceLocation).ConfigureAwait(false);
            }
        }
    }
}
