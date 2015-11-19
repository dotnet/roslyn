// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportFormattingRule(Name, LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = AnchorIndentationFormattingRule.Name)]
    internal class QueryExpressionFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Query Expressions Formatting Rule";

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            var queryExpression = node as QueryExpressionSyntax;
            if (queryExpression != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, queryExpression.GetFirstToken(includeZeroWidth: true), queryExpression.GetLastToken(includeZeroWidth: true));
            }
        }

        private void AddIndentBlockOperationsForFromClause(List<IndentBlockOperation> list, FromClauseSyntax fromClause)
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

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<IndentBlockOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            var queryExpression = node as QueryExpressionSyntax;
            if (queryExpression != null)
            {
                AddIndentBlockOperationsForFromClause(list, queryExpression.FromClause);

                foreach (var queryClause in queryExpression.Body.Clauses)
                {
                    // if it is nested query expression
                    var fromClause = queryClause as FromClauseSyntax;
                    if (fromClause != null)
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

        public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            var queryClause = node as QueryClauseSyntax;
            if (queryClause != null)
            {
                var firstToken = queryClause.GetFirstToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, firstToken, queryClause.GetLastToken(includeZeroWidth: true));
            }

            var selectOrGroupClause = node as SelectOrGroupClauseSyntax;
            if (selectOrGroupClause != null)
            {
                var firstToken = selectOrGroupClause.GetFirstToken(includeZeroWidth: true);
                AddAnchorIndentationOperation(list, firstToken, selectOrGroupClause.GetLastToken(includeZeroWidth: true));
            }

            var continuation = node as QueryContinuationSyntax;
            if (continuation != null)
            {
                AddAnchorIndentationOperation(list, continuation.IntoKeyword, continuation.GetLastToken(includeZeroWidth: true));
            }
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
        {
            if (previousToken.IsNestedQueryExpression())
            {
                return CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines);
            }

            // skip the very first from keyword
            if (currentToken.IsFirstFromKeywordInExpression())
            {
                return nextOperation.Invoke();
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
                        if (optionSet.GetOption(CSharpFormattingOptions.NewLineForClausesInQuery))
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

            return nextOperation.Invoke();
        }
    }
}
