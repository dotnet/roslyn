// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BlockSyntaxExtensions
    {
        public static bool TryConvertToExpressionBody(
            this BlockSyntax block, ParseOptions options,
            out ArrowExpressionClauseSyntax arrowExpression,
            out SyntaxToken semicolonToken)
        {
            if ((options as CSharpParseOptions)?.LanguageVersion >= LanguageVersion.CSharp7)
            {
                if (block != null && block.Statements.Count == 1)
                {
                    var firstStatement = block.Statements[0];

                    if (TryGetExpression(firstStatement, out var expression, out semicolonToken))
                    {
                        arrowExpression = SyntaxFactory.ArrowExpressionClause(expression);

                        semicolonToken = semicolonToken.WithAppendedTrailingTrivia(
                            block.CloseBraceToken.LeadingTrivia.Where(t =>!t.IsWhitespaceOrEndOfLine()));
                        return true;
                    }
                }
            }

            arrowExpression = null;
            semicolonToken = default(SyntaxToken);
            return false;
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
                    // If there are any comments on the return keyword, move them to
                    // the expression.
                    expression = firstStatement.GetLeadingTrivia().Any(t => t.IsSingleOrMultiLineComment())
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