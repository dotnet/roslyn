// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ContinueStatementSyntax
    {
        public ContinueStatementSyntax Update(SyntaxToken continueKeyword, SyntaxToken semicolonToken)
            => Update(attributeLists: default, continueKeyword, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ContinueStatementSyntax ContinueStatement(SyntaxToken continueKeyword, SyntaxToken semicolonToken)
            => ContinueStatement(attributeLists: default, continueKeyword, semicolonToken);
    }
}
