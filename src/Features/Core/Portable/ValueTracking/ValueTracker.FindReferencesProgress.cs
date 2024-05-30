// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ValueTracking;

internal static partial class ValueTracker
{
    private class FindReferencesProgress(OperationCollector valueTrackingProgressCollector) : IStreamingFindReferencesProgress, IStreamingProgressTracker
    {
        private readonly OperationCollector _operationCollector = valueTrackingProgressCollector;

        public IStreamingProgressTracker ProgressTracker => this;

        public ValueTask AddItemsAsync(int count, CancellationToken _) => new();

        public ValueTask ItemsCompletedAsync(int count, CancellationToken _) => new();

        public ValueTask OnCompletedAsync(CancellationToken _) => new();

        public ValueTask OnDefinitionFoundAsync(SymbolGroup symbolGroup, CancellationToken _) => new();

        public async ValueTask OnReferencesFoundAsync(
            ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)> references,
            CancellationToken cancellationToken)
        {
            foreach (var (_, symbol, location) in references)
                await OnReferenceFoundAsync(symbol, location, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask OnReferenceFoundAsync(
            ISymbol symbol, ReferenceLocation location, CancellationToken cancellationToken)
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
                var node = location.Location.FindNode(cancellationToken);

                // Assignments to a member using a "this." or "Me." result in the node being an
                // identifier and the parent of the node being the member access expression. The member
                // access expression gives the right value for "IsLeftSideOfAnyAssignment" but also
                // gives the correct operation, where as the IdentifierSyntax does not.
                if (syntaxFacts.IsMemberAccessExpression(node.Parent))
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
            else if (symbol is IPropertySymbol { IsIndexer: true } propertySymbol)
            {
                // If the location isn't written to but it's an index accessor we still want to follow
                // it as a source to be tracked for the property itself. Specifically, we want to check
                // invocation sites and track the arguments being used in the invocation and potentially
                // the expression being indexed to, if it is relavent (as determined by the OperationCollector).
                var node = location.Location.FindNode(cancellationToken);
                var syntaxFacts = location.Document.GetRequiredLanguageService<ISyntaxFactsService>();

                var elementAccess = node.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsElementAccessExpression);
                if (elementAccess is null)
                {
                    return;
                }

                syntaxFacts.GetPartsOfElementAccessExpression(elementAccess, out var expression, out var argumentList);
                var semanticModel = await location.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                if (argumentList is not null)
                {
                    foreach (var argument in syntaxFacts.GetArgumentsOfArgumentList(argumentList))
                    {
                        var argumentOperation = semanticModel.GetOperation(argument, cancellationToken);
                        if (argumentOperation is not null)
                        {
                            await _operationCollector.VisitAsync(argumentOperation, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                if (expression is not null)
                {
                    // We do not want to track "this" as part of an index operation, but we do 
                    // want to track other variables that are accessed. Arguably they "contribute" even if
                    // not specifically to the argument of the element access.
                    if (syntaxFacts.IsThisExpression(expression))
                    {
                        return;
                    }

                    var expressionOperation = semanticModel.GetOperation(expression, cancellationToken);
                    if (expressionOperation is not null)
                    {
                        await _operationCollector.VisitAsync(expressionOperation, cancellationToken).ConfigureAwait(false);
                    }
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
