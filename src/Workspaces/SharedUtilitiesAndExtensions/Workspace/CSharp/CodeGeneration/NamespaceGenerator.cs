// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class NamespaceGenerator
    {
        public static BaseNamespaceDeclarationSyntax AddNamespaceTo(
            ICodeGenerationService service,
            BaseNamespaceDeclarationSyntax destination,
            INamespaceSymbol @namespace,
            CSharpCodeGenerationContextInfo info,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateNamespaceDeclaration(
                service, @namespace,
                CodeGenerationDestination.Namespace,
                info,
                cancellationToken);
            if (declaration is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
                throw new ArgumentException(WorkspaceExtensionsResources.Namespace_can_not_be_added_in_this_destination);

            var members = Insert(destination.Members, namespaceDeclaration, info, availableIndices);
            return destination.WithMembers(members);
        }

        public static CompilationUnitSyntax AddNamespaceTo(
            ICodeGenerationService service,
            CompilationUnitSyntax destination,
            INamespaceSymbol @namespace,
            CSharpCodeGenerationContextInfo info,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateNamespaceDeclaration(
                service,
                @namespace,
                CodeGenerationDestination.CompilationUnit,
                info,
                cancellationToken);

            if (declaration is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
                throw new ArgumentException(WorkspaceExtensionsResources.Namespace_can_not_be_added_in_this_destination);

            var members = Insert(destination.Members, namespaceDeclaration, info, availableIndices);
            return destination.WithMembers(members);
        }

        internal static SyntaxNode GenerateNamespaceDeclaration(
            ICodeGenerationService service,
            INamespaceSymbol @namespace,
            CodeGenerationDestination destination,
            CSharpCodeGenerationContextInfo info,
            CancellationToken cancellationToken)
        {
            GetNameAndInnermostNamespace(@namespace, info, out var name, out var innermostNamespace);

            var declaration = GetDeclarationSyntaxWithoutMembers(
                @namespace, innermostNamespace, name, destination, info);

            declaration = info.Context.GenerateMembers
                ? service.AddMembers(declaration, innermostNamespace.GetMembers(), info, cancellationToken)
                : declaration;

            return AddFormatterAndCodeGeneratorAnnotationsTo(declaration);
        }

        public static SyntaxNode UpdateCompilationUnitOrNamespaceDeclaration(
            ICodeGenerationService service,
            SyntaxNode declaration,
            IList<ISymbol> newMembers,
            CSharpCodeGenerationContextInfo info,
            CancellationToken cancellationToken)
        {
            declaration = RemoveAllMembers(declaration);
            declaration = service.AddMembers(declaration, newMembers, info, cancellationToken);
            return AddFormatterAndCodeGeneratorAnnotationsTo(declaration);
        }

        private static SyntaxNode GenerateNamespaceDeclarationWorker(
            string name, INamespaceSymbol innermostNamespace,
            CodeGenerationDestination destination,
            CSharpCodeGenerationContextInfo info)
        {
            var usings = GenerateUsingDirectives(innermostNamespace);

            // If they're just generating the empty namespace then make that into compilation unit.
            if (name == string.Empty)
                return SyntaxFactory.CompilationUnit().WithUsings(usings);

            if (destination == CodeGenerationDestination.CompilationUnit &&
                info.Options.NamespaceDeclarations.Value == NamespaceDeclarationPreference.FileScoped &&
                info.LanguageVersion >= LanguageVersion.CSharp10)
            {
                return SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(name)).WithUsings(usings);
            }

            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(name)).WithUsings(usings);
        }

        private static SyntaxNode GetDeclarationSyntaxWithoutMembers(
            INamespaceSymbol @namespace,
            INamespaceSymbol innermostNamespace,
            string name,
            CodeGenerationDestination destination,
            CSharpCodeGenerationContextInfo info)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<SyntaxNode>(@namespace, info);
            return reusableSyntax == null
                ? GenerateNamespaceDeclarationWorker(name, innermostNamespace, destination, info)
                : RemoveAllMembers(reusableSyntax);
        }

        private static SyntaxNode RemoveAllMembers(SyntaxNode declaration)
            => declaration switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.WithMembers(default),
                BaseNamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.WithMembers(default),
                _ => declaration,
            };

        private static SyntaxList<UsingDirectiveSyntax> GenerateUsingDirectives(INamespaceSymbol innermostNamespace)
        {
            var usingDirectives =
                CodeGenerationNamespaceInfo.GetImports(innermostNamespace)
                                           .Select(GenerateUsingDirective)
                                           .WhereNotNull()
                                           .ToList();

            return usingDirectives.ToSyntaxList();
        }

        private static UsingDirectiveSyntax? GenerateUsingDirective(ISymbol symbol)
        {
            if (symbol is IAliasSymbol alias)
            {
                var name = GenerateName(alias.Target);
                if (name != null)
                {
                    return SyntaxFactory.UsingDirective(
                        SyntaxFactory.NameEquals(alias.Name.ToIdentifierName()),
                        name);
                }
            }
            else if (symbol is INamespaceOrTypeSymbol namespaceOrType)
            {
                var name = GenerateName(namespaceOrType);
                if (name != null)
                {
                    return SyntaxFactory.UsingDirective(name);
                }
            }

            return null;
        }

        private static NameSyntax GenerateName(INamespaceOrTypeSymbol symbol)
        {
            return symbol is ITypeSymbol type
                ? type.GenerateNameSyntax()
                : SyntaxFactory.ParseName(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }
    }
}
