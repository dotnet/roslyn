// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Remote;
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
            var solution = project.Solution;

            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                    solution,
                    (service, solutionInfo, cancellationToken) =>
                        service.SearchGeneratedDocumentsAsync(solutionInfo, project.Id, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
            else
            {
                var result = SearchGeneratedDocumentsInCurrentProcessAsync(
                    project, searchPattern, kinds, cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
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
