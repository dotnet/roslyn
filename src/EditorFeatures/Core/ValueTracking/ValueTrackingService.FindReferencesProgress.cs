// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal partial class ValueTrackingService
    {
        private class FindReferencesProgress : IStreamingFindReferencesProgress, IStreamingProgressTracker
        {
            private readonly CancellationToken _cancellationToken;
            private readonly OperationCollector _operationCollector;
            public FindReferencesProgress(OperationCollector valueTrackingProgressCollector, CancellationToken cancellationToken = default)
            {
                _operationCollector = valueTrackingProgressCollector;
                _cancellationToken = cancellationToken;
            }

            public IStreamingProgressTracker ProgressTracker => this;

            public ValueTask AddItemsAsync(int count) => new();

            public ValueTask ItemCompletedAsync() => new();

            public ValueTask OnCompletedAsync() => new();

            public ValueTask OnDefinitionFoundAsync(SymbolGroup symbolGroup) => new();

            public ValueTask OnFindInDocumentCompletedAsync(Document document) => new();

            public ValueTask OnFindInDocumentStartedAsync(Document document) => new();

            public async ValueTask OnReferenceFoundAsync(SymbolGroup _, ISymbol symbol, ReferenceLocation location)
            {
                if (!location.Location.IsInSource)
                {
                    return;
                }

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

                    // Assignments to a member using a "this." or "Me." result in the node being an
                    // identifier and the parent of the node being the member access expression. The member
                    // access expression gives the right value for "IsLeftSideOfAnyAssignment" but also
                    // gives the correct operation, where as the IdentifierSyntax does not.
                    if (node.Parent is not null && syntaxFacts.IsAnyMemberAccessExpression(node.Parent))
                    {
                        node = node.Parent;
                    }

                    if (syntaxFacts.IsLeftSideOfAnyAssignment(node))
                    {
                        await AddItemsFromAssignmentAsync(location.Document, node, _operationCollector, _cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var semanticModel = await location.Document.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                        var operation = semanticModel.GetOperation(node, _cancellationToken);
                        if (operation is null)
                        {
                            return;
                        }

                        await _operationCollector.VisitAsync(operation, _cancellationToken).ConfigureAwait(false);
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

                if (originalNode is null || originalNode.Parent is null)
                {
                    return;
                }

                var semanticModel = await document.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                var operation = semanticModel.GetOperation(originalNode.Parent, _cancellationToken);
                if (operation is not IObjectCreationOperation)
                {
                    return;
                }

                await _operationCollector.VisitAsync(operation, _cancellationToken).ConfigureAwait(false);
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
                if (operation is not IInvocationOperation)
                {
                    return;
                }

                await _operationCollector.VisitAsync(operation, _cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
