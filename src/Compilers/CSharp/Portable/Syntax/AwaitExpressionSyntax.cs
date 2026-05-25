// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class AwaitExpressionSyntax
    {
        public AwaitExpressionSyntax Update(SyntaxToken awaitKeyword, ExpressionSyntax expression)
            => Update(awaitKeyword, questionToken: default, expression);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static Syntax.AwaitExpressionSyntax AwaitExpression(SyntaxToken awaitKeyword, Syntax.ExpressionSyntax expression)
            => AwaitExpression(awaitKeyword, questionToken: default, expression);
    }
}
