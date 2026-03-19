// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

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
            statement = statement.WithPrependedLeadingTrivia(arrowExpression.ArrowToken.TrailingTrivia);

        if (arrowExpression.ArrowToken.LeadingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
            statement = statement.WithPrependedLeadingTrivia(arrowExpression.ArrowToken.LeadingTrivia);

        return true;
    }
}
