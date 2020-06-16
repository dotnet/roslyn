// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
