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
    internal partial class VisualStudioSymbolFinderEngineService : ISymbolFinderEngineService
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
            var clientService = solution.Workspace.Services.GetService<IRemoteHostClientService>();
            var client = await clientService.GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);

            if (client == null)
            {
                await DefaultSymbolFinderEngineService.FindReferencesInCurrentProcessAsync(
                    symbolAndProjectId, solution, progress, documents, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Create a callback that we can pass to the server process to hear about the 
            // results as it finds them.  When we hear about results we'll forward them to
            // the 'progress' parameter which will then upate the UI.
            var serverCallback = new ServerCallback(solution, progress, cancellationToken);

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(
                solution, serverCallback, cancellationToken).ConfigureAwait(false))
            {
                await session.InvokeAsync(
                    WellKnownServiceHubServices.CodeAnalysisService_FindReferencesAsync,
                    SerializableSymbolAndProjectId.Dehydrate(symbolAndProjectId),
                    documents?.Select(SerializableDocumentId.Dehydrate).ToArray()).ConfigureAwait(false);
            }
        }
    }
}