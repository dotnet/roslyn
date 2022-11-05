// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public async IAsyncEnumerable<INavigateToSearchResult> SearchGeneratedDocumentsAsync(
            Project project,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);

            await foreach (var item in ConvertItemsAsync(
                project.Solution, activeDocument, SearchGeneratedDocumentsWorkerAsync(), cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }

            IAsyncEnumerable<RoslynNavigateToItem> SearchGeneratedDocumentsWorkerAsync()
            {
                var solution = project.Solution;

                if (client != null)
                {
                    var channel = Channel.CreateUnbounded<RoslynNavigateToItem>();

                    // Kick off the work to do the search in another thread.  That work will push the results into the
                    // channel.  When the work finishes (for any reason, including cancellation), the channel will be 
                    // completed.
                    Task.Run(() => client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                            solution,
                            (service, solutionInfo, callbackId, cancellationToken) =>
                                service.SearchGeneratedDocumentsAsync(solutionInfo, project.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                            new NavigateToSearchServiceCallback(channel), cancellationToken), cancellationToken)
                        .CompletesChannel(channel);

                    return channel.Reader.ReadAllAsync(cancellationToken);
                }
                else
                {
                    return SearchGeneratedDocumentsInCurrentProcessAsync(
                        project, searchPattern, kinds, cancellationToken);
                }
            }
        }

        public static async IAsyncEnumerable<RoslynNavigateToItem> SearchGeneratedDocumentsInCurrentProcessAsync(
            Project project,
            string pattern,
            IImmutableSet<string> kinds,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(pattern);

            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            // First generate all the source-gen docs.  Then handoff to the standard search routine to find matches in them.  
            var generatedDocs = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            await foreach (var item in ProcessDocumentsAsync(searchDocument: null, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, generatedDocs.ToSet<Document>(), cancellationToken).ConfigureAwait(false))
                yield return item;
        }
    }
}
