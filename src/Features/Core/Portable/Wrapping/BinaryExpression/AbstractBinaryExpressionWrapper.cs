// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Precedence;
using Roslyn.Utilities;

#if DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.CodeAnalysis.Wrapping.BinaryExpression
{
    internal abstract partial class AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> : AbstractSyntaxWrapper
        where TBinaryExpressionSyntax : SyntaxNode
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly IPrecedenceService _precedenceService;

        protected AbstractBinaryExpressionWrapper(
            IIndentationService indentationService,
            ISyntaxFacts syntaxFacts,
            IPrecedenceService precedenceService) : base(indentationService)
        {
            _syntaxFacts = syntaxFacts;
            _precedenceService = precedenceService;
        }

        /// <summary>
        /// Get's the language specific trivia that should be inserted before an operator if the
        /// user wants to wrap the operator to the next line.  For C# this is a simple newline-trivia.
        /// For VB, this will be a line-continuation char (<c>_</c>), followed by a newline.
        /// </summary>
        protected abstract SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine);

        public sealed override async Task<ICodeActionComputer?> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, SyntaxWrappingOptions options, bool containsSyntaxError, CancellationToken cancellationToken)
        {
            if (containsSyntaxError)
                return null;

            if (node is not TBinaryExpressionSyntax binaryExpr)
                return null;

            var precedence = _precedenceService.GetPrecedenceKind(binaryExpr);
            if (precedence == PrecedenceKind.Other)
                return null;

            // Don't process this binary expression if it's in a parent binary expr of the same or
            // lower precedence.  We'll just allow our caller to walk up to that and call back into
            // us to handle.  This way, we're always starting at the topmost binary expr of this
            // precedence.
            //
            // for example, if we have `if (a + b == c + d)` expectation is to wrap on the lower
            // precedence `==` op, not either of the `+` ops
            //
            // Note: we use `<=` when comparing precedence because lower precedence has a higher
            // value.
            if (binaryExpr.Parent is TBinaryExpressionSyntax parentBinary &&
                precedence <= _precedenceService.GetPrecedenceKind(parentBinary))
            {
                return null;
            }

            var exprsAndOperators = GetExpressionsAndOperators(precedence, binaryExpr);
#if DEBUG
            Debug.Assert(exprsAndOperators.Length >= 3);
            Debug.Assert(exprsAndOperators.Length % 2 == 1, "Should have odd number of exprs and operators");
            for (var i = 0; i < exprsAndOperators.Length; i++)
            {
                var item = exprsAndOperators[i];
                Debug.Assert(((i % 2) == 0 && item.IsNode) ||
                             ((i % 2) == 1 && item.IsToken));
            }
#endif

            var containsUnformattableContent = await ContainsUnformattableContentAsync(
                document, exprsAndOperators, cancellationToken).ConfigureAwait(false);

            if (containsUnformattableContent)
                return null;

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return new BinaryExpressionCodeActionComputer(
                this, document, sourceText, options, binaryExpr,
                exprsAndOperators, cancellationToken);
        }

        private ImmutableArray<SyntaxNodeOrToken> GetExpressionsAndOperators(
            PrecedenceKind precedence, TBinaryExpressionSyntax binaryExpr)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var result);
            AddExpressionsAndOperators(precedence, binaryExpr, result);
            return result.ToImmutable();
        }

        private void AddExpressionsAndOperators(
            PrecedenceKind precedence, SyntaxNode expr, ArrayBuilder<SyntaxNodeOrToken> result)
        {
            // In-order traverse which visit the left child -> operator in the binary expression -> right child
            using var pooledStack = SharedPools.Default<Stack<(SyntaxNode node, bool isLeafNode)>>().GetPooledObject();
            var stack = pooledStack.Object;
            // Two kinds of node in the tree:
            // 1. ValidBinaryExpression (non-leaf node), which has left child, operator and right child.
            // 2. NonValidBinaryExpression (leaf node), and we stop going deeper in the tree.
            // currentNode represents the node we try to expand. If it is null, then pop element from the stack.
            var currentNode = expr;
            while (!stack.IsEmpty() || currentNode != null)
            {
                if (currentNode != null)
                {
                    // If this is a valid binary expression, go to its left child, push the node to stack and note it is a non-leaf node
                    if (IsValidBinaryExpression(precedence, currentNode))
                    {
                        _syntaxFacts.GetPartsOfBinaryExpression(currentNode, out var left, out var _, out var _);
                        stack.Push((currentNode, false));
                        currentNode = left;
                    }
                    else
                    {
                        // This is a leaf node in the tree, push it to the stack, and start popping element.
                        stack.Push((currentNode, true));
                        currentNode = null;
                    }
                }
                else
                {
                    var (node, isLeafNode) = stack.Pop();
                    if (isLeafNode)
                    {
                        // Add the leaf node to the tree. Because it can't be expanded more, continue popping.
                        result.Add(node);
                    }
                    else
                    {
                        // This is a non-leaf node in the tree, so add its operator to result, and try to expand its right subtree
                        _syntaxFacts.GetPartsOfBinaryExpression(node, out var _, out var opToken, out var right);
                        result.Add(opToken);
                        currentNode = right;
                    }
                }
            }

            bool IsValidBinaryExpression(PrecedenceKind precedence, SyntaxNode? node)
                => node is TBinaryExpressionSyntax && precedence == _precedenceService.GetPrecedenceKind(node);
        }
    }
}
