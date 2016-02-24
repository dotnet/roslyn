// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class PropertyDeclarationSyntax
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This member is obsolete.", true)]
        public SyntaxToken Semicolon
        {
            get
            {
                return this.SemicolonToken;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This member is obsolete.", true)]
        public PropertyDeclarationSyntax WithSemicolon(SyntaxToken semicolon)
        {
            return this.WithSemicolonToken(semicolon);
        }

        public PropertyDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer, SyntaxToken semicolonToken)
        {
            return Update(attributeLists, modifiers, this.RefKeyword, type, explicitInterfaceSpecifier, identifier, accessorList, expressionBody, initializer, semicolonToken);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static PropertyDeclarationSyntax PropertyDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer, SyntaxToken semicolonToken)
        {
            return PropertyDeclaration(
                attributeLists,
                modifiers,
                refKeyword: default(SyntaxToken),
                type: type,
                explicitInterfaceSpecifier: explicitInterfaceSpecifier, 
                identifier: identifier, 
                accessorList: accessorList, 
                expressionBody: expressionBody, 
                initializer: initializer, 
                semicolonToken: semicolonToken);
        }
    }
}

