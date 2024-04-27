// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

#if NET
using Parallel = System.Threading.Tasks.Parallel;
#else
using Parallel = Roslyn.Utilities.ParallelUtilities;
#endif

internal abstract partial class AbstractNavigateToSearchService
{
    public async Task SearchGeneratedDocumentsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        Contract.ThrowIfTrue(projects.IsEmpty);
        Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument, onResultsFound, cancellationToken);

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted);

            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                // Sync and search the full solution snapshot.  While this function is called serially per project,
                // we want to operate on the same solution snapshot on the OOP side per project so that we can
                // benefit from things like cached compilations.  If we produced different snapshots, those
                // compilations would not be shared and we'd have to rebuild them.
                solution,
                (service, solutionInfo, callbackId, cancellationToken) =>
                    service.SearchGeneratedDocumentsAsync(solutionInfo, projects.SelectAsArray(p => p.Id), searchPattern, [.. kinds], callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        await SearchGeneratedDocumentsInCurrentProcessAsync(
            projects, searchPattern, kinds, onItemsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SearchGeneratedDocumentsInCurrentProcessAsync(
        ImmutableArray<Project> projects,
        string pattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<RoslynNavigateToItem>, Task> onItemsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        var channel = Channel.CreateUnbounded<RoslynNavigateToItem>(s_channelOptions);

        await Task.WhenAll(
            FindAllItemsAndWriteToChannelAsync(channel.Writer, SearchProjectsAsync),
            ReadItemsFromChannelAndReportToCallbackAsync(channel.Reader, onItemsFound, cancellationToken)).ConfigureAwait(false);

        return;

        async Task SearchProjectsAsync(Action<RoslynNavigateToItem> onItemFound)
        {
            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(pattern);
            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            await Parallel.ForEachAsync(
                projects,
                cancellationToken,
                async (project, cancellationToken) =>
                {
                    // First generate all the source-gen docs.  Then handoff to the standard search routine to find matches in them.  
                    var sourceGeneratedDocs = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);

                    await Parallel.ForEachAsync(
                        sourceGeneratedDocs,
                        cancellationToken,
                        (document, cancellationToken) => ProcessDocumentAsync(
                            document, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onItemFound, cancellationToken)).ConfigureAwait(false);

                    await onProjectCompleted().ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
    }
}
