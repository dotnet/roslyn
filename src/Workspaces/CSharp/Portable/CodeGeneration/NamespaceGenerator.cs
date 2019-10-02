// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class NamespaceGenerator
    {
        public static NamespaceDeclarationSyntax AddNamespaceTo(
            ICodeGenerationService service,
            NamespaceDeclarationSyntax destination,
            INamespaceSymbol @namespace,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateNamespaceDeclaration(service, @namespace, options, cancellationToken);
            if (!(declaration is NamespaceDeclarationSyntax))
            {
                throw new ArgumentException(CSharpWorkspaceResources.Namespace_can_not_be_added_in_this_destination);
            }

            var members = Insert(destination.Members, (NamespaceDeclarationSyntax)declaration, options, availableIndices);
            return destination.WithMembers(members);
        }

        public static CompilationUnitSyntax AddNamespaceTo(
            ICodeGenerationService service,
            CompilationUnitSyntax destination,
            INamespaceSymbol @namespace,
            CodeGenerationOptions options,
            IList<bool> availableIndices,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateNamespaceDeclaration(service, @namespace, options, cancellationToken);
            if (!(declaration is NamespaceDeclarationSyntax))
            {
                throw new ArgumentException(CSharpWorkspaceResources.Namespace_can_not_be_added_in_this_destination);
            }

            var members = Insert(destination.Members, (NamespaceDeclarationSyntax)declaration, options, availableIndices);
            return destination.WithMembers(members);
        }

        internal static SyntaxNode GenerateNamespaceDeclaration(
            ICodeGenerationService service,
            INamespaceSymbol @namespace,
            CodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            options ??= CodeGenerationOptions.Default;
            GetNameAndInnermostNamespace(@namespace, options, out var name, out var innermostNamespace);

            var declaration = GetDeclarationSyntaxWithoutMembers(@namespace, innermostNamespace, name, options);

            declaration = options.GenerateMembers
                    ? service.AddMembers(declaration, innermostNamespace.GetMembers(), options, cancellationToken)
                    : declaration;

            return AddFormatterAndCodeGeneratorAnnotationsTo(declaration);
        }

        public static SyntaxNode UpdateCompilationUnitOrNamespaceDeclaration(
            ICodeGenerationService service,
            SyntaxNode declaration,
            IList<ISymbol> newMembers,
            CodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            declaration = RemoveAllMembers(declaration);
            declaration = service.AddMembers(declaration, newMembers, options, cancellationToken);
            return AddFormatterAndCodeGeneratorAnnotationsTo(declaration);
        }

        private static SyntaxNode GenerateNamespaceDeclarationWorker(
            string name, INamespaceSymbol innermostNamespace)
        {
            var usings = GenerateUsingDirectives(innermostNamespace);

            // If they're just generating the empty namespace then make that into compilation unit.
            if (name == string.Empty)
            {
                return SyntaxFactory.CompilationUnit().WithUsings(usings);
            }

            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(name)).WithUsings(usings);
        }

        private static SyntaxNode GetDeclarationSyntaxWithoutMembers(
            INamespaceSymbol @namespace,
            INamespaceSymbol innermostNamespace,
            string name,
            CodeGenerationOptions options)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<SyntaxNode>(@namespace, options);
            if (reusableSyntax == null)
            {
                return GenerateNamespaceDeclarationWorker(name, innermostNamespace);
            }

            return RemoveAllMembers(reusableSyntax);
        }

        private static SyntaxNode RemoveAllMembers(SyntaxNode declaration)
            => declaration.Kind() switch
            {
                SyntaxKind.CompilationUnit => ((CompilationUnitSyntax)declaration).WithMembers(default),
                SyntaxKind.NamespaceDeclaration => ((NamespaceDeclarationSyntax)declaration).WithMembers(default),
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

        private static UsingDirectiveSyntax GenerateUsingDirective(ISymbol symbol)
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
            if (symbol is ITypeSymbol type)
            {
                return type.GenerateTypeSyntax() as NameSyntax;
            }
            else
            {
                return SyntaxFactory.ParseName(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }
    }
}
