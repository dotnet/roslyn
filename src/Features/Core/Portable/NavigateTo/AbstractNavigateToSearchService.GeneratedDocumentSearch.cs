// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
            Project project,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            var onItemFound = GetOnItemFoundCallback(solution, onResultFound, cancellationToken);

            if (client != null)
            {
                var callback = new NavigateToSearchServiceCallback(onItemFound);

                // don't need to sync the entire solution when searching a particular project.
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    project,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                        service.SearchGeneratedDocumentsAsync(solutionInfo, project.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchGeneratedDocumentsInCurrentProcessAsync(
                project, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        public static async Task SearchGeneratedDocumentsInCurrentProcessAsync(
            Project project,
            string pattern,
            IImmutableSet<string> kinds,
            Func<RoslynNavigateToItem, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(pattern);

            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            // First generate all the source-gen docs.  Then handoff to the standard search routine to find matches in them.  
            var generatedDocs = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            await ProcessDocumentsAsync(searchDocument: null, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onResultFound, generatedDocs.ToSet<Document>(), cancellationToken).ConfigureAwait(false);
        }
    }
}
