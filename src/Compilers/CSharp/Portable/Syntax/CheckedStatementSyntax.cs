// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class CheckedStatementSyntax
    {
        public CheckedStatementSyntax Update(SyntaxToken keyword, BlockSyntax block)
            => Update(attributeLists: default, keyword, block);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static CheckedStatementSyntax CheckedStatement(SyntaxKind kind, SyntaxToken keyword, BlockSyntax block)
            => CheckedStatement(kind, attributeLists: default, keyword, block);
    }
}
