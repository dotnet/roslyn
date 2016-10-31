// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BlockSyntaxExtensions
    {
        public static ArrowExpressionClauseSyntax TryConvertToExpressionBody(
            this BlockSyntax block, ParseOptions options)
        {
            if ((options as CSharpParseOptions)?.LanguageVersion >= LanguageVersion.CSharp7)
            {
                if (block != null && block.Statements.Count == 1)
                {
                    var firstStatement = block.Statements[0];
                    var expression = TryGetExpression(firstStatement);
                    if (expression != null)
                    {
                        return SyntaxFactory.ArrowExpressionClause(expression);
                    }
                }
            }

            return null;
        }

        private static ExpressionSyntax TryGetExpression(StatementSyntax firstStatement)
        {
            if (firstStatement.Kind() == SyntaxKind.ExpressionStatement)
            {
                return ((ExpressionStatementSyntax)firstStatement).Expression;
            }
            else if (firstStatement.Kind() == SyntaxKind.ReturnStatement)
            {
                var returnStatement = (ReturnStatementSyntax)firstStatement;
                if (returnStatement.Expression != null)
                {
                    // If there are any comments on the return keyword, move them to
                    // the expression.
                    return firstStatement.GetLeadingTrivia().Any(t => t.IsSingleOrMultiLineComment())
                        ? returnStatement.Expression.WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                        : returnStatement.Expression;
                }
            }
            else if (firstStatement.Kind() == SyntaxKind.ThrowStatement)
            {
                var throwStatement = (ThrowStatementSyntax)firstStatement;
                if (throwStatement.Expression != null)
                {
                    return SyntaxFactory.ThrowExpression(throwStatement.ThrowKeyword, throwStatement.Expression);
                }
            }

            return null;
        }
    }
}