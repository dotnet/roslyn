// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // This file contains the current FindReferences APIs.  The current APIs allow for OOP 
    // implementation and will defer to the oop server if it is available.  If not, it will
    // compute the results in process.

    public static partial class SymbolFinder
    {
        internal static async Task FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                // If ProjectId is null then this is a call through our old public API.  We don't have
                // the necessary data to effectively run the call out of proc.
                if (symbolAndProjectId.ProjectId != null)
                {
                    var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                    if (client != null)
                    {
                        // Create a callback that we can pass to the server process to hear about the 
                        // results as it finds them.  When we hear about results we'll forward them to
                        // the 'progress' parameter which will then update the UI.
                        var serverCallback = new FindReferencesServerCallback(solution, progress, cancellationToken);

                        var success = await client.TryRunRemoteAsync(
                            WellKnownServiceHubServices.CodeAnalysisService,
                            nameof(IRemoteSymbolFinder.FindReferencesAsync),
                            new object[]
                            {
                                SerializableSymbolAndProjectId.Dehydrate(symbolAndProjectId),
                                documents?.Select(d => d.Id).ToArray(),
                                SerializableFindReferencesSearchOptions.Dehydrate(options),
                            },
                            solution,
                            serverCallback,
                            cancellationToken).ConfigureAwait(false);

                        if (success)
                        {
                            return;
                        }
                    }
                }

                // Couldn't effectively search in OOP. Perform the search in-proc.
                await FindReferencesInCurrentProcessAsync(
                    symbolAndProjectId, solution, progress,
                    documents, options, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static Task FindReferencesInCurrentProcessAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var finders = ReferenceFinders.DefaultReferenceFinders;
            progress ??= StreamingFindReferencesProgress.Instance;
            var engine = new FindReferencesSearchEngine(
                solution, documents, finders, progress, options, cancellationToken);
            return engine.FindReferencesAsync(symbolAndProjectId);
        }
    }
}
