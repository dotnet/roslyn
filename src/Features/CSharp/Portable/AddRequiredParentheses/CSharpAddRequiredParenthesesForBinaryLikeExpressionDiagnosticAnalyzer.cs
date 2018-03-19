// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal class CSharpAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer :
        AbstractAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kinds = ImmutableArray.Create(
            SyntaxKind.AddExpression,
            SyntaxKind.SubtractExpression,
            SyntaxKind.MultiplyExpression,
            SyntaxKind.DivideExpression,
            SyntaxKind.ModuloExpression,
            SyntaxKind.LeftShiftExpression,
            SyntaxKind.RightShiftExpression,
            SyntaxKind.LogicalOrExpression,
            SyntaxKind.LogicalAndExpression,
            SyntaxKind.BitwiseOrExpression,
            SyntaxKind.BitwiseAndExpression,
            SyntaxKind.ExclusiveOrExpression,
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.LessThanExpression,
            SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.IsExpression,
            SyntaxKind.AsExpression,
            SyntaxKind.CoalesceExpression,
            SyntaxKind.IsPatternExpression,
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.AddAssignmentExpression,
            SyntaxKind.SubtractAssignmentExpression,
            SyntaxKind.MultiplyAssignmentExpression,
            SyntaxKind.DivideAssignmentExpression,
            SyntaxKind.ModuloAssignmentExpression,
            SyntaxKind.AndAssignmentExpression,
            SyntaxKind.ExclusiveOrAssignmentExpression,
            SyntaxKind.OrAssignmentExpression,
            SyntaxKind.LeftShiftAssignmentExpression,
            SyntaxKind.RightShiftAssignmentExpression);

        protected override ImmutableArray<SyntaxKind> GetSyntaxNodeKinds()
            => s_kinds;

        protected override int GetPrecedence(SyntaxNode binaryLike)
            => (int)((ExpressionSyntax)binaryLike).GetOperatorPrecedence();

        protected override PrecedenceKind GetPrecedenceKind(SyntaxNode binaryLike)
            => CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer.GetPrecedenceKind((ExpressionSyntax)binaryLike);

        protected override bool IsBinaryLike(SyntaxNode node)
            => node is BinaryExpressionSyntax ||
               node is AssignmentExpressionSyntax ||
               node is IsPatternExpressionSyntax isPattern && isPattern.Pattern is ConstantPatternSyntax;

        protected override void GetPartsOfBinaryLike(
            SyntaxNode binaryLikeOpt,
            out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
        {
            Debug.Assert(IsBinaryLike(binaryLikeOpt));
            switch (binaryLikeOpt)
            {
            case BinaryExpressionSyntax binaryExpression:
                left = binaryExpression.Left;
                operatorToken = binaryExpression.OperatorToken;
                right = binaryExpression.Right;
                return;

            case AssignmentExpressionSyntax assignmentExpression:
                left = assignmentExpression.Left;
                operatorToken = assignmentExpression.OperatorToken;
                right = assignmentExpression.Right;
                return;

            case IsPatternExpressionSyntax isPatternExpression:
                left = isPatternExpression.Expression;
                operatorToken = isPatternExpression.IsKeyword;
                right = ((ConstantPatternSyntax)isPatternExpression.Pattern).Expression;
                return;

            default:
                throw ExceptionUtilities.UnexpectedValue(binaryLikeOpt);
            }
        }

        protected override SyntaxNode GetParentExpressionOrAssignment(SyntaxNode binaryLike)
            => binaryLike.Parent is ConstantPatternSyntax
                ? binaryLike.Parent.Parent as ExpressionSyntax
                : binaryLike.Parent as ExpressionSyntax;
    }
}
