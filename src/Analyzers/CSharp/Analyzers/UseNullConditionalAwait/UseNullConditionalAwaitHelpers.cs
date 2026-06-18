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
    /// Matches a non-null check and returns the checked operand: <c>a != null</c> or <c>null != a</c>.
    /// </summary>
    public static bool TryGetNotNullCheckOperand(
        ExpressionSyntax condition, [NotNullWhen(true)] out ExpressionSyntax? operand)
    {
        operand = null;

        if (condition.WalkDownParentheses() is BinaryExpressionSyntax(SyntaxKind.NotEqualsExpression) binary)
        {
            if (binary.Right.IsKind(SyntaxKind.NullLiteralExpression))
                operand = binary.Left.WalkDownParentheses();
            else if (binary.Left.IsKind(SyntaxKind.NullLiteralExpression))
                operand = binary.Right.WalkDownParentheses();
        }

        return operand != null;
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
