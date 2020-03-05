// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ReturnStatementSyntax
    {
        public ReturnStatementSyntax Update(SyntaxToken returnKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => Update(attributeLists: default, returnKeyword, expression, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ReturnStatementSyntax ReturnStatement(SyntaxToken returnKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => ReturnStatement(attributeLists: default, returnKeyword, expression, semicolonToken);
    }
}
