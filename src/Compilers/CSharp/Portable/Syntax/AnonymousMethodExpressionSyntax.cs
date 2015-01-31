// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class AnonymousMethodExpressionSyntax
    {
        public BlockSyntax Block => (BlockSyntax)this.Body;

        public AnonymousMethodExpressionSyntax WithBlock(BlockSyntax block)
        {
            return this.Update(this.AsyncKeyword, this.DelegateKeyword, this.ParameterList, block);
        }

        public AnonymousMethodExpressionSyntax AddBlockStatements(params StatementSyntax[] items)
        {
            return this.WithBlock(this.Block.WithStatements(this.Block.Statements.AddRange(items)));
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new AnonymousMethodExpressionSyntax instance.</summary>
        public static AnonymousMethodExpressionSyntax AnonymousMethodExpression()
        {
            return SyntaxFactory.AnonymousMethodExpression(default(SyntaxToken), SyntaxFactory.Token(SyntaxKind.DelegateKeyword), default(ParameterListSyntax), SyntaxFactory.Block());
        }
    }
}
