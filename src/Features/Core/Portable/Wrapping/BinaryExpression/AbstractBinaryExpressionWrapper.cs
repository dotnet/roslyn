// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Wrapping.BinaryExpression
{
    using Microsoft.CodeAnalysis.Indentation;

    internal abstract partial class AbstractBinaryExpressionWrapper<TBinaryExpressionSyntax> : AbstractSyntaxWrapper
        where TBinaryExpressionSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly IPrecedenceService _precedenceService;

        protected AbstractBinaryExpressionWrapper(
            IIndentationService indentationService,
            ISyntaxFactsService syntaxFacts,
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

        public sealed override async Task<ICodeActionComputer> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (!(node is TBinaryExpressionSyntax binaryExpr))
            {
                return null;
            }

            var precedence = _precedenceService.GetPrecedenceKind(binaryExpr);
            if (precedence == PrecedenceKind.Other)
            {
                return null;
            }

            // Don't process this binary expression if it's in a parent binary expr of the same
            // precedence.  We'll just allow our caller to walk up to that and call back into us 
            // to handle.  This way, we're always starting at the topmost binary expr of this
            // precedence.
            if (binaryExpr.Parent is TBinaryExpressionSyntax parentBinary &&
                precedence == _precedenceService.GetPrecedenceKind(parentBinary))
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
            {
                return null;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new BinaryExpressionCodeActionComputer(
                this, document, sourceText, options, binaryExpr,
                exprsAndOperators, cancellationToken);
        }

        private ImmutableArray<SyntaxNodeOrToken> GetExpressionsAndOperators(
            PrecedenceKind precedence, TBinaryExpressionSyntax binaryExpr)
        {
            var result = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
            AddExpressionsAndOperators(precedence, binaryExpr, result);
            return result.ToImmutableAndFree();
        }

        private void AddExpressionsAndOperators(
            PrecedenceKind precedence, SyntaxNode expr, ArrayBuilder<SyntaxNodeOrToken> result)
        {
            if (expr is TBinaryExpressionSyntax &&
                precedence == _precedenceService.GetPrecedenceKind(expr))
            {
                _syntaxFacts.GetPartsOfBinaryExpression(
                    expr, out var left, out var opToken, out var right);
                AddExpressionsAndOperators(precedence, left, result);
                result.Add(opToken);
                AddExpressionsAndOperators(precedence, right, result);
            }
            else
            {
                result.Add(expr);
            }
        }
    }
}
