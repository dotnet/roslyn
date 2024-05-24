// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

// This file contains the current FindReferences APIs.  The current APIs allow for OOP 
// implementation and will defer to the oop server if it is available.  If not, it will
// compute the results in process.

public static partial class SymbolFinder
{
    internal static async Task FindReferencesAsync(
        ISymbol symbol,
        Solution solution,
        IStreamingFindReferencesProgress progress,
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
        {
            // We do not use OOP for FAR for body-level symbols (like locals/local-functions/etc.).  There's no
            // point in sending to OOP as it will do nothing but add overhead.  Specifically, a body level symbol
            // already came from source, and it roots the Compilation it came from (through its
            // ISourceAssemblySymbol). 
            //
            // Since we literally only need to examine the symbol's containing method-like-body to look for other
            // references, it's much better to just stay in process.As we have a local, it's highly likely that the
            // caller of this also got that local symbol from a semantic model that they are also holding.  So, in
            // most cases there's no additional semantic costs at all, and this just becomes a walk of the existing
            // bound nodes with the same name to find hits, which is the fastest we could hope for without a
            // dedicated compiler API.
            if (!SymbolKey.IsBodyLevelSymbol(symbol) && SerializableSymbolAndProjectId.TryCreate(symbol, solution, cancellationToken, out var serializedSymbol))
            {
                var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    // Create a callback that we can pass to the server process to hear about the 
                    // results as it finds them.  When we hear about results we'll forward them to
                    // the 'progress' parameter which will then update the UI.
                    var serverCallback = new FindReferencesServerCallback(solution, progress);
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
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var finders = ReferenceFinders.DefaultReferenceFinders;
        progress ??= NoOpStreamingFindReferencesProgress.Instance;
        var engine = new FindReferencesSearchEngine(
            solution, documents, finders, progress, options);
        return engine.FindReferencesAsync(symbol, cancellationToken);
    }

    internal static Task FindReferencesInDocumentsInCurrentProcessAsync(
        ISymbol symbol,
        Solution solution,
        IStreamingFindReferencesProgress progress,
        IImmutableSet<Document> documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        // For finding in a document, we only support unidirectional cascading.  This allows us to not have to look
        // beyond these documents (and the symbols we find in it) to know if those symbols have an appropriate
        // inheritance relationship with the starting symbol.
        options = options with { UnidirectionalHierarchyCascade = true };

        var finders = ReferenceFinders.DefaultReferenceFinders;
        var engine = new FindReferencesSearchEngine(
            solution, documents, finders, progress, options);
        return engine.FindReferencesInDocumentsAsync(symbol, documents, cancellationToken);
    }
}
