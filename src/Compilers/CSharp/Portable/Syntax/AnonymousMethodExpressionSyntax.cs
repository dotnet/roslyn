// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
#pragma warning disable RS0041 // uses oblivious reference types

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class AnonymousMethodExpressionSyntax
    {
        public new AnonymousMethodExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block).WithExpressionBody(null)
                : WithExpressionBody((ExpressionSyntax)body).WithBlock(null);

        public AnonymousMethodExpressionSyntax Update(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? Update(asyncKeyword, delegateKeyword, parameterList, block, null)
                : Update(asyncKeyword, delegateKeyword, parameterList, null, (ExpressionSyntax)body);

        public override SyntaxToken AsyncKeyword
            => this.Modifiers.FirstOrDefault(SyntaxKind.AsyncKeyword);

        internal override AnonymousFunctionExpressionSyntax WithAsyncKeywordCore(SyntaxToken asyncKeyword) => WithAsyncKeyword(asyncKeyword);
        public new AnonymousMethodExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword)
            => this.Update(asyncKeyword, this.DelegateKeyword, this.ParameterList, this.Block, this.ExpressionBody);

        public AnonymousMethodExpressionSyntax Update(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, BlockSyntax block, ExpressionSyntax expressionBody)
            => Update(UpdateAsyncKeyword(asyncKeyword), delegateKeyword, parameterList, block, expressionBody);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new AnonymousMethodExpressionSyntax instance.</summary>
        public static AnonymousMethodExpressionSyntax AnonymousMethodExpression()
            => AnonymousMethodExpression(
                asyncKeyword: default,
                Token(SyntaxKind.DelegateKeyword),
                parameterList: null,
                Block(),
                expressionBody: null);

        public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, BlockSyntax block, ExpressionSyntax expressionBody)
            => AnonymousMethodExpression(TokenList(asyncKeyword), delegateKeyword, parameterList, block, expressionBody);
    }
}
