// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ParenthesizedLambdaExpressionSyntax
    {
        public ParenthesizedLambdaExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block)
                : WithExpressionBody((ExpressionSyntax)body);

        public ParenthesizedLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? Update(asyncKeyword, parameterList, arrowToken, block, null)
                : Update(asyncKeyword, parameterList, arrowToken, null, (ExpressionSyntax)body);
    }
}
