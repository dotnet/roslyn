// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class UnsafeStatementSyntax
    {
        public UnsafeStatementSyntax Update(SyntaxToken unsafeKeyword, BlockSyntax block)
            => Update(attributeLists: default, unsafeKeyword, block);
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
