// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
