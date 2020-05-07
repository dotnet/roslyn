// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        public static NameSyntax GenerateNameSyntax(this INamespaceOrTypeSymbol symbol)
        {
            throw new NotImplementedException();
        }

        public static TypeSyntax GenerateTypeSyntax(this INamespaceOrTypeSymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    return GenerateType((INamedTypeSymbol)symbol);
                case SymbolKind.ArrayType:
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.PointerType:
                case SymbolKind.TypeParameter:
                    break;
                case SymbolKind.Namespace:
                    return GenerateNameSyntax((INamespaceSymbol)symbol);
                default:
                    break;
            }

            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }

        private static SyntaxList<MemberDeclarationSyntax> GenerateMemberDeclarations(IEnumerable<INamespaceOrTypeSymbol> members)
        {
            using var _ = GetArrayBuilder<MemberDeclarationSyntax>(out var builder);

            foreach (var member in members)
                builder.Add(GenerateMemberDeclaration(member));

            return List(builder);
        }

        private static MemberDeclarationSyntax GenerateMemberDeclaration(INamespaceOrTypeSymbol member)
            => (MemberDeclarationSyntax)GenerateSyntax(member);

        private static SyntaxList<UsingDirectiveSyntax> GenerateUsingDirectives(
            ImmutableArray<INamespaceOrTypeSymbol> imports)
        {
            using var _ = GetArrayBuilder<UsingDirectiveSyntax>(out var builder);

            foreach (var import in imports)
            {
                if (import is INamespaceSymbol nsSymbol)
                    builder.Add(UsingDirective(ParseName(nsSymbol.Name)));
                else if (import is ITypeSymbol typeSymbol)
                    builder.Add(UsingDirective(Token(SyntaxKind.StaticKeyword), alias: null, GenerateNameSyntax(typeSymbol)));
            }

            return List(builder);
        }
    }
}
