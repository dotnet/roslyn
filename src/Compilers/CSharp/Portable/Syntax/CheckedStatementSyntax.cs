// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
