﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
