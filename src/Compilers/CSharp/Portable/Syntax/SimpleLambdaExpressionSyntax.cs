// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class SimpleLambdaExpressionSyntax
    {
        public new SimpleLambdaExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block).WithExpressionBody(null)
                : WithExpressionBody((ExpressionSyntax)body).WithBlock(null);

        public SimpleLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? Update(asyncKeyword, parameter, arrowToken, block, null)
                : Update(asyncKeyword, parameter, arrowToken, null, (ExpressionSyntax)body);
    }

    public partial class LambdaExpressionSyntax
    {
        public new LambdaExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block).WithExpressionBody(null)
                : WithExpressionBody((ExpressionSyntax)body).WithBlock(null);
    }
}
