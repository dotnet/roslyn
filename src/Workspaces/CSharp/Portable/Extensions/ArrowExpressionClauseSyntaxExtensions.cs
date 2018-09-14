// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ArrowExpressionClauseSyntaxExtensions
    {
        public static bool TryConvertToBlock(
            this ArrowExpressionClauseSyntax arrowExpression,
            SyntaxToken semicolonToken,
            bool createReturnStatementForExpression,
            out BlockSyntax block)
        {
            if (!arrowExpression.TryConvertToStatement(semicolonToken, createReturnStatementForExpression, out var statement))
            {
                block = null;
                return false;
            }

            block = SyntaxFactory.Block(statement);
            return true;
        }

        public static bool TryConvertToStatement(
            this ArrowExpressionClauseSyntax arrowExpression,
            SyntaxToken semicolonToken,
            bool createReturnStatementForExpression,
            out StatementSyntax statement)
        {
            if (!arrowExpression.Expression.TryConvertToStatement(
                    semicolonToken, createReturnStatementForExpression, out statement))
            {
                return false;
            }

            if (arrowExpression.ArrowToken.TrailingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
            {
                statement = statement.WithPrependedLeadingTrivia(arrowExpression.ArrowToken.TrailingTrivia);
            }

            return true;
        }
    }
}
