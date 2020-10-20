﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // This file contains the current FindReferences APIs.  The current APIs allow for OOP 
    // implementation and will defer to the oop server if it is available.  If not, it will
    // compute the results in process.

    public static partial class SymbolFinder
    {
        internal static async Task FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                if (SerializableSymbolAndProjectId.TryCreate(symbol, solution, cancellationToken, out var serializedSymbol))
                {
                    var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                    if (client != null)
                    {
                        // Create a callback that we can pass to the server process to hear about the 
                        // results as it finds them.  When we hear about results we'll forward them to
                        // the 'progress' parameter which will then update the UI.
                        var serverCallback = new FindReferencesServerCallback(solution, progress, cancellationToken);
                        var documentIds = documents?.SelectAsArray(d => d.Id) ?? default;

                        await client.TryInvokeAsync<IRemoteSymbolFinderService>(
                            solution,
                            (service, solutionInfo, callbackId, cancellationToken) => service.FindReferencesAsync(solutionInfo, callbackId, serializedSymbol, documentIds, options, cancellationToken),
                            serverCallback,
                            cancellationToken).ConfigureAwait(false);

                        return;
                    }
                }

                // Couldn't effectively search in OOP. Perform the search in-proc.
                await FindReferencesInCurrentProcessAsync(
                    symbol, solution, progress,
                    documents, options, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static Task FindReferencesInCurrentProcessAsync(
            ISymbol symbol,
            Solution solution,
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var finders = ReferenceFinders.DefaultReferenceFinders;
            progress ??= NoOpStreamingFindReferencesProgress.Instance;
            var engine = new FindReferencesSearchEngine(
                solution, documents, finders, progress, options, cancellationToken);
            return engine.FindReferencesAsync(symbol);
        }
    }
}
