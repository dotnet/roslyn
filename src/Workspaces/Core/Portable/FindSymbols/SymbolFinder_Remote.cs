﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        internal static async Task FindLiteralReferencesAsync(
           object value,
           Solution solution,
           IStreamingFindLiteralReferencesProgress progress,
           CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                var outOfProcessAllowed = solution.Workspace.Options.GetOption(SymbolFinderOptions.OutOfProcessAllowed);
                if (!outOfProcessAllowed)
                {
                    // This is a call through our old public API.  We don't have the necessary
                    // data to effectively run the call out of proc.
                    await FindLiteralReferencesInCurrentProcessAsync(
                        value, solution, progress, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await FindLiteralReferencesInServiceProcessOrFallBackToLocalProcessAsync(
                        value, solution, progress, cancellationToken).ConfigureAwait(false);
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
            var client = await solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                await FindReferencesInCurrentProcessAsync(
                    symbolAndProjectId, solution, progress, documents, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Create a callback that we can pass to the server process to hear about the 
            // results as it finds them.  When we hear about results we'll forward them to
            // the 'progress' parameter which will then update the UI.
            var serverCallback = new FindReferencesServerCallback(solution, progress, cancellationToken);

            await client.RunCodeAnalysisServiceOnRemoteHostAsync(
                solution, serverCallback,
                nameof(IRemoteSymbolFinder.FindReferencesAsync),
                new object[] { SerializableSymbolAndProjectId.Dehydrate(symbolAndProjectId), documents?.Select(d => d.Id).ToArray() },
                cancellationToken).ConfigureAwait(false);
        }

        internal static Task FindLiteralReferencesInCurrentProcessAsync(
            object value, Solution solution,
            IStreamingFindLiteralReferencesProgress progress,
            CancellationToken cancellationToken)
        {
            var engine = new FindLiteralsSearchEngine(
                solution, progress, value, cancellationToken);
            return engine.FindReferencesAsync();
        }

        private static async Task FindLiteralReferencesInServiceProcessOrFallBackToLocalProcessAsync(
            object value,
            Solution solution,
            IStreamingFindLiteralReferencesProgress progress,
            CancellationToken cancellationToken)
        {
            var handled = await TryFindLiteralReferencesInServiceProcessAsync(
                value, solution, progress, cancellationToken).ConfigureAwait(false);
            if (handled)
            {
                return;
            }

            await FindLiteralReferencesInCurrentProcessAsync(
                value, solution, progress, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> TryFindLiteralReferencesInServiceProcessAsync(
            object value,
            Solution solution,
            IStreamingFindLiteralReferencesProgress progress,
            CancellationToken cancellationToken)
        {
            var client = await solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return false;
            }

            // Create a callback that we can pass to the server process to hear about the 
            // results as it finds them.  When we hear about results we'll forward them to
            // the 'progress' parameter which will then update the UI.
            var serverCallback = new FindLiteralsServerCallback(solution, progress, cancellationToken);

            using (var session = await client.TryCreateCodeAnalysisServiceSessionAsync(
                solution, serverCallback, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    return false;
                }

                await session.InvokeAsync(
                    nameof(IRemoteSymbolFinder.FindLiteralReferencesAsync),
                    value).ConfigureAwait(false);
            }

            return true;
        }
    }
}