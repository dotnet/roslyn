// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ArrowExpressionClauseSyntaxExtensions
    {
        public static BlockSyntax ConvertToBlock(
            this ArrowExpressionClauseSyntax arrowExpression,
            SyntaxToken semicolonToken,
            bool createReturnStatementForExpression)
        {
            var statement = ConvertToStatement(arrowExpression.Expression, semicolonToken, createReturnStatementForExpression);
            statement = statement.WithPrependedLeadingTrivia(arrowExpression.ArrowToken.TrailingTrivia);
            return SyntaxFactory.Block(statement);
        }

        private static StatementSyntax ConvertToStatement(
            ExpressionSyntax expression, 
            SyntaxToken semicolonToken, 
            bool createReturnStatementForExpression)
        {
            if (expression.IsKind(SyntaxKind.ThrowExpression))
            {
                var throwExpression = (ThrowExpressionSyntax)expression;
                return SyntaxFactory.ThrowStatement(throwExpression.ThrowKeyword, throwExpression.Expression, semicolonToken);
            }
            else if (createReturnStatementForExpression)
            {
                return SyntaxFactory.ReturnStatement(expression)
                                    .WithSemicolonToken(semicolonToken);
            }
            else
            {
                return SyntaxFactory.ExpressionStatement(expression)
                                    .WithSemicolonToken(semicolonToken);
            }
        }
    }
}
