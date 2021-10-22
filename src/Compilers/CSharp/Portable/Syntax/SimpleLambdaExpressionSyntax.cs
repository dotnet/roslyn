// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public SimpleLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => Update(UpdateAsyncKeyword(asyncKeyword), parameter, arrowToken, block, expressionBody);

        public SimpleLambdaExpressionSyntax Update(SyntaxTokenList modifiers, ParameterSyntax parameter, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => Update(this.AttributeLists, modifiers, parameter, arrowToken, block, expressionBody);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(SyntaxToken asyncKeyword, ParameterSyntax parameter, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => SimpleLambdaExpression(attributeLists: default, TokenList(asyncKeyword), parameter, arrowToken, block, expressionBody);

        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(ParameterSyntax parameter, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => SimpleLambdaExpression(attributeLists: default, default(SyntaxTokenList), parameter, block, expressionBody);

        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(SyntaxTokenList modifiers, ParameterSyntax parameter, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => SimpleLambdaExpression(attributeLists: default, modifiers, parameter, arrowToken, block, expressionBody);

        public static SimpleLambdaExpressionSyntax SimpleLambdaExpression(SyntaxTokenList modifiers, ParameterSyntax parameter, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => SimpleLambdaExpression(attributeLists: default, modifiers, parameter, block, expressionBody);
    }
}
