// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        public override SyntaxToken AsyncKeyword
            => this.Modifiers.FirstOrDefault(SyntaxKind.AsyncKeyword);

        internal override AnonymousFunctionExpressionSyntax WithAsyncKeywordCore(SyntaxToken asyncKeyword)
            => WithAsyncKeyword(asyncKeyword);

        public new SimpleLambdaExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword)
            => this.Update(asyncKeyword, this.Parameter, this.ArrowToken, this.Block, this.ExpressionBody);

        public SimpleLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, BlockSyntax block, ExpressionSyntax expressionBody)
            => Update(SyntaxFactory.TokenList(asyncKeyword), parameter, arrowToken, block, expressionBody);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, BlockSyntax block, ExpressionSyntax expressionBody)
            => SimpleLambdaExpression(TokenList(asyncKeyword), parameter, arrowToken, block, expressionBody);

        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(ParameterSyntax parameter, BlockSyntax block, ExpressionSyntax expressionBody)
            => SimpleLambdaExpression(default(SyntaxTokenList), parameter, block, expressionBody);
    }
}
