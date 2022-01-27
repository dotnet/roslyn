// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class QueryExpressionFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Query Expressions Formatting Rule";

        private readonly CSharpSyntaxFormattingOptions _options;

        public QueryExpressionFormattingRule()
            : this(CSharpSyntaxFormattingOptions.Default)
        {
        }

        private QueryExpressionFormattingRule(CSharpSyntaxFormattingOptions options)
        {
            _options = options;
        }

        public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
        {
            var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

            if (_options.NewLines.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses) == newOptions.NewLines.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses))
            {
                return this;
            }

            return new QueryExpressionFormattingRule(newOptions);
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            if (node is QueryExpressionSyntax queryExpression)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, queryExpression.GetFirstToken(includeZeroWidth: true), queryExpression.GetLastToken(includeZeroWidth: true));
            }
        }

        private static void AddIndentBlockOperationsForFromClause(List<IndentBlockOperation> list, FromClauseSyntax fromClause)
        {
            // Only add the indent block operation if the 'in' keyword is present. Otherwise, we'll get the following:
            //
            //     from x
            //         in args
            //
            // Rather than:
            //
            //     from x
            //     in args
            //
            // However, we want to get the following result if the 'in' keyword is present to allow nested queries
            // to be formatted properly.
            //
            //     from x in
            //         args

            if (fromClause.InKeyword.IsMissing)
            {
                return;
            }

            var baseToken = fromClause.FromKeyword;
            var startToken = fromClause.Expression.GetFirstToken(includeZeroWidth: true);
            var endToken = fromClause.Expression.GetLastToken(includeZeroWidth: true);

            AddIndentBlockOperation(list, baseToken, startToken, endToken);
        }

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
        {
            nextOperation.Invoke();

            if (node is QueryExpressionSyntax queryExpression)
            {
                AddIndentBlockOperationsForFromClause(list, queryExpression.FromClause);

                foreach (var queryClause in queryExpression.Body.Clauses)
                {
                    // if it is nested query expression
                    if (queryClause is FromClauseSyntax fromClause)
                    {
                        AddIndentBlockOperationsForFromClause(list, fromClause);
                    }
                }

                // set alignment line for query expression
                var baseToken = queryExpression.GetFirstToken(includeZeroWidth: true);
                var endToken = queryExpression.GetLastToken(includeZeroWidth: true);
                if (!baseToken.IsMissing && !baseToken.Equals(endToken))
                {
                    var startToken = baseToken.GetNextToken(includeZeroWidth: true);
                    SetAlignmentBlockOperation(list, baseToken, startToken, endToken);
                }
            }
        }

        public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, in NextAnchorIndentationOperationAction nextOperation)
        {
            nextOperation.Invoke();
            switch (node)
            {
                case QueryClauseSyntax queryClause:
                    {
                        var firstToken = queryClause.GetFirstToken(includeZeroWidth: true);
                        AddAnchorIndentationOperation(list, firstToken, queryClause.GetLastToken(includeZeroWidth: true));
                        return;
                    }

                case SelectOrGroupClauseSyntax selectOrGroupClause:
                    {
                        var firstToken = selectOrGroupClause.GetFirstToken(includeZeroWidth: true);
                        AddAnchorIndentationOperation(list, firstToken, selectOrGroupClause.GetLastToken(includeZeroWidth: true));
                        return;
                    }

                case QueryContinuationSyntax continuation:
                    AddAnchorIndentationOperation(list, continuation.IntoKeyword, continuation.GetLastToken(includeZeroWidth: true));
                    return;
            }
        }

        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (previousToken.IsNestedQueryExpression())
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            // skip the very first from keyword
            if (currentToken.IsFirstFromKeywordInExpression())
            {
                return nextOperation.Invoke(in previousToken, in currentToken);
            }

            switch (currentToken.Kind())
            {
                case SyntaxKind.FromKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.LetKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.SelectKeyword:
                    if (currentToken.GetAncestor<QueryExpressionSyntax>() != null)
                    {
                        if (_options.NewLines.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses))
                        {
                            return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                        }
                        else
                        {
                            return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
                        }
                    }

                    break;
            }

            return nextOperation.Invoke(in previousToken, in currentToken);
        }
    }
}
