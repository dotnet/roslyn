// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal partial class ValueTrackingService
    {
        private class FindReferencesProgress : IStreamingFindReferencesProgress, IStreamingProgressTracker
        {
            private readonly CancellationToken _cancellationToken;
            private readonly ValueTrackingProgressCollector _valueTrackingProgressCollector;
            public FindReferencesProgress(ValueTrackingProgressCollector valueTrackingProgressCollector, CancellationToken cancellationToken = default)
            {
                _valueTrackingProgressCollector = valueTrackingProgressCollector;
                _cancellationToken = cancellationToken;
            }

            public IStreamingProgressTracker ProgressTracker => this;

            public ValueTask AddItemsAsync(int count) => new();

            public ValueTask ItemCompletedAsync() => new();

            public ValueTask OnCompletedAsync() => new();

            public ValueTask OnDefinitionFoundAsync(ISymbol symbol) => new();

            public ValueTask OnFindInDocumentCompletedAsync(Document document) => new();

            public ValueTask OnFindInDocumentStartedAsync(Document document) => new();

            public async ValueTask OnReferenceFoundAsync(ISymbol symbol, ReferenceLocation location)
            {
                if (!location.Location.IsInSource)
                {
                    return;
                }

                var solution = location.Document.Project.Solution;

                if (symbol is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.IsConstructor())
                    {
                        await TrackConstructorAsync(location).ConfigureAwait(false);
                    }
                    else
                    {
                        // If we're searching for references to a method, we don't want to store the symbol as that method again. Instead
                        // we want to track the invocations and how to follow their source
                        await TrackMethodInvocationArgumentsAsync(location).ConfigureAwait(false);
                    }
                }
                else if (location.IsWrittenTo)
                {
                    var syntaxFacts = location.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var node = location.Location.FindNode(CancellationToken.None);

                    if (syntaxFacts.IsLeftSideOfAnyAssignment(node))
                    {
                        await AddItemsFromAssignmentAsync(location.Document, node, _valueTrackingProgressCollector, _cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await _valueTrackingProgressCollector.TryReportAsync(solution, location.Location, symbol, _cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            public ValueTask OnStartedAsync() => new();

            private async Task TrackConstructorAsync(ReferenceLocation referenceLocation)
            {
                var document = referenceLocation.Document;
                var span = referenceLocation.Location.SourceSpan;

                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                var originalNode = syntaxRoot.FindNode(span);

                if (originalNode is null)
                {
                    return;
                }

                var semanticModel = await document.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                var operation = semanticModel.GetOperation(originalNode.Parent, _cancellationToken);
                if (operation is not IObjectCreationOperation objectCreationOperation)
                {
                    return;
                }

                await TrackArgumentsAsync(objectCreationOperation.Arguments, document).ConfigureAwait(false);
            }

            private async Task TrackMethodInvocationArgumentsAsync(ReferenceLocation referenceLocation)
            {
                var document = referenceLocation.Document;
                var span = referenceLocation.Location.SourceSpan;

                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(_cancellationToken).ConfigureAwait(false);
                var originalNode = syntaxRoot.FindNode(span);

                if (originalNode is null)
                {
                    return;
                }

                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var invocationSyntax = originalNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsInvocationExpression);
                if (invocationSyntax is null)
                {
                    return;
                }

                var semanticModel = await document.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                var operation = semanticModel.GetOperation(invocationSyntax, _cancellationToken);
                if (operation is not IInvocationOperation invocationOperation)
                {
                    return;
                }

                await TrackArgumentsAsync(invocationOperation.Arguments, document).ConfigureAwait(false);
            }

            private async Task TrackArgumentsAsync(ImmutableArray<IArgumentOperation> argumentOperations, Document document)
            {
                var collectorsAndArgumentMap = argumentOperations
                    .Select(argument => (collector: CreateCollector(), argument))
                    .ToImmutableArray();

                var tasks = collectorsAndArgumentMap
                    .Select(pair => TrackExpressionAsync(pair.argument.Value, document, pair.collector, _cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);

                var items = collectorsAndArgumentMap
                    .Select(pair => pair.collector)
                    .SelectMany(collector => collector.GetItems())
                    .Reverse(); // ProgressCollector uses a Stack, and we want to maintain the order by arguments, so reverse

                foreach (var item in items)
                {
                    _valueTrackingProgressCollector.Report(item);
                }

                ValueTrackingProgressCollector CreateCollector()
                {
                    var collector = new ValueTrackingProgressCollector();
                    collector.Parent = _valueTrackingProgressCollector.Parent;
                    return collector;
                }
            }
        }
    }
}
