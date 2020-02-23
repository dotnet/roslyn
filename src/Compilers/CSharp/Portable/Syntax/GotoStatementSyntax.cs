// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class GotoStatementSyntax
    {
        public GotoStatementSyntax Update(SyntaxToken gotoKeyword, SyntaxToken caseOrDefaultKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => Update(attributeLists: default, gotoKeyword, caseOrDefaultKeyword, expression, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static GotoStatementSyntax GotoStatement(SyntaxKind kind, SyntaxToken caseOrDefaultKeyword, ExpressionSyntax expression)
            => GotoStatement(kind, attributeLists: default, caseOrDefaultKeyword, expression);

        public static GotoStatementSyntax GotoStatement(SyntaxKind kind, SyntaxToken gotoKeyword, SyntaxToken caseOrDefaultKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => GotoStatement(kind, attributeLists: default, gotoKeyword, caseOrDefaultKeyword, expression, semicolonToken);
    }
}
