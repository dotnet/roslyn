// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class ReferencingSyntaxFinder(Solution solution, CancellationToken cancellationToken)
{
    private static readonly FindReferencesSearchOptions s_options = new()
    {
        AssociatePropertyReferencesWithSpecificAccessor = false,
        Cascade = false,
        DisplayAllDefinitions = false,
        Explicit = true,
        UnidirectionalHierarchyCascade = false
    };

    public IEnumerable<SyntaxNode> Find(ISymbol symbol)
        => FindAsync(symbol).ToBlockingEnumerable(cancellationToken);

    public async IAsyncEnumerable<SyntaxNode> FindAsync(ISymbol symbol)
    {
        using var _ = PooledHashSet<SyntaxNode>.GetInstance(out var cachedRoots);

        var reader = await ProducerConsumer<ReferenceLocation>.RunChannelAsync(
            ProducerConsumerOptions.SingleReaderOptions,
            async static (callback, args, cancellationToken) =>
            {
                var (solution, symbol) = args;

                await SymbolFinder.FindReferencesAsync(
                    symbol,
                    solution,
                    new Progress(callback),
                    documents: null,
                    s_options,
                    cancellationToken).ConfigureAwait(false);
            },
            static (reader, _, _) => Task.FromResult(reader),
            args: (solution, symbol),
            cancellationToken).ConfigureAwait(false);

        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            var root = await item.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                continue;

            // Hold onto the root so that if we find more references in the same document, we don't have to reparse it.
            cachedRoots.Add(root);
            yield return item.Location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken);
        }
    }

    private sealed class Progress(Action<ReferenceLocation> callback) : IStreamingFindReferencesProgress
    {
        public ValueTask OnStartedAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask OnCompletedAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask OnReferencesFoundAsync(ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)> references, CancellationToken cancellationToken)
        {
            foreach (var (_, _, location) in references)
                callback(location);

            return ValueTask.CompletedTask;
        }

        public IStreamingProgressTracker ProgressTracker
            => NoOpStreamingFindReferencesProgress.Instance.ProgressTracker;
    }
}
#endif
