// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Wrapping.BinaryExpression
{
    internal abstract partial class AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> : AbstractSyntaxWrapper
        where TBinaryExpressionSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;

        protected AbstractBinaryExpressionWrapper(ISyntaxFactsService syntaxFacts)
        {
            _syntaxFacts = syntaxFacts;
        }

        /// <summary>
        /// Get's the language specific trivia that should be inserted before an operator if the
        /// user wants to wrap the operator to the next line.  For C# this is a simple newline-trivia.
        /// For VB, this will be a line-continuation char (<c>_</c>), followed by a newline.
        /// </summary>
        protected abstract SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine);

        public sealed override async Task<ICodeActionComputer> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (!(node is TBinaryExpressionSyntax binaryExpr))
            {
                return default;
            }

            if (!IsLogicalExpression(binaryExpr))
            {
                return default;
            }

            // Don't process this binary expression if it's in a parent logical expr.  We'll just
            // allow our caller to walk up to that and call back into us to handle.  This way, we're
            // always starting at the topmost logical binary expr.
            if (IsLogicalExpression(binaryExpr.Parent))
            {
                return default;
            }

            var exprsAndOperators = GetExpressionsAndOperators(binaryExpr);
#if DEBUG
            Debug.Assert(exprsAndOperators.Length >= 3);
            Debug.Assert(exprsAndOperators.Length % 2 == 1, "Should have odd number of exprs and operators");
            for (int i = 0; i < exprsAndOperators.Length; i++)
            {
                var item = exprsAndOperators[i];
                Debug.Assert(((i % 2) == 0 && item.IsNode) ||
                             ((i % 2) == 1 && item.IsToken));
            }
#endif

            var containsUnformattableContent = await ContainsUnformattableContentAsync(
                document, exprsAndOperators, cancellationToken).ConfigureAwait(false);

            if (containsUnformattableContent)
            {
                return default;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new BinaryExpressionCodeActionComputer(
                this, document, sourceText, options, binaryExpr,
                exprsAndOperators, cancellationToken);
        }

        private ImmutableArray<SyntaxNodeOrToken> GetExpressionsAndOperators(TBinaryExpressionSyntax binaryExpr)
        {
            var result = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
            AddExpressionsAndOperators(binaryExpr, result);
            return result.ToImmutableAndFree();
        }

        private void AddExpressionsAndOperators(
            SyntaxNode expr, ArrayBuilder<SyntaxNodeOrToken> result)
        {
            if (IsLogicalExpression(expr))
            {
                _syntaxFacts.GetPartsOfBinaryExpression(
                    expr, out var left, out var opToken, out var right);
                AddExpressionsAndOperators(left, result);
                result.Add(opToken);
                AddExpressionsAndOperators(right, result);
            }
            else
            {
                result.Add(expr);
            }
        }

        private bool IsLogicalExpression(SyntaxNode node)
            => _syntaxFacts.IsLogicalAndExpression(node) ||
               _syntaxFacts.IsLogicalOrExpression(node);
    }
}
