// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class AnonymousFunctionExpressionSyntax
    {
        /// <summary>
        /// Either the <see cref="Block"/> if it is not <c>null</c> or the
        /// <see cref="ExpressionBody"/> otherwise.
        /// </summary>
        public CSharpSyntaxNode Body => Block ?? (CSharpSyntaxNode)ExpressionBody;

        public AnonymousFunctionExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block).WithExpressionBody(null)
                : WithExpressionBody((ExpressionSyntax)body).WithBlock(null);

        public abstract SyntaxToken AsyncKeyword { get; }

        public AnonymousFunctionExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword)
            => WithAsyncKeywordCore(asyncKeyword);

        internal abstract AnonymousFunctionExpressionSyntax WithAsyncKeywordCore(SyntaxToken asyncKeyword);
    }
}
