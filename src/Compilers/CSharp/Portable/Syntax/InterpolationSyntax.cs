// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static InterpolationSyntax Interpolation(ExpressionSyntax expression)
            => Interpolation(Token(SyntaxKind.OpenBraceToken), expression, Token(SyntaxKind.CloseBraceToken));

        public static InterpolationSyntax Interpolation(ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause)
            => Interpolation(Token(SyntaxKind.OpenBraceToken), expression, alignmentClause, formatClause, Token(SyntaxKind.CloseBraceToken));
    }
}
