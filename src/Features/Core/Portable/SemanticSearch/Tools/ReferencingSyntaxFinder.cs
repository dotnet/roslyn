// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class ReferencingSyntaxFinder(Solution solution, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
{
    public IEnumerable<SyntaxNode> FindSyntaxNodes(ISymbol symbol)
        => FindSyntaxNodesAsync(symbol).ToBlockingEnumerable(cancellationToken);

    public async IAsyncEnumerable<SyntaxNode> FindSyntaxNodesAsync(ISymbol symbol)
    {
        var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef == null)
        {
            yield break;
        }

        var document = solution.GetDocument(syntaxRef.SyntaxTree);
        if (document == null)
        {
            yield break;
        }

        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location == null)
        {
            yield break;
        }

        var service = document.GetRequiredLanguageService<IFindUsagesService>();

        var channel = Channel.CreateUnbounded<SourceReferenceItem>(new()
        {
            SingleReader = true,
            SingleWriter = false,
        });

        using var _ = cancellationToken.Register(
            static (obj, cancellationToken) => ((Channel<SourceReferenceItem>)obj!).Writer.TryComplete(new OperationCanceledException(cancellationToken)),
            state: channel);

        var context = new FindUsagesContext(channel);

        var writeTask = ProduceItemsAndWriteToChannelAsync();

        await foreach (var reference in channel.Reader.ReadAllAsync(cancellationToken))
        {
            // TODO: consider grouping by document to avoid repeated syntax root lookup

            var root = await reference.SourceSpan.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                continue;
            }

            yield return root.FindNode(reference.SourceSpan.SourceSpan, findInTrivia: true, getInnermostNodeForTie: true);
        }

        await writeTask.ConfigureAwait(false);

        async Task ProduceItemsAndWriteToChannelAsync()
        {
            await Task.Yield().ConfigureAwait(false);

            Exception? exception = null;
            try
            {
                await service.FindReferencesAsync(context, document, location.SourceSpan.Start, classificationOptions, cancellationToken).ConfigureAwait(false);
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

    private sealed class FindUsagesContext(Channel<SourceReferenceItem> channel) : IFindUsagesContext
    {
        public async ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken)
        {
            await foreach (var reference in references)
            {
                // It's ok to use TryWrite here.  TryWrite always succeeds unless the channel is completed. And the
                // channel is only ever completed by us (after produceItems completes or throws an exception) or if the
                // cancellationToken is triggered above in RunAsync. In that latter case, it's ok for writing to the
                // channel to do nothing as we no longer need to write out those assets to the pipe.
                _ = channel.Writer.TryWrite(reference);
            }
        }

        public IStreamingProgressTracker ProgressTracker
            => NoOpStreamingFindReferencesProgress.Instance.ProgressTracker;

        public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask ReportMessageAsync(string message, NotificationSeverity severity, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask ReportNoResultsAsync(string message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
#endif
