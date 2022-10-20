// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

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
        /// If the given <paramref name="asyncKeyword"/> is default, remove all async keywords, if any.
        /// Otherwise, replace the existing <see cref="AsyncKeyword"/> (the first one) or add a new one.
        /// </summary>
        private protected SyntaxTokenList UpdateAsyncKeyword(SyntaxToken asyncKeyword)
        {
            // Remove *all* async keywords if any, i.e, after this call, AsyncKeyword property should return 'default'.
            if (asyncKeyword == default)
            {
                if (Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    return new SyntaxTokenList(Modifiers.Where(m => !m.IsKind(SyntaxKind.AsyncKeyword)));
                }

                return Modifiers;
            }

            // add or replace.
            var existingAsync = AsyncKeyword;
            if (existingAsync == default)
            {
                return Modifiers.Add(asyncKeyword);
            }

            return Modifiers.Replace(existingAsync, asyncKeyword);
        }
    }
}
