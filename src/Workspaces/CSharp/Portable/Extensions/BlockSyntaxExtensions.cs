// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BlockSyntaxExtensions
    {
        public static bool TryConvertToExpressionBody(
            this BlockSyntax block, ParseOptions options,
            ExpressionBodyPreference preference,
            out ArrowExpressionClauseSyntax arrowExpression,
            out SyntaxToken semicolonToken)
        {
            if (preference != ExpressionBodyPreference.Never &&
                (options as CSharpParseOptions)?.LanguageVersion >= LanguageVersion.CSharp7)
            {
                if (block != null && block.Statements.Count == 1)
                {
                    var firstStatement = block.Statements[0];

                    if (TryGetExpression(firstStatement, out var expression, out semicolonToken) &&
                        MatchesPreference(expression, preference))
                    {
                        arrowExpression = SyntaxFactory.ArrowExpressionClause(expression);

                        // The close brace of the block may have important trivia on it (like 
                        // comments or directives).  Preserve them on the semicolon when we
                        // convert to an expression body.
                        semicolonToken = semicolonToken.WithAppendedTrailingTrivia(
                            block.CloseBraceToken.LeadingTrivia.Where(t => !t.IsWhitespaceOrEndOfLine()));
                        return true;
                    }
                }
            }

            arrowExpression = null;
            semicolonToken = default(SyntaxToken);
            return false;
        }

        public static bool MatchesPreference(
            ExpressionSyntax expression, ExpressionBodyPreference preference)
        {
            if (preference == ExpressionBodyPreference.WhenPossible)
            {
                return true;
            }

            Contract.ThrowIfFalse(preference == ExpressionBodyPreference.WhenOnSingleLine);
            return CSharpSyntaxFactsService.Instance.IsOnSingleLine(expression, fullSpan: false);
        }

        private static bool TryGetExpression(
            StatementSyntax firstStatement, out ExpressionSyntax expression, out SyntaxToken semicolonToken)
        {
            if (firstStatement is ExpressionStatementSyntax exprStatement)
            {
                expression = exprStatement.Expression;
                semicolonToken = exprStatement.SemicolonToken;
                return true;
            }
            else if (firstStatement is ReturnStatementSyntax returnStatement)
            {
                if (returnStatement.Expression != null)
                {
                    // If there are any comments or directives on the return keyword, move them to
                    // the expression.
                    expression = firstStatement.GetLeadingTrivia().Any(t => t.IsDirective || t.IsSingleOrMultiLineComment())
                        ? returnStatement.Expression.WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                        : returnStatement.Expression;
                    semicolonToken = returnStatement.SemicolonToken;
                    return true;
                }
            }
            else if (firstStatement is ThrowStatementSyntax throwStatement)
            {
                if (throwStatement.Expression != null)
                {
                    expression = SyntaxFactory.ThrowExpression(throwStatement.ThrowKeyword, throwStatement.Expression);
                    semicolonToken = throwStatement.SemicolonToken;
                    return true;
                }
            }

            expression = null;
            semicolonToken = default(SyntaxToken);
            return false;
        }
    }
}