// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private MemberDeclarationSyntax GeneratePropertyOrIndexerDeclaration(IPropertySymbol symbol)
        {
            if (symbol.IsIndexer)
                return GenerateIndexerDeclaration(symbol);

            return PropertyDeclaration(
                GenerateAttributeLists(symbol.GetAttributes()),
                GenerateModifiers(symbol),
                symbol.Type.GenerateTypeSyntax(),
                GenerateExplicitInterfaceSpecification(symbol.ExplicitInterfaceImplementations),
                Identifier(symbol.Name),
                GeneratePropertyAccessorList(symbol));
        }

        private IndexerDeclarationSyntax GenerateIndexerDeclaration(IPropertySymbol symbol)
        {
            return IndexerDeclaration(
                GenerateAttributeLists(symbol.GetAttributes()),
                GenerateModifiers(symbol),
                symbol.Type.GenerateTypeSyntax(),
                GenerateExplicitInterfaceSpecification(symbol.ExplicitInterfaceImplementations),
                GenerateBracketedParameterList(symbol.Parameters),
                GeneratePropertyAccessorList(symbol));
        }

        private AccessorListSyntax GeneratePropertyAccessorList(IPropertySymbol symbol)
        {
            using var _ = GetArrayBuilder<AccessorDeclarationSyntax>(out var accessors);

            accessors.AddIfNotNull(GenerateAccessorDeclaration(SyntaxKind.GetAccessorDeclaration, symbol.GetMethod));
            accessors.AddIfNotNull(GenerateAccessorDeclaration(SyntaxKind.SetAccessorDeclaration, symbol.SetMethod));

            return AccessorList(List(accessors));
        }
    }
}
