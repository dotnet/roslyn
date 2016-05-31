// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class IndexerDeclarationSyntax
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
        public IndexerDeclarationSyntax WithSemicolon(SyntaxToken semicolon)
        {
            return this.WithSemicolonToken(semicolon);
        }

        public IndexerDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken thisKeyword, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken)
        {
            return Update(attributeLists, modifiers, this.RefKeyword, type, explicitInterfaceSpecifier, thisKeyword, parameterList, accessorList, expressionBody, semicolonToken);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static IndexerDeclarationSyntax IndexerDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            BracketedParameterListSyntax parameterList,
            AccessorListSyntax accessorList)
        {
            return SyntaxFactory.IndexerDeclaration(
                attributeLists: attributeLists,
                modifiers: modifiers,
                type: type,
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                parameterList: parameterList,
                accessorList: accessorList,
                expressionBody: default(ArrowExpressionClauseSyntax));
        }

        public static IndexerDeclarationSyntax IndexerDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists, 
            SyntaxTokenList modifiers, 
            TypeSyntax type, 
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            SyntaxToken thisKeyword, 
            BracketedParameterListSyntax parameterList, 
            AccessorListSyntax accessorList, 
            ArrowExpressionClauseSyntax expressionBody, 
            SyntaxToken semicolonToken)
        {
            return SyntaxFactory.IndexerDeclaration(
                attributeLists: attributeLists,
                modifiers: modifiers,
                refKeyword: default(SyntaxToken),
                type: type,
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                thisKeyword: thisKeyword,
                parameterList: parameterList,
                accessorList: accessorList,
                expressionBody: expressionBody,
                semicolonToken: semicolonToken);
        }
    }
}