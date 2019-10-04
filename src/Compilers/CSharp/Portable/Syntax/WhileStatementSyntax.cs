// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class WhileStatementSyntax
    {
        public WhileStatementSyntax Update(SyntaxToken whileKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(attributeLists: default, whileKeyword, openParenToken, condition, closeParenToken, statement);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static WhileStatementSyntax WhileStatement(SyntaxToken whileKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement)
            => WhileStatement(attributeLists: default, whileKeyword, openParenToken, condition, closeParenToken, statement);
    }
}
