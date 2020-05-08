// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private EnumDeclarationSyntax GenerateEnumDeclaration(INamedTypeSymbol symbol)
        {
            return EnumDeclaration(
                GenerateAttributeLists(symbol.GetAttributes()),
                GenerateModifiers(symbol),
                Identifier(symbol.Name),
                GenerateBaseList(symbol.BaseType, symbol.Interfaces),
                GenerateEnumMemberDeclarations(symbol.GetMembers()));
        }

        private static SeparatedSyntaxList<EnumMemberDeclarationSyntax> GenerateEnumMemberDeclarations(
            ImmutableArray<ISymbol> symbols)
        {
            using var _ = GetArrayBuilder<EnumMemberDeclarationSyntax>(out var members);

            foreach (var symbol in symbols)
            {
                if (!symbol.IsImplicitlyDeclared && symbol is IFieldSymbol field)
                    members.Add(GenerateEnumMemberDeclaration(field));
            }

            return SeparatedList(members);
        }

        private static EnumMemberDeclarationSyntax GenerateEnumMemberDeclaration(IFieldSymbol field)
        {
            var expression = GenerateConstantExpression(field.Type, field.HasConstantValue, field.ConstantValue);
            var equalsValue = expression == null ? null : EqualsValueClause(expression);

            return EnumMemberDeclaration(
                GenerateAttributeLists(field.GetAttributes()),
                modifiers: default,
                Identifier(field.Name),
                equalsValue);
        }
    }
}
