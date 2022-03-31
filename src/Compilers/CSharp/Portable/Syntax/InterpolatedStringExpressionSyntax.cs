// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken)
            => InterpolatedStringExpression(stringStartToken, Token(SyntaxKind.InterpolatedStringEndToken));

        public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken, SyntaxList<InterpolatedStringContentSyntax> contents)
            => InterpolatedStringExpression(stringStartToken, contents, Token(SyntaxKind.InterpolatedStringEndToken));
    }
}
