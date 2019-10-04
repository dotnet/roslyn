// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class BreakStatementSyntax
    {
        public BreakStatementSyntax Update(SyntaxToken breakKeyword, SyntaxToken semicolonToken)
            => Update(attributeLists: default, breakKeyword, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static BreakStatementSyntax BreakStatement(SyntaxToken breakKeyword, SyntaxToken semicolonToken)
            => BreakStatement(attributeLists: default, breakKeyword, semicolonToken);
    }
}
