// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class AbstractRefactoringHelpersService : IRefactoringHelpersService
    {
        public async Task<TSyntaxNode> TryGetSelectedNodeAsync<TSyntaxNode>(
            Document document, TextSpan selection, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
        {
            return await TryGetSelectedNodeAsync(document, selection, n => n is TSyntaxNode, cancellationToken).ConfigureAwait(false) as TSyntaxNode;
        }

        public Task<SyntaxNode> TryGetSelectedNodeAsync(Document document, TextSpan selection, Predicate<SyntaxNode> predicate, CancellationToken cancellationToken)
        {
            return TryGetSelectedNodeAsync(document, selection, predicate, DefaultNodeExtractor, cancellationToken);
        }

        public async Task<SyntaxNode> TryGetSelectedNodeAsync(Document document, TextSpan selection, Predicate<SyntaxNode> predicate, Func<SyntaxNode, ISyntaxFactsService, SyntaxNode> extractNode, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var selectionStripped = await CodeRefactoringHelpers.GetStrippedTextSpan(document, selection, cancellationToken).ConfigureAwait(false);

            // Handle selections
            // - the smallest node whose span inlcudes whole selection
            // - extraction from such node is attempted, result tested via predicate
            // - travels upwards through same-sized nodes, extracting and testing predicate
            var node = root.FindNode(selectionStripped, getInnermostNodeForTie: true);
            SyntaxNode prevNode;
            do
            {
                var wantedNode = TryGetAcceptedNodeOrExtracted(node, predicate, extractNode, syntaxFacts);
                if (wantedNode != default)
                {
                    return wantedNode;
                }

                prevNode = node;
                node = node.Parent;
            }
            while (node != null && prevNode.FullWidth() == node.FullWidth());

            // consider what selection is touching only when it's empty -> prevents
            // `[|C|] methodName(){}` from registering as relevant for method Node
            if (!selection.IsEmpty)
            {
                return default;
            }

            // get Token for current selection (empty) location
            var tokenOnSelection = root.FindToken(selectionStripped.Start);

            // Token to right or containing current selection
            var tokenToRightOrIn = tokenOnSelection.Span.Contains(selectionStripped.Start)
                ? tokenOnSelection
                : default;

            if (tokenToRightOrIn != default)
            {
                var rightNode = tokenOnSelection.Parent;
                do
                {
                    // consider either a Node that is a parent of touched Token (selection can be within) or ancestor Node of such Token whose span starts on selection
                    var wantedNode = TryGetAcceptedNodeOrExtracted(rightNode, predicate, extractNode, syntaxFacts);
                    if (wantedNode != default)
                    {
                        return wantedNode;
                    }

                    rightNode = rightNode?.Parent;
                }
                while (rightNode != null && rightNode.Span.Start == selection.Start);
            }

            // if selection inside tokenToRightOrIn -> no Token can be to Left (tokenToRightOrIn is left from selection)
            if (tokenToRightOrIn != default && tokenToRightOrIn.Span.Start != selectionStripped.Start)
            {
                return default;
            }

            // Token to left
            var tokenPreSelection = (tokenOnSelection.Span.End == selectionStripped.Start)
                ? tokenOnSelection
                : tokenOnSelection.GetPreviousToken();

            var tokenToLeft = (tokenPreSelection.Span.End == selectionStripped.Start)
                ? tokenPreSelection
                : default;

            if (tokenToLeft != default)
            {
                var leftNode = tokenToLeft.Parent;
                do
                {
                    // consider either a Node that is a parent of touched Token (selection can be within) or ancestor Node of such Token whose span ends on selection
                    var wantedNode = TryGetAcceptedNodeOrExtracted(leftNode, predicate, extractNode, syntaxFacts);
                    if (wantedNode != default)
                    {
                        return wantedNode;
                    }

                    leftNode = leftNode?.Parent;
                }
                while (leftNode != null && leftNode.Span.End == selection.Start);
            }

            // nothing found
            return default;

            static SyntaxNode TryGetAcceptedNodeOrExtracted(SyntaxNode node, Predicate<SyntaxNode> predicate, Func<SyntaxNode, ISyntaxFactsService, SyntaxNode> extractNode, ISyntaxFactsService syntaxFacts)
            {
                if (node == default)
                {
                    return default;
                }

                if (predicate(node))
                {
                    return node;
                }

                var extrNode = extractNode(node, syntaxFacts);
                if (extrNode != default && predicate(extrNode))
                {
                    return extrNode;
                }

                return default;
            }
        }

        public virtual SyntaxNode DefaultNodeExtractor(SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            // var a = b;
            // -> b
            if (syntaxFacts.IsLocalDeclarationStatement(node))
            {
                var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(node);
                if (variables.Count == 1)
                {
                    var declaredVariable = variables.First();
                    var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(declaredVariable);

                    if (initializer != default)
                    {
                        var value = syntaxFacts.GetValueOfEqualsValueClause(initializer);
                        if (value != default)
                        {
                            return value;
                        }
                    }
                }
            }

            // a = b;
            // -> b
            if (syntaxFacts.IsSimpleAssignmentStatement(node))
            {
                syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(node, out _, out _, out var rightSide);
                if (rightSide != default)
                {
                    return rightSide;
                }
            }

            return node;
        }
    }
}
