// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class StackAllocArrayCreationExpressionSyntax
    {
        public StackAllocArrayCreationExpressionSyntax Update(SyntaxToken stackAllocKeyword, TypeSyntax type)
            => Update(StackAllocKeyword, type, default(InitializerExpressionSyntax));
    }

    public partial class LocalDeclarationStatementSyntax
    {
        public LocalDeclarationStatementSyntax Update(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => Update(default(SyntaxToken), default(SyntaxToken), modifiers, declaration, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static StackAllocArrayCreationExpressionSyntax StackAllocArrayCreationExpression(SyntaxToken stackAllocKeyword, TypeSyntax type)
            => StackAllocArrayCreationExpression(stackAllocKeyword, type, default(InitializerExpressionSyntax));

        public static LocalDeclarationStatementSyntax LocalDeclarationStatement(SyntaxTokenList modifiers, VariableDeclarationSyntax declaration, SyntaxToken semicolonToken)
            => LocalDeclarationStatement(default(SyntaxToken), default(SyntaxToken), modifiers, declaration, semicolonToken);
    }
}
