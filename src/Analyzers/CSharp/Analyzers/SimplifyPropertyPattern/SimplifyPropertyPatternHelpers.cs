// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyPropertyPattern
{
    internal static class SimplifyPropertyPatternHelpers
    {
        public static bool IsSimplifiable(
            SubpatternSyntax subpattern,
            [NotNullWhen(true)] out SubpatternSyntax? innerSubpattern,
            [NotNullWhen(true)] out BaseExpressionColonSyntax? outerExpressionColon)
        {
            // can't simplify if parent pattern is not a property pattern 
            //
            // can't simplify if we have anything inside other than a property pattern clause.  i.e.
            // `a: { b: ... } x` is not simplifiable as we'll lose the `x` binding for the `a` property.
            //
            // can't simplify `a: { }` or `a: { b: ..., c: ... }`
            if (subpattern is
                {
                    Parent: PropertyPatternClauseSyntax,
                    ExpressionColon: { } outer,
                    Pattern: RecursivePatternSyntax
                    {
                        Type: null,
                        PositionalPatternClause: null,
                        Designation: null,
                        PropertyPatternClause.Subpatterns: { Count: 1 } subpatterns
                    }
                } &&
                subpatterns[0] is { ExpressionColon: { } inner } &&
                IsMergable(outer.Expression) &&
                IsMergable(inner.Expression))
            {
                innerSubpattern = subpatterns[0];
                outerExpressionColon = outer;
                return true;
            }

            innerSubpattern = null;
            outerExpressionColon = null;
            return false;
        }

        public static bool IsMergable([NotNullWhen(true)] ExpressionSyntax? expression)
        {
            if (expression is SimpleNameSyntax)
                return true;

            if (expression is MemberAccessExpressionSyntax memberAccessExpression && IsMergable(memberAccessExpression.Expression))
                return true;

            return false;
        }
    }
}
