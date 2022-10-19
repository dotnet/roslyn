// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class TryStatementSyntax
    {
        public TryStatementSyntax Update(SyntaxToken tryKeyword, BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax @finally)
            => Update(AttributeLists, tryKeyword, block, catches, @finally);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static TryStatementSyntax TryStatement(BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax? @finally)
            => TryStatement(attributeLists: default, block, catches, @finally);

        public static TryStatementSyntax TryStatement(SyntaxToken tryKeyword, BlockSyntax block, SyntaxList<CatchClauseSyntax> catches, FinallyClauseSyntax? @finally)
            => TryStatement(attributeLists: default, tryKeyword, block, catches, @finally);
    }
}
