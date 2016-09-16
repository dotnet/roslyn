// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.FindReferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindSymbols
{
    [ExportWorkspaceService(typeof(ISymbolFinderEngineService), ServiceLayer.Host), Shared]
    internal class VisualStudioSymbolFinderEngineService : ISymbolFinderEngineService
    {
        public async Task FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, 
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                if (symbolAndProjectId.ProjectId == null)
                {
                    // This is a call through our old public API.  We don't have the necessary
                    // data to effectively run the call out of proc.
                    await DefaultSymbolFinderEngineService.FindReferencesInCurrentProcessAsync(
                        symbolAndProjectId, solution, progress, documents, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await FindReferencesInServiceProcessAsync(
                        symbolAndProjectId, solution, progress, documents, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task FindReferencesInServiceProcessAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution, 
            IStreamingFindReferencesProgress progress, 
            IImmutableSet<Document> documents, 
            CancellationToken cancellationToken)
        {
            documents = documents ?? ImmutableHashSet<Document>.Empty;
            var clientService = solution.Workspace.Services.GetService<IRemoteHostClientService>();
            var client = await clientService.GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);

            if (client == null)
            {
                await DefaultSymbolFinderEngineService.FindReferencesInCurrentProcessAsync(
                    symbolAndProjectId, solution, progress, documents, cancellationToken).ConfigureAwait(false);
                return;
            }

            var serverCallback = new ServerCallback(solution, progress, cancellationToken);

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(
                solution, serverCallback, cancellationToken).ConfigureAwait(false))
            {
                await session.InvokeAsync(
                    WellKnownServiceHubServices.CodeAnalysisService_FindReferencesAsync,
                    SerializableSymbolAndProjectId.Dehydrate(symbolAndProjectId),
                    documents.Select(SerializableDocumentId.Dehydrate).ToArray()).ConfigureAwait(false);
            }
        }

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

            public Task OnStarted() => _progress.OnStartedAsync();
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

            public async Task OnDefinitionFoundAsync(SerializableSymbolAndProjectId argument)
            {
                var symbolAndProjectId = await argument.RehydrateAsync(
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