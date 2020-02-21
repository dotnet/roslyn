﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal class CSharpAddRequiredParenthesesDiagnosticAnalyzer :
        AbstractAddRequiredParenthesesDiagnosticAnalyzer<
            ExpressionSyntax, ExpressionSyntax, SyntaxKind>
    {
        public CSharpAddRequiredParenthesesDiagnosticAnalyzer()
            : base(CSharpPrecedenceService.Instance)
        {
        }

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
            SyntaxKind.IsPatternExpression);

        protected override ImmutableArray<SyntaxKind> GetSyntaxNodeKinds()
            => s_kinds;

        protected override int GetPrecedence(ExpressionSyntax binaryLike)
            => (int)binaryLike.GetOperatorPrecedence();

        protected override bool IsBinaryLike(ExpressionSyntax node)
            => node is BinaryExpressionSyntax ||
               node is IsPatternExpressionSyntax isPattern && isPattern.Pattern is ConstantPatternSyntax;

        protected override (ExpressionSyntax, SyntaxToken, ExpressionSyntax) GetPartsOfBinaryLike(ExpressionSyntax binaryLike)
        {
            Debug.Assert(IsBinaryLike(binaryLike));
            switch (binaryLike)
            {
                case BinaryExpressionSyntax binaryExpression:
                    return (binaryExpression.Left, binaryExpression.OperatorToken, binaryExpression.Right);

                case IsPatternExpressionSyntax isPatternExpression:
                    return (isPatternExpression.Expression, isPatternExpression.IsKeyword, ((ConstantPatternSyntax)isPatternExpression.Pattern).Expression);

                default:
                    throw ExceptionUtilities.UnexpectedValue(binaryLike);
            }
        }

        protected override ExpressionSyntax? TryGetParentExpression(ExpressionSyntax binaryLike)
            => binaryLike.Parent is ConstantPatternSyntax
                ? binaryLike.Parent.Parent as ExpressionSyntax
                : binaryLike.Parent as ExpressionSyntax;
    }
}
