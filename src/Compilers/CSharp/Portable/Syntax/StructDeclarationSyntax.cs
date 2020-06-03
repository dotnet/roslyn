// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static StructDeclarationSyntax StructDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            BaseListSyntax baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxList<MemberDeclarationSyntax> members)
            => SyntaxFactory.StructDeclaration(
                attributeLists,
                modifiers,
                SyntaxFactory.Token(SyntaxKind.StructKeyword),
                identifier,
                typeParameterList,
                baseList,
                constraintClauses,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                members,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default);

        /// <summary>Creates a new StructDeclarationSyntax instance.</summary>
        public static StructDeclarationSyntax StructDeclaration(SyntaxToken identifier)
            => SyntaxFactory.StructDeclaration(default, default(SyntaxTokenList), SyntaxFactory.Token(SyntaxKind.StructKeyword), identifier, null, null, default, SyntaxFactory.Token(SyntaxKind.CloseBraceToken), default, SyntaxFactory.Token(SyntaxKind.CloseBraceToken), default);

        /// <summary>Creates a new StructDeclarationSyntax instance.</summary>
        public static StructDeclarationSyntax StructDeclaration(string identifier)
            => SyntaxFactory.StructDeclaration(default, default(SyntaxTokenList), SyntaxFactory.Token(SyntaxKind.StructKeyword), SyntaxFactory.Identifier(identifier), null, null, default, default, default, default, default);
    }
}
