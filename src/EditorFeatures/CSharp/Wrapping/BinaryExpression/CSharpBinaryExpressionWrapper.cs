//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Editor.Wrapping;
//using Microsoft.CodeAnalysis.Options;
//using Microsoft.CodeAnalysis.PooledObjects;
//using Microsoft.CodeAnalysis.Text;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent;
//using Microsoft.CodeAnalysis.Formatting;
//using Microsoft.CodeAnalysis.Options;
//using Microsoft.CodeAnalysis.PooledObjects;
//using Microsoft.CodeAnalysis.Shared.Extensions;
//using Microsoft.CodeAnalysis.Text;
//using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
//using Microsoft.CodeAnalysis.CSharp;

//namespace Microsoft.CodeAnalysis.Editor.CSharp.Wrapping.BinaryExpression
//{
//    internal class CSharpBinaryExpressionWrapper : AbstractWrapper
//    {
//        public override async Task<ImmutableArray<CodeAction>> ComputeRefactoringsAsync(
//            Document document, int position, SyntaxNode node, CancellationToken cancellationToken)
//        {
//            if (!(node is BinaryExpressionSyntax binaryExpr))
//            {
//                return default;
//            }

//            if (!IsLogicalExpression(binaryExpr))
//            {
//                return default;
//            }

//            // Don't process this binary expression if it's in a parent logical expr.  We'll just
//            // allow our caller to walk up to that and call back into us to handle.  This way, we're
//            // always starting at the topmost logical binary expr.
//            if (IsLogicalExpression(binaryExpr.Parent))
//            {
//                return default;
//            }

//            var exprsAndOperators = GetExpressionsAndOperators(binaryExpr);
//            var containsUnformattableContent = await ContainsUnformattableContentAsync(
//                document, exprsAndOperators, cancellationToken).ConfigureAwait(false);

//            if (containsUnformattableContent)
//            {
//                return default;
//            }

//            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
//            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
//            var computer = new CodeActionComputer(
//                this, document, sourceText, options, binaryExpr, exprsAndOperators);

//            return await computer.GetTopLevelCodeActionsAsync(cancellationToken).ConfigureAwait(false);
//        }

//        private static ImmutableArray<SyntaxNodeOrToken> GetExpressionsAndOperators(BinaryExpressionSyntax binaryExpr)
//        {
//            var result = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();

//            AddExpressionsAndOperators(binaryExpr, result);

//            return result.ToImmutableAndFree();
//        }

//        private static void AddExpressionsAndOperators(
//            ExpressionSyntax expr, ArrayBuilder<SyntaxNodeOrToken> result)
//        {
//            if (expr is BinaryExpressionSyntax binaryExpr)
//            {
//                AddExpressionsAndOperators(binaryExpr.Left, result);
//                result.Add(binaryExpr.OperatorToken);
//                AddExpressionsAndOperators(binaryExpr.Right, result);
//            }
//            else
//            {
//                result.Add(expr);
//            }
//        }

//        private static bool IsLogicalExpression(SyntaxNode node)
//            => node.Kind() == SyntaxKind.LogicalAndExpression ||
//               node.Kind() == SyntaxKind.LogicalAndExpression;

//        private class CodeActionComputer : AbstractComputer<CSharpBinaryExpressionWrapper>
//        {
//            private readonly BinaryExpressionSyntax _binaryExpression;
//            private readonly string _indentationString;

//            public CodeActionComputer(
//                CSharpBinaryExpressionWrapper service,
//                Document document,
//                SourceText originalSourceText,
//                DocumentOptionSet options,
//                BinaryExpressionSyntax binaryExpression)
//                : base(service, document, originalSourceText, options)
//            {
//                _binaryExpression = binaryExpression;
//                _indentationString = OriginalSourceText.GetOffset(binaryExpression.Span.End)
//                                                       .CreateIndentationString(UseTabs, TabSize);
//            }

//            protected override Task<TextSpan> GetSpanToFormatAsync(
//                Document newDocument, CancellationToken cancellationToken)
//            {
//                return default;
//            }

//            protected override async Task AddTopLevelCodeActionsAsync(
//                ArrayBuilder<CodeAction> codeActions,
//                HashSet<string> seenDocuments,
//                CancellationToken cancellationToken)
//            {
//                codeActions.AddIfNotNull(await GetUnwrapCodeAction(seenDocuments, cancellationToken).ConfigureAwait(false));
//            }

//            private async Task<CodeAction> GetUnwrapCodeAction(HashSet<string> seenDocuments, CancellationToken cancellationToken)
//            {
//                var edits = GetUnwrapEdits();
//                var title = FeaturesResources.Unwrap_expression;

//                return await CreateCodeActionAsync(
//                    seenDocuments, edits, parentTitle: nameof(BinaryExpressionSyntax), title, cancellationToken).ConfigureAwait(false);
//            }

//            private ImmutableArray<TextChange> GetUnwrapEdits()
//            {
//                var result = ArrayBuilder<TextChange>.GetInstance();

//                AddUnwrapEdits(result, _binaryExpression);

//                return result.ToImmutableAndFree();

//                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);

//                foreach (var comma in _listItems.GetSeparators())
//                {
//                    result.Add(DeleteBetween(comma.GetPreviousToken(), comma));
//                    result.Add(DeleteBetween(comma, comma.GetNextToken()));
//                }

//                result.Add(DeleteBetween(_listItems.Last(), _listSyntax.GetLastToken()));
//                return result.ToImmutableAndFree();
//            }

//            private void AddUnwrapEdits(
//                ArrayBuilder<TextChange> result, BinaryExpressionSyntax binaryExpression)
//            {
//                if (binaryExpression == null)
//                {
//                    return;
//                }

//                AddUnwrapEdits(result, binaryExpression.Left as BinaryExpressionSyntax);
//                AddUnwrapEdits(result, binaryExpression.Right as BinaryExpressionSyntax);
//            }
//        }
//    }
//}
