// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class AwaitExpressionSyntax
    {
        public AwaitExpressionSyntax Update(SyntaxToken awaitKeyword, ExpressionSyntax expression)
            => Update(awaitKeyword, this.QuestionToken, expression);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static AwaitExpressionSyntax AwaitExpression(SyntaxToken awaitKeyword, ExpressionSyntax expression)
            => AwaitExpression(awaitKeyword, default, expression);
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class ContextAwareSyntax
    {
        public AwaitExpressionSyntax AwaitExpression(SyntaxToken awaitKeyword, ExpressionSyntax expression)
            => AwaitExpression(awaitKeyword, null, expression);
    }
}

