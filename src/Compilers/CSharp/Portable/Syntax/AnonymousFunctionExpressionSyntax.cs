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
        public CSharpSyntaxNode Body => Block ?? (CSharpSyntaxNode)ExpressionBody!;

        public AnonymousFunctionExpressionSyntax WithBody(CSharpSyntaxNode body)
            => body is BlockSyntax block
                ? WithBlock(block).WithExpressionBody(null)
                : WithExpressionBody((ExpressionSyntax)body).WithBlock(null);

        public abstract SyntaxToken AsyncKeyword { get; }

        public AnonymousFunctionExpressionSyntax WithAsyncKeyword(SyntaxToken asyncKeyword)
            => WithAsyncKeywordCore(asyncKeyword);

        internal abstract AnonymousFunctionExpressionSyntax WithAsyncKeywordCore(SyntaxToken asyncKeyword);

        /// <summary>
        /// If the given <paramref name="asyncKeyword"/> is default, remove async if exists.
        /// Otherwise, replace the existing <see cref="AsyncKeyword"/> or add a new one.
        /// </summary>
        private protected SyntaxTokenList UpdateAsyncKeyword(SyntaxToken asyncKeyword)
        {
            var existingAsync = AsyncKeyword;

            // remove async keyword (if exists).
            if (asyncKeyword == default)
            {
                if (existingAsync != default)
                {
                    return Modifiers.Remove(existingAsync);
                }

                return Modifiers;
            }

            // add or replace.
            if (existingAsync == default)
            {
                return Modifiers.Add(asyncKeyword);
            }

            return Modifiers.Replace(existingAsync, asyncKeyword);
        }
    }
}
