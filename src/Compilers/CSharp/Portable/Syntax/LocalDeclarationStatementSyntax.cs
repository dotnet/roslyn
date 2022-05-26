// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LocalDeclarationStatementSyntax
    {
        public LocalDeclarationStatementSyntax Update(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => Update(AwaitKeyword, UsingKeyword, modifiers, declaration, semicolonToken);

        public LocalDeclarationStatementSyntax Update(SyntaxToken awaitKeyword, SyntaxToken usingKeyword, SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => Update(AttributeLists, awaitKeyword, usingKeyword, modifiers, declaration, semicolonToken);
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
