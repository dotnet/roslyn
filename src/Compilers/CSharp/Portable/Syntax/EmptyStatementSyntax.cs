// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class EmptyStatementSyntax
    {
        public EmptyStatementSyntax Update(SyntaxToken semicolonToken)
            => Update(attributeLists: default, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static EmptyStatementSyntax EmptyStatement(SyntaxToken semicolonToken)
            => EmptyStatement(attributeLists: default, semicolonToken);
    }
}
