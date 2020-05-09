// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        public TypeSyntax GenerateTypeSyntax(INamespaceOrTypeSymbol symbol, bool onlyNames)
        {
            if (symbol is INamespaceSymbol nsSymbol)
                return GenerateNameSyntax(nsSymbol);
            else if (symbol is ITypeSymbol type)
                return GenerateTypeSyntax(type, onlyNames);

            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }



        private SyntaxList<MemberDeclarationSyntax> GenerateMemberDeclarations(ImmutableArray<ISymbol> members)
        {
            using var _ = GetArrayBuilder<MemberDeclarationSyntax>(out var builder);

            foreach (var member in members)
            {
                if (member.IsImplicitlyDeclared)
                    continue;

                // skip accessors
                if (IsAnyAccessor(member))
                    continue;

                builder.Add(GenerateMemberDeclaration(member));
            }

            return List(builder);
        }

        private MemberDeclarationSyntax GenerateMemberDeclaration(ISymbol member)
            => (MemberDeclarationSyntax)Generate(member);

        private static SyntaxList<UsingDirectiveSyntax> GenerateUsingDirectives(
            ImmutableArray<INamespaceOrTypeSymbol> imports)
        {
            using var _ = GetArrayBuilder<UsingDirectiveSyntax>(out var builder);

            foreach (var import in imports)
            {
                if (import is INamespaceSymbol nsSymbol)
                    builder.Add(UsingDirective(ParseName(nsSymbol.Name)));
                else if (import is ITypeSymbol typeSymbol)
                    builder.Add(UsingDirective(Token(SyntaxKind.StaticKeyword), alias: null, typeSymbol.GenerateNameSyntax()));
            }

            return List(builder);
        }
    }
}
