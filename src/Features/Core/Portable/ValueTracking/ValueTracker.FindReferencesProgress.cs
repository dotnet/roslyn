﻿// Licensed to the .NET Foundation under one or more agreements.
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
    internal static partial class ValueTracker
    {
        private class FindReferencesProgress : IStreamingFindReferencesProgress, IStreamingProgressTracker
        {
            private readonly OperationCollector _operationCollector;
            public FindReferencesProgress(OperationCollector valueTrackingProgressCollector)
            {
                _operationCollector = valueTrackingProgressCollector;
            }

            public IStreamingProgressTracker ProgressTracker => this;

            public ValueTask AddItemsAsync(int count, CancellationToken _) => new();

            public ValueTask ItemCompletedAsync(CancellationToken _) => new();

            public ValueTask OnCompletedAsync(CancellationToken _) => new();

            public ValueTask OnDefinitionFoundAsync(SymbolGroup symbolGroup, CancellationToken _) => new();

            public ValueTask OnFindInDocumentCompletedAsync(Document document, CancellationToken _) => new();

            public ValueTask OnFindInDocumentStartedAsync(Document document, CancellationToken _) => new();

            public async ValueTask OnReferenceFoundAsync(SymbolGroup _, ISymbol symbol, ReferenceLocation location, CancellationToken cancellationToken)
            {
                if (!location.Location.IsInSource)
                {
                    return;
                }

                if (symbol is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.IsConstructor())
                    {
                        await TrackConstructorAsync(location, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // If we're searching for references to a method, we don't want to store the symbol as that method again. Instead
                        // we want to track the invocations and how to follow their source
                        await TrackMethodInvocationArgumentsAsync(location, cancellationToken).ConfigureAwait(false);
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
                        await AddItemsFromAssignmentAsync(location.Document, node, _operationCollector, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var semanticModel = await location.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        var operation = semanticModel.GetOperation(node, cancellationToken);
                        if (operation is null)
                        {
                            return;
                        }

                        await _operationCollector.VisitAsync(operation, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            public ValueTask OnStartedAsync(CancellationToken _) => new();

            private async Task TrackConstructorAsync(ReferenceLocation referenceLocation, CancellationToken cancellationToken)
            {
                var document = referenceLocation.Document;
                var span = referenceLocation.Location.SourceSpan;

                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var originalNode = syntaxRoot.FindNode(span);

                if (originalNode is null || originalNode.Parent is null)
                {
                    return;
                }

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var operation = semanticModel.GetOperation(originalNode.Parent, cancellationToken);
                if (operation is not IObjectCreationOperation)
                {
                    return;
                }

                await _operationCollector.VisitAsync(operation, cancellationToken).ConfigureAwait(false);
            }

            private async Task TrackMethodInvocationArgumentsAsync(ReferenceLocation referenceLocation, CancellationToken cancellationToken)
            {
                var document = referenceLocation.Document;
                var span = referenceLocation.Location.SourceSpan;

                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
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

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var operation = semanticModel.GetOperation(invocationSyntax, cancellationToken);
                if (operation is not IInvocationOperation)
                {
                    return;
                }

                await _operationCollector.VisitAsync(operation, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
