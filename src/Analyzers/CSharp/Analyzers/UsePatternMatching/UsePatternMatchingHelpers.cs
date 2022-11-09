// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    internal static class UsePatternMatchingHelpers
    {
        public static bool TryGetPartsOfAsAndMemberAccessCheck(
            BinaryExpressionSyntax asExpression,
            [NotNullWhen(true)] out ConditionalAccessExpressionSyntax? conditionalAccessExpression,
            out BinaryExpressionSyntax? binaryExpression,
            out IsPatternExpressionSyntax? isPatternExpression)
        {
            conditionalAccessExpression = null;
            binaryExpression = null;
            isPatternExpression = null;

            if (asExpression.Kind() == SyntaxKind.AsExpression)
            {
                // has to be `(expr as T)`
                if (asExpression.Parent is not ParenthesizedExpressionSyntax
                    {
                        // Has to be `(expr as T)?...`
                        Parent: ConditionalAccessExpressionSyntax parentConditionalAccess
                    })
                {
                    return false;
                }

                conditionalAccessExpression = parentConditionalAccess;

                // After the `?` has to be `.X.Y.Z`

                var whenNotNull = parentConditionalAccess.WhenNotNull;
                while (whenNotNull is MemberAccessExpressionSyntax memberAccess)
                    whenNotNull = memberAccess.Expression;

                // Ensure we have `.X`
                if (whenNotNull is not MemberBindingExpressionSyntax)
                    return false;

                if (conditionalAccessExpression.Parent is
                        BinaryExpressionSyntax(
                            SyntaxKind.EqualsExpression or
                            SyntaxKind.NotEqualsExpression or
                            SyntaxKind.GreaterThanExpression or
                            SyntaxKind.GreaterThanOrEqualExpression or
                            SyntaxKind.LessThanExpression or
                            SyntaxKind.LessThanOrEqualExpression) parentBinaryExpression &&
                    parentBinaryExpression.Left == conditionalAccessExpression)
                {
                    // `(expr as T)?... == other_expr
                    //
                    // Can convert if other_expr is a constant (checked by caller).
                    binaryExpression = parentBinaryExpression;
                    return true;
                }
                else if (conditionalAccessExpression.Parent is IsPatternExpressionSyntax parentIsPatternExpression)
                {
                    // `(expr as T)?... is pattern
                    //
                    //  In this case we can always convert to a pattern.
                    isPatternExpression = parentIsPatternExpression;
                    return true;
                }
            }

            return false;
        }
    }
}
