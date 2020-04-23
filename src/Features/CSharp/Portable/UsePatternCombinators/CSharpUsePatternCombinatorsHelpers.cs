// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators
{
    internal static class CSharpUsePatternCombinatorsHelpers
    {
        public static SyntaxKind[] SyntaxKinds => new[]
        {
            SyntaxKind.ForStatement,
            SyntaxKind.EqualsValueClause,
            SyntaxKind.IfStatement,
            SyntaxKind.WhenClause,
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement,
            SyntaxKind.ReturnStatement,
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.ArrowExpressionClause,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.Argument,
        };

        public static ExpressionSyntax? GetExpression(SyntaxNode node)
        {
            return node switch
            {
                ForStatementSyntax n => n.Condition,
                EqualsValueClauseSyntax n => n.Value,
                IfStatementSyntax n => n.Condition,
                WhenClauseSyntax n => n.Condition,
                WhileStatementSyntax n => n.Condition,
                DoStatementSyntax n => n.Condition,
                ReturnStatementSyntax n => n.Expression,
                YieldStatementSyntax n => n.Expression,
                ArrowExpressionClauseSyntax n => n.Expression,
                AssignmentExpressionSyntax n => n.Right,
                LambdaExpressionSyntax n => n.ExpressionBody,
                ArgumentSyntax { RefKindKeyword: { RawKind: 0 } } n => n.Expression,
                _ => null,
            };
        }
    }
}

