// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class SimpleLambdaExpressionSyntax
    {
        public SimpleLambdaExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block)
                : WithExpressionBody((ExpressionSyntax)body);

        public SimpleLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? Update(asyncKeyword, parameter, arrowToken, block, null)
                : Update(asyncKeyword, parameter, arrowToken, null, (ExpressionSyntax)body);
    }
}
