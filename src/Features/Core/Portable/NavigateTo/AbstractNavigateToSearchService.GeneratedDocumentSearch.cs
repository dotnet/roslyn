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
        public IAsyncEnumerable<INavigateToSearchResult> SearchGeneratedDocumentsAsync(
            Project project,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            var result = SearchGeneratedDocumentsWorkerAsync(cancellationToken);
            return ConvertItemsAsync(project.Solution, activeDocument, result, cancellationToken);

            async IAsyncEnumerable<RoslynNavigateToItem> SearchGeneratedDocumentsWorkerAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var solution = project.Solution;

                var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var channel = Channel.CreateUnbounded<RoslynNavigateToItem>();

                    Task.Run(async () =>
                    {
                        await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                            solution,
                             (service, solutionInfo, callbackId, cancellationToken) =>
                                service.SearchGeneratedDocumentsAsync(solutionInfo, project.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                             new NavigateToSearchServiceCallback(channel), cancellationToken).ConfigureAwait(false);
                    }, cancellationToken).CompletesChannel(channel);

                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                        yield return item;
                }
                else
                {
                    var result = SearchGeneratedDocumentsInCurrentProcessAsync(
                        project, searchPattern, kinds, cancellationToken);

                    await foreach (var item in result.ConfigureAwait(false))
                        yield return item;
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
