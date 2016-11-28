// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class LocalDeclarationStatementSyntax
    {
        public LocalDeclarationStatementSyntax Update(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
        {
            return Update(modifiers, this.RefKeyword, declaration, semicolonToken);
        }

        public LocalDeclarationStatementSyntax AddDeclarationVariables(params VariableDeclaratorSyntax[] items)
        {
            return this.WithDeclaration(this.Declaration.WithVariables(this.Declaration.Variables.AddRange(items)));
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new LocalDeclarationStatementSyntax instance.</summary>
        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
        {
            return LocalDeclarationStatement(modifiers, default(SyntaxToken), declaration, semicolonToken);
        }
    }
}