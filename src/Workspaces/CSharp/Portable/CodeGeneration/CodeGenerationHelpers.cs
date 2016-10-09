// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class CodeGenerationHelpers
    {
        public static ArrowExpressionClauseSyntax TryConvertToExpressionBody(BlockSyntax block)
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
                    return returnStatement.Expression;
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