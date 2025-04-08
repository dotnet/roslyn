﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
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
        var channel = Channel.CreateUnbounded<ReferenceLocation>(new()
        {
            SingleReader = true,
            SingleWriter = false,
        });

        using var _ = cancellationToken.Register(
            static (obj, cancellationToken) => ((Channel<SourceReferenceItem>)obj!).Writer.TryComplete(new OperationCanceledException(cancellationToken)),
            state: channel);

        var progress = new Progress(channel);

        var writeTask = ProduceItemsAndWriteToChannelAsync();

        await foreach (var reference in channel.Reader.ReadAllAsync(cancellationToken))
        {
            // TODO: consider grouping by document to avoid repeated syntax root lookup

            var root = await reference.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                continue;
            }

            yield return root.FindNode(reference.Location.SourceSpan, findInTrivia: true, getInnermostNodeForTie: true);
        }

        await writeTask.ConfigureAwait(false);

        async Task ProduceItemsAndWriteToChannelAsync()
        {
            await Task.Yield().ConfigureAwait(false);

            Exception? exception = null;
            try
            {
                //await service.FindReferencesAsync(context, document, location.SourceSpan.Start, classificationOptions, cancellationToken).ConfigureAwait(false);
                await SymbolFinder.FindReferencesAsync(
                    symbol,
                    solution,
                    progress,
                    documents: null,
                    s_options,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when ((exception = ex) == null)
            {
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                // No matter what path we take (exceptional or non-exceptional), always complete the channel so the
                // writing task knows it's done.
                channel.Writer.TryComplete(exception);
            }
        }
    }

    private sealed class Progress(Channel<ReferenceLocation> channel) : IStreamingFindReferencesProgress
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
            {
                // It's ok to use TryWrite here.  TryWrite always succeeds unless the channel is completed. And the
                // channel is only ever completed by us (after produceItems completes or throws an exception) or if the
                // cancellationToken is triggered above in RunAsync. In that latter case, it's ok for writing to the
                // channel to do nothing as we no longer need to write out those assets to the pipe.
                _ = channel.Writer.TryWrite(location);
            }

            return ValueTask.CompletedTask;
        }

        public IStreamingProgressTracker ProgressTracker
            => NoOpStreamingFindReferencesProgress.Instance.ProgressTracker;
    }
}
#endif
