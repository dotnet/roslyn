// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ThrowStatementSyntax
    {
        public ThrowStatementSyntax Update(SyntaxToken throwKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => Update(attributeLists: default, throwKeyword, expression, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ThrowStatementSyntax ThrowStatement(SyntaxToken throwKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => ThrowStatement(attributeLists: default, throwKeyword, expression, semicolonToken);
    }
}

