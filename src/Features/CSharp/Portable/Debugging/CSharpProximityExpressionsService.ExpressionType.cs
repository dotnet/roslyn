// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.CSharp.Debugging
{
    internal partial class CSharpProximityExpressionsService
    {
        // Flags used for "collecting" terms for proximity expressions.  The flags are somewhat
        // confusing.  The key point to remember is that an expression will be placed in the result
        // set if and only if ValidTerm is set.  ValidExpression is used to indicate that while an
        // expression may not be a ValidTerm by itself, it can be used as a sub-expression of a
        // larger expression that IS a ValidTerm.  (Note: ValidTerm implies ValidExpression)
        //
        // For example, consider the expression a[b+c].  The analysis of this expression starts at
        // the ElementAccessExpression.  The rules for an ElementAccessExpression say that the
        // expression is a ValidTerm if and only if both the LHS('a' in this case) and the
        // RHS('b+c') are valid ValidExpressions. The LHS is a ValidTerm, and the RHS is a binary
        // operator-- this time AddExpression. The rules for AddExpression state that the expression
        // is never a ValidTerm, but is a ValidExpression if both the LHS and the RHS are
        // ValidExpressions. In this case, both 'b' and 'c' are ValidTerms (thus valid expressions),
        // so 'a+b' is a ValidExpression (but not a ValidTerm), and finally 'a[b+c]' is considered a
        // ValidTerm. So, the result of GetProximityExpressions for this expression would be:
        //
        // a
        //
        // b
        //
        // c
        //
        // a[b+c]
        //
        // (but not "b+c")
        [Flags]
        private enum ExpressionType
        {
            Invalid = 0x0,
            ValidExpression = 0x1,

            // Note: ValidTerm implies ValidExpression.
            ValidTerm = 0x3
        }
    }
}
