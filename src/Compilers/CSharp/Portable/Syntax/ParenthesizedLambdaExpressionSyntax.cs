// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ParenthesizedLambdaExpressionSyntax
    {
        public new ParenthesizedLambdaExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block).WithExpressionBody(null)
                : WithExpressionBody((ExpressionSyntax)body).WithBlock(null);

        public ParenthesizedLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? Update(asyncKeyword, parameterList, arrowToken, block, null)
                : Update(asyncKeyword, parameterList, arrowToken, null, (ExpressionSyntax)body);

        public override SyntaxToken AsyncKeyword
            => this.Modifiers.FirstOrDefault(SyntaxKind.AsyncKeyword);

        internal override AnonymousFunctionExpressionSyntax WithAsyncKeywordCore(SyntaxToken asyncKeyword)
            => WithAsyncKeyword(asyncKeyword);

        public new ParenthesizedLambdaExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword)
            => this.Update(asyncKeyword, this.ParameterList, this.ArrowToken, this.Block, this.ExpressionBody);

        public ParenthesizedLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, BlockSyntax block, ExpressionSyntax expressionBody)
            => Update(SyntaxFactory.TokenList(asyncKeyword), parameterList, arrowToken, block, expressionBody);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, BlockSyntax block, ExpressionSyntax expressionBody)
            => ParenthesizedLambdaExpression(TokenList(asyncKeyword), parameterList, arrowToken, block, expressionBody);
    }
}
