// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class UnsafeStatementSyntax
    {
        public UnsafeStatementSyntax Update(SyntaxToken unsafeKeyword, BlockSyntax block)
            => Update(AttributeLists, unsafeKeyword, block);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static UnsafeStatementSyntax UnsafeStatement(SyntaxToken unsafeKeyword, BlockSyntax block)
            => UnsafeStatement(attributeLists: default, unsafeKeyword, block);
    }
}
