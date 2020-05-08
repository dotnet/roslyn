// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static SyntaxNode GenerateCompilationUnitOrNamespaceDeclaration(INamespaceSymbol symbol)
        {
            var usings = GenerateUsingDirectives(CodeGenerator.GetImports(symbol));
            var members = GenerateMemberDeclarations(symbol.GetMembers());

            if (symbol.IsGlobalNamespace)
                return CompilationUnit(externs: default, usings, attributeLists: default, members);

            return NamespaceDeclaration(ParseName(symbol.Name), externs: default, usings, members);
        }

        private static NameSyntax GenerateNameSyntax(INamespaceSymbol symbol)
        {
            var nameSyntax = IdentifierName(symbol.Name);
            if (symbol.ContainingNamespace == null)
                return nameSyntax;

            if (symbol.ContainingNamespace.IsGlobalNamespace)
                return AliasQualifiedName(SyntaxFacts.GetText(SyntaxKind.GlobalKeyword), nameSyntax);

            var containingNamespace = symbol.ContainingNamespace.GenerateNameSyntax();
            return QualifiedName(containingNamespace, nameSyntax);
        }
    }
}
