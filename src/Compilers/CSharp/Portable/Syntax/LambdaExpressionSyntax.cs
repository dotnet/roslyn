// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
#pragma warning disable RS0041 // uses oblivious reference types

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LambdaExpressionSyntax
    {
        public new LambdaExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block).WithExpressionBody(null)
                : WithExpressionBody((ExpressionSyntax)body).WithBlock(null);

        public new LambdaExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword)
            => (LambdaExpressionSyntax)WithAsyncKeywordCore(asyncKeyword);
    }
}
