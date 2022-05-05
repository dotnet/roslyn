// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public ParenthesizedLambdaExpressionSyntax Update(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => Update(UpdateAsyncKeyword(asyncKeyword), parameterList, arrowToken, block, expressionBody);

        public ParenthesizedLambdaExpressionSyntax Update(SyntaxTokenList modifiers, ParameterListSyntax parameterList, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => Update(this.AttributeLists, modifiers, parameterList, arrowToken, block, expressionBody);

        public ParenthesizedLambdaExpressionSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, ParameterListSyntax parameterList, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => Update(attributeLists, modifiers, returnType: null, parameterList, arrowToken, block, expressionBody);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(SyntaxToken asyncKeyword, ParameterListSyntax parameterList, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => ParenthesizedLambdaExpression(TokenList(asyncKeyword), parameterList, arrowToken, block, expressionBody);

        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(ParameterListSyntax parameterList, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => ParenthesizedLambdaExpression(default(SyntaxTokenList), parameterList, block, expressionBody);

        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(SyntaxTokenList modifiers, ParameterListSyntax parameterList, SyntaxToken arrowToken, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => SyntaxFactory.ParenthesizedLambdaExpression(attributeLists: default, modifiers, returnType: null, parameterList, arrowToken, block, expressionBody);

        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(SyntaxTokenList modifiers, ParameterListSyntax parameterList, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => SyntaxFactory.ParenthesizedLambdaExpression(attributeLists: default, modifiers, parameterList, block, expressionBody);

        public static ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, ParameterListSyntax parameterList, BlockSyntax? block, ExpressionSyntax? expressionBody)
            => SyntaxFactory.ParenthesizedLambdaExpression(attributeLists, modifiers, returnType: null, parameterList, block, expressionBody);
    }
}
