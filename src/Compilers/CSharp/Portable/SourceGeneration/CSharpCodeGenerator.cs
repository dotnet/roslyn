// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal static class CSharpCodeGenerator
    {
        public static string GenerateString(this ISymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
            => symbol.GenerateSyntax(indentation, eol, elasticTrivia).ToFullString();

        public static SyntaxNode GenerateSyntax(this ISymbol symbol, string indentation = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation, string eol = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL, bool elasticTrivia = false)
        {
            return GenerateSyntaxWorker(symbol).NormalizeWhitespace<SyntaxNode>(indentation, eol, elasticTrivia);
        }

        private static SyntaxNode GenerateSyntaxWorker(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    break;
                case SymbolKind.ArrayType:
                    break;
                case SymbolKind.Assembly:
                    break;
                case SymbolKind.DynamicType:
                    break;
                case SymbolKind.ErrorType:
                    break;
                case SymbolKind.Event:
                    break;
                case SymbolKind.Field:
                    break;
                case SymbolKind.Label:
                    return GenerateLabel((ILabelSymbol)symbol);
                case SymbolKind.Local:
                    break;
                case SymbolKind.Method:
                    break;
                case SymbolKind.NetModule:
                    break;
                case SymbolKind.NamedType:
                    break;
                case SymbolKind.Namespace:
                    return GenerateNamespace((INamespaceSymbol)symbol);
                case SymbolKind.Parameter:
                    break;
                case SymbolKind.PointerType:
                    break;
                case SymbolKind.Property:
                    break;
                case SymbolKind.RangeVariable:
                    break;
                case SymbolKind.TypeParameter:
                    break;
                case SymbolKind.Preprocessing:
                    break;
                case SymbolKind.Discard:
                    break;
                default:
                    break;
            }

            throw new NotImplementedException();
        }

        private static SyntaxNode GenerateNamespace(INamespaceSymbol symbol)
        {
            var usings = GenerateUsings(CodeGenerator.GetImports(symbol));
            var members = GenerateMembers(symbol.GetMembers());

            if (symbol.IsGlobalNamespace)
                return CompilationUnit(externs: default, usings, attributeLists: default, members);

            return NamespaceDeclaration(GenerateName(symbol.Name), externs: default, usings, members);
        }

        private static SyntaxList<MemberDeclarationSyntax> GenerateMembers(IEnumerable<INamespaceOrTypeSymbol> members)
        {
            var builder = ArrayBuilder<MemberDeclarationSyntax>.GetInstance();

            foreach (var member in members)
                builder.Add(GenerateMember(member));

            return List(builder.ToImmutableAndFree());
        }

        private static MemberDeclarationSyntax GenerateMember(INamespaceOrTypeSymbol member)
            => (MemberDeclarationSyntax)GenerateSyntaxWorker(member);

        private static SyntaxList<UsingDirectiveSyntax> GenerateUsings(
            ImmutableArray<INamespaceOrTypeSymbol> imports)
        {
            var builder = ArrayBuilder<UsingDirectiveSyntax>.GetInstance();

            foreach (var import in imports)
            {
                if (import is INamespaceSymbol nsSymbol)
                    builder.Add(UsingDirective(GenerateName(nsSymbol.Name)));
                else if (import is ITypeSymbol typeSymbol)
                    builder.Add(UsingDirective(Token(SyntaxKind.StaticKeyword), alias: null, GenerateName(typeSymbol)));
            }

            return List(builder.ToImmutableAndFree());
        }

        private static NameSyntax GenerateName(ITypeSymbol typeSymbol)
        {
            throw new NotImplementedException();
        }

        private static NameSyntax GenerateName(string name)
            => ParseName(name);

        private static SyntaxNode GenerateLabel(ILabelSymbol symbol)
        {
            throw new NotImplementedException();
        }
    }
}
