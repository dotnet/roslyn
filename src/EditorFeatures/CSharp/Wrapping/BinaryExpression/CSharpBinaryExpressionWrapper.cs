// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Wrapping;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Wrapping.BinaryExpression
{
    internal class CSharpBinaryExpressionWrapper : AbstractWrapper
    {
        public override async Task<ICodeActionComputer> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (!(node is BinaryExpressionSyntax binaryExpr))
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

        private static ImmutableArray<SyntaxNodeOrToken> GetExpressionsAndOperators(BinaryExpressionSyntax binaryExpr)
        {
            var result = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();

            AddExpressionsAndOperators(binaryExpr, result);

            return result.ToImmutableAndFree();
        }

        private static void AddExpressionsAndOperators(
            ExpressionSyntax expr, ArrayBuilder<SyntaxNodeOrToken> result)
        {
            if (IsLogicalExpression(expr))
            {
                var binaryExpr = (BinaryExpressionSyntax)expr;
                AddExpressionsAndOperators(binaryExpr.Left, result);
                result.Add(binaryExpr.OperatorToken);
                AddExpressionsAndOperators(binaryExpr.Right, result);
            }
            else
            {
                result.Add(expr);
            }
        }

        private static bool IsLogicalExpression(SyntaxNode node)
            => node.Kind() == SyntaxKind.LogicalAndExpression ||
               node.Kind() == SyntaxKind.LogicalOrExpression;

        private class BinaryExpressionCodeActionComputer : AbstractCodeActionComputer<CSharpBinaryExpressionWrapper>
        {
            private readonly BinaryExpressionSyntax _binaryExpression;
            private readonly ImmutableArray<SyntaxNodeOrToken> _exprsAndOperators;
            private readonly SyntaxTriviaList _indentationTrivia;

            public BinaryExpressionCodeActionComputer(
                CSharpBinaryExpressionWrapper service,
                Document document,
                SourceText originalSourceText,
                DocumentOptionSet options,
                BinaryExpressionSyntax binaryExpression,
                ImmutableArray<SyntaxNodeOrToken> exprsAndOperators,
                CancellationToken cancellationToken)
                : base(service, document, originalSourceText, options, cancellationToken)
            {
                _binaryExpression = binaryExpression;
                _exprsAndOperators = exprsAndOperators;

                var generator = SyntaxGenerator.GetGenerator(document);
                var indentationString = OriginalSourceText.GetOffset(binaryExpression.Span.Start)
                                                          .CreateIndentationString(UseTabs, TabSize);

                _indentationTrivia = new SyntaxTriviaList(generator.Whitespace(indentationString));
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
            {
                var actions = ArrayBuilder<WrapItemsAction>.GetInstance();

                actions.Add(await GetWrapCodeActionAsync(includeOperators: false).ConfigureAwait(false));
                actions.Add(await GetWrapCodeActionAsync(includeOperators: true).ConfigureAwait(false));
                actions.Add(await GetUnwrapCodeActionAsync().ConfigureAwait(false));

                return ImmutableArray.Create(new WrappingGroup(
                    isInlinable: true, actions.ToImmutableAndFree()));
            }

            private Task<WrapItemsAction> GetWrapCodeActionAsync(bool includeOperators)
                => TryCreateCodeActionAsync(
                    GetWrapEdits(includeOperators),
                    FeaturesResources.Wrapping,
                    includeOperators
                        ? FeaturesResources.Wrap_expression_including_operators
                        : FeaturesResources.Wrap_expression);

            private ImmutableArray<Edit> GetWrapEdits(bool includeOperators)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                for (int i = 1; i < _exprsAndOperators.Length; i += 2)
                {
                    var left = _exprsAndOperators[i - 1].AsNode();
                    var opToken = _exprsAndOperators[i].AsToken();
                    var right = _exprsAndOperators[i + 1].AsNode();

                    if (includeOperators)
                    {
                        // convert: 
                        //      (a == b) && (c == d) to
                        //
                        //      (a == b)
                        //      && (c == d)
                        result.Add(Edit.UpdateBetween(left, NewLineTrivia, _indentationTrivia, opToken));
                        result.Add(Edit.UpdateBetween(opToken, SingleWhitespaceTrivia, NoTrivia, right));
                    }
                    else
                    {
                        // convert: 
                        //      (a == b) && (c == d) to
                        //
                        //      (a == b) &&
                        //      (c == d)
                        result.Add(Edit.UpdateBetween(left, SingleWhitespaceTrivia, NoTrivia, opToken));
                        result.Add(Edit.UpdateBetween(opToken, NewLineTrivia, _indentationTrivia, right));
                    }
                }

                return result.ToImmutableAndFree();
            }

            private Task<WrapItemsAction> GetUnwrapCodeActionAsync()
                => TryCreateCodeActionAsync(
                    GetUnwrapEdits(), FeaturesResources.Wrapping, FeaturesResources.Unwrap_expression);

            private ImmutableArray<Edit> GetUnwrapEdits()
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                for (int i = 0; i < _exprsAndOperators.Length - 1; i++)
                {
                    result.Add(Edit.UpdateBetween(
                        _exprsAndOperators[i], SingleWhitespaceTrivia,
                        NoTrivia, _exprsAndOperators[i + 1]));
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
