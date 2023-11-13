// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public async Task SearchGeneratedDocumentsAsync(
            Solution solution,
            ImmutableArray<Project> projects,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            Func<Project, INavigateToSearchResult, Task> onResultFound,
            Func<Task> onProjectCompleted,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(projects.IsEmpty);
            Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

            var onItemFound = GetOnItemFoundCallback(solution, activeDocument, onResultFound, cancellationToken);

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var callback = new NavigateToSearchServiceCallback(onItemFound, onProjectCompleted);

                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    // Sync and search the full solution snapshot.  While this function is called serially per project,
                    // we want to operate on the same solution snapshot on the OOP side per project so that we can
                    // benefit from things like cached compilations.  If we produced different snapshots, those
                    // compilations would not be shared and we'd have to rebuild them.
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                        service.SearchGeneratedDocumentsAsync(solutionInfo, projects.SelectAsArray(p => p.Id), searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchGeneratedDocumentsInCurrentProcessAsync(
                projects, searchPattern, kinds, onItemFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
        }

        public static async Task SearchGeneratedDocumentsInCurrentProcessAsync(
            ImmutableArray<Project> projects,
            string pattern,
            IImmutableSet<string> kinds,
            Func<RoslynNavigateToItem, Task> onItemFound,
            Func<Task> onProjectCompleted,
            CancellationToken cancellationToken)
        {
            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(pattern);
            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            // Projects is already sorted in dependency order by the host.  Process in that order so that prior
            // compilations are available for later projects when needed.
            foreach (var project in projects)
            {
                // First generate all the source-gen docs.  Then handoff to the standard search routine to find matches in them.  
                var sourceGeneratedDocs = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
                using var _ = GetPooledHashSet<Document>(sourceGeneratedDocs, out var documents);

                await ProcessDocumentsAsync(
                    searchDocument: null, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onItemFound, documents, cancellationToken).ConfigureAwait(false);

                await onProjectCompleted().ConfigureAwait(false);
            }
        }
    }
}
