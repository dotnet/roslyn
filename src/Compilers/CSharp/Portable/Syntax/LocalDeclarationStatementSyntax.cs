// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LocalDeclarationStatementSyntax
    {
        public LocalDeclarationStatementSyntax Update(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => Update(awaitKeyword: default, usingKeyword: default, modifiers, declaration, semicolonToken);

        public LocalDeclarationStatementSyntax Update(SyntaxToken awaitKeyword, SyntaxToken usingKeyword, SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => Update(attributeLists: default, awaitKeyword, usingKeyword, modifiers, declaration, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => LocalDeclarationStatement(awaitKeyword: default, usingKeyword: default, modifiers, declaration, semicolonToken);

        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(SyntaxToken awaitKeyword, SyntaxToken usingKeyword, SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => LocalDeclarationStatement(attributeLists: default, awaitKeyword, usingKeyword, modifiers, declaration, semicolonToken);

        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration)
            => LocalDeclarationStatement(attributeLists: default, modifiers, declaration);
    }
}
