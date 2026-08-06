// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseNullConditionalAwait;

internal static class UseNullConditionalAwaitHelpers
{
    /// <summary>
    /// Matches a non-null check and returns the checked operand: <c>a != null</c>, <c>null != a</c>,
    /// or <c>a is not null</c>. Used by the if-statement form, where the awaited expression only runs
    /// when the operand is known to be non-null.
    /// </summary>
    public static bool TryGetNotNullCheckOperand(
        ExpressionSyntax condition, [NotNullWhen(true)] out ExpressionSyntax? operand)
        => TryGetNullCheckOperand(condition, out operand, out var isEquals) && !isEquals;

    /// <summary>
    /// Matches a null or non-null check and returns the checked operand along with which sense the
    /// check has. <paramref name="isEquals"/> is <see langword="true"/> for <c>a == null</c> /
    /// <c>a is null</c> and <see langword="false"/> for <c>a != null</c> / <c>a is not null</c>.
    /// Leading <c>!</c> flips the sense; parentheses are seen through on both the condition and the
    /// operand.
    /// </summary>
    public static bool TryGetNullCheckOperand(
        ExpressionSyntax condition, [NotNullWhen(true)] out ExpressionSyntax? operand, out bool isEquals)
    {
        operand = null;
        isEquals = false;

        condition = condition.WalkDownParentheses();

        // `!(...)` flips the sense of the inner check.
        if (condition is PrefixUnaryExpressionSyntax(SyntaxKind.LogicalNotExpression) logicalNot)
        {
            if (!TryGetNullCheckOperand(logicalNot.Operand, out operand, out isEquals))
                return false;

            isEquals = !isEquals;
            return true;
        }

        switch (condition)
        {
            case BinaryExpressionSyntax(SyntaxKind.EqualsExpression) equals:
                isEquals = true;
                operand = GetNullComparisonOperand(equals);
                break;

            case BinaryExpressionSyntax(SyntaxKind.NotEqualsExpression) notEquals:
                isEquals = false;
                operand = GetNullComparisonOperand(notEquals);
                break;

            // `a is null` / `a is not null`.
            case IsPatternExpressionSyntax { Pattern: var pattern } isPattern when TryGetConstantNullPatternSense(pattern, out isEquals):
                operand = isPattern.Expression.WalkDownParentheses();
                break;
        }

        return operand != null;
    }

    private static ExpressionSyntax? GetNullComparisonOperand(BinaryExpressionSyntax binary)
    {
        var left = binary.Left.WalkDownParentheses();
        var right = binary.Right.WalkDownParentheses();

        if (right.IsKind(SyntaxKind.NullLiteralExpression))
            return left;

        if (left.IsKind(SyntaxKind.NullLiteralExpression))
            return right;

        return null;
    }

    /// <summary>
    /// Recognizes the <c>null</c> / <c>not null</c> patterns (and <c>not not null</c>, etc.), returning
    /// whether the pattern tests for equality with null.
    /// </summary>
    private static bool TryGetConstantNullPatternSense(PatternSyntax pattern, out bool isEquals)
    {
        isEquals = true;

        while (pattern is UnaryPatternSyntax(SyntaxKind.NotPattern) notPattern)
        {
            isEquals = !isEquals;
            pattern = notPattern.Pattern;
        }

        return pattern is ConstantPatternSyntax constant &&
            constant.Expression.IsKind(SyntaxKind.NullLiteralExpression);
    }

    /// <summary>
    /// Given the null-checked <paramref name="conditionOperand"/> (<c>a</c>) and the operand of an
    /// <c>await</c> expression (<paramref name="awaitOperand"/>, <c>E</c>), returns the node within
    /// <c>E</c> that <c>a</c> is the receiver of, i.e. the spot the <c>?.</c> attaches to when forming
    /// <c>await? E'</c>. For the bare case (<c>await a</c>) the whole operand is returned (no <c>?.</c>
    /// is needed; <c>await?</c> itself does the null test). Returns <see langword="null"/> when <c>E</c>
    /// is not rooted at <c>a</c> or the conversion would be invalid.
    /// </summary>
    public static ExpressionSyntax? GetReceiverMatch(
        SemanticModel semanticModel, ExpressionSyntax conditionOperand, ExpressionSyntax awaitOperand, CancellationToken cancellationToken)
    {
        awaitOperand = awaitOperand.WalkDownParentheses();

        // Bare receiver: `await a` becomes `await? a`.
        if (AreEquivalent(awaitOperand, conditionOperand))
            return awaitOperand;

        // Otherwise walk down the access chain looking for `a` as the receiver of a member/element access.
        var current = awaitOperand;
        while (true)
        {
            var unwrapped = Unwrap(current);
            if (unwrapped is null)
                return null;

            if (current is MemberAccessExpressionSyntax or ElementAccessExpressionSyntax &&
                AreEquivalent(unwrapped, conditionOperand))
            {
                // `a.StaticMember` is really `Type.StaticMember` and can't become `a?.StaticMember`.
                if (current is MemberAccessExpressionSyntax memberAccess &&
                    semanticModel.GetSymbolInfo(memberAccess, cancellationToken).GetAnySymbol() is { IsStatic: true })
                {
                    return null;
                }

                return unwrapped;
            }

            current = unwrapped;
        }
    }

    private static ExpressionSyntax? Unwrap(ExpressionSyntax node)
        => node.WalkDownParentheses() switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
            ConditionalAccessExpressionSyntax conditionalAccess => conditionalAccess.Expression,
            ElementAccessExpressionSyntax elementAccess => elementAccess.Expression,
            _ => null,
        };

    private static bool AreEquivalent(ExpressionSyntax left, ExpressionSyntax right)
        => SyntaxFactory.AreEquivalent(left, right, topLevel: false);
}
