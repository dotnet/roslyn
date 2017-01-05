// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal static async Task FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                var outOfProcessAllowed = solution.Workspace.Options.GetOption(SymbolFinderOptions.OutOfProcessAllowed);
                if (symbolAndProjectId.ProjectId == null || !outOfProcessAllowed)
                {
                    // This is a call through our old public API.  We don't have the necessary
                    // data to effectively run the call out of proc.
                    await FindReferencesInCurrentProcessAsync(
                        symbolAndProjectId, solution, progress, documents, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await FindReferencesInServiceProcessAsync(
                        symbolAndProjectId, solution, progress, documents, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        internal static Task FindReferencesInCurrentProcessAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution,
            IStreamingFindReferencesProgress progress, IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            var finders = ReferenceFinders.DefaultReferenceFinders;
            progress = progress ?? StreamingFindReferencesProgress.Instance;
            var engine = new FindReferencesSearchEngine(
                solution, documents, finders, progress, cancellationToken);
            return engine.FindReferencesAsync(symbolAndProjectId);
        }

        private static async Task FindReferencesInServiceProcessAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            var client = await solution.Workspace.GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                await FindReferencesInCurrentProcessAsync(
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
                    nameof(IRemoteSymbolFinder.FindReferencesAsync),
                    SerializableSymbolAndProjectId.Dehydrate(symbolAndProjectId),
                    documents?.Select(SerializableDocumentId.Dehydrate).ToArray()).ConfigureAwait(false);
            }
        }
    }
}