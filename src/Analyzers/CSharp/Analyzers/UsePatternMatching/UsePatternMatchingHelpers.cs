// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

internal static class UsePatternMatchingHelpers
{
    public static bool TryGetPartsOfAsAndMemberAccessCheck(
        BinaryExpressionSyntax asExpression,
        [NotNullWhen(true)] out ConditionalAccessExpressionSyntax? conditionalAccessExpression,
        out BinaryExpressionSyntax? binaryExpression,
        out IsPatternExpressionSyntax? isPatternExpression,
        out LanguageVersion requiredLanguageVersion)
    {
        conditionalAccessExpression = null;
        binaryExpression = null;
        isPatternExpression = null;
        requiredLanguageVersion = LanguageVersion.CSharp8;

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
            {
                // Extended property patterns are only in 10 and up.
                requiredLanguageVersion = LanguageVersion.CSharp10;
                whenNotNull = memberAccess.Expression;
            }

            // Ensure we have `.X`
            if (whenNotNull is not MemberBindingExpressionSyntax)
                return false;

            if (conditionalAccessExpression.Parent is BinaryExpressionSyntax(SyntaxKind.EqualsExpression) parentBinaryExpression1 &&
                parentBinaryExpression1.Left == conditionalAccessExpression)
            {
                // `(expr as T)?... == other_expr
                //
                // Can convert if other_expr is a constant (checked by caller).
                binaryExpression = parentBinaryExpression1;
                return true;
            }
            else if (conditionalAccessExpression.Parent is
                    BinaryExpressionSyntax(
                        SyntaxKind.NotEqualsExpression or
                        SyntaxKind.GreaterThanExpression or
                        SyntaxKind.GreaterThanOrEqualExpression or
                        SyntaxKind.LessThanExpression or
                        SyntaxKind.LessThanOrEqualExpression) parentBinaryExpression2 &&
                parentBinaryExpression2.Left == conditionalAccessExpression)
            {
                // `(expr as T)?... != other_expr
                //
                // Can convert if other_expr is a constant (checked by caller).
                binaryExpression = parentBinaryExpression2;

                // relational patterns need c# 9 or above.
                requiredLanguageVersion = (LanguageVersion)Math.Max((int)requiredLanguageVersion, (int)LanguageVersion.CSharp9);
                return true;
            }
            else if (conditionalAccessExpression.Parent is IsPatternExpressionSyntax parentIsPatternExpression)
            {
                // `(expr as T)?... is pattern`
                //
                //  We can convert this to a pattern in most cases except for:
                //
                // `(expr as T)?... is not X y`.  As it is not legal to transform into `expr is T { Member: not X y }`.
                // Specifically, `not variable` patterns are not legal except at the top level of a pattern.
                if (parentIsPatternExpression.Pattern is UnaryPatternSyntax
                    {
                        Pattern: DeclarationPatternSyntax or VarPatternSyntax or RecursivePatternSyntax { Designation: not null }
                    })
                {
                    return false;
                }

                isPatternExpression = parentIsPatternExpression;
                return true;
            }
        }

        return false;
    }
}
