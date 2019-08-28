// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// This is the root class for all code model objects. It contains methods that
    /// are common to everything.
    /// </summary>
    public partial class AbstractCodeModelObject
    {
        private static CodeGenerationOptions GetCodeGenerationOptions(
            EnvDTE.vsCMAccess access, ParseOptions parseOptions)
        {
            var generateDefaultAccessibility = (access & EnvDTE.vsCMAccess.vsCMAccessDefault) == 0;
            return new CodeGenerationOptions(
                generateDefaultAccessibility: generateDefaultAccessibility,
                parseOptions: parseOptions);
        }

        protected SyntaxNode CreateConstructorDeclaration(SyntaxNode containerNode, string typeName, EnvDTE.vsCMAccess access)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newMethodSymbol = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default,
                accessibility: CodeModelService.GetAccessibility(access, SymbolKind.Method, destination),
                modifiers: new DeclarationModifiers(),
                typeName: typeName,
                parameters: default);

            return CodeGenerationService.CreateMethodDeclaration(
                newMethodSymbol, destination,
                options: GetCodeGenerationOptions(access, containerNode.SyntaxTree.Options));
        }

        protected SyntaxNode CreateDestructorDeclaration(SyntaxNode containerNode, string typeName)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newMethodSymbol = CodeGenerationSymbolFactory.CreateDestructorSymbol(
                attributes: default,
                typeName: typeName);

            return CodeGenerationService.CreateMethodDeclaration(
                newMethodSymbol, destination);
        }

        protected SyntaxNode CreateDelegateTypeDeclaration(SyntaxNode containerNode, string name, EnvDTE.vsCMAccess access, INamedTypeSymbol returnType)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newTypeSymbol = CodeGenerationSymbolFactory.CreateDelegateTypeSymbol(
                attributes: default,
                accessibility: CodeModelService.GetAccessibility(access, SymbolKind.NamedType, destination),
                modifiers: new DeclarationModifiers(),
                returnType: returnType,
                refKind: RefKind.None,
                name: name);

            return CodeGenerationService.CreateNamedTypeDeclaration(
                newTypeSymbol, destination,
                options: GetCodeGenerationOptions(access, containerNode.SyntaxTree.Options));
        }

        protected SyntaxNode CreateEventDeclaration(SyntaxNode containerNode, string name, EnvDTE.vsCMAccess access, ITypeSymbol type, bool createPropertyStyleEvent)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            IMethodSymbol addMethod = null;
            IMethodSymbol removeMethod = null;

            if (createPropertyStyleEvent)
            {
                addMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: default,
                    accessibility: Accessibility.NotApplicable,
                    modifiers: new DeclarationModifiers(),
                    returnType: null,
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: "add_" + name,
                    typeParameters: default,
                    parameters: default);

                removeMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: default,
                    accessibility: Accessibility.NotApplicable,
                    modifiers: new DeclarationModifiers(),
                    returnType: null,
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: "remove_" + name,
                    typeParameters: default,
                    parameters: default);
            }

            var newEventSymbol = CodeGenerationSymbolFactory.CreateEventSymbol(
                attributes: default,
                accessibility: CodeModelService.GetAccessibility(access, SymbolKind.Event, destination),
                modifiers: new DeclarationModifiers(),
                type: type,
                explicitInterfaceImplementations: default,
                name: name,
                addMethod: addMethod,
                removeMethod: removeMethod);

            return CodeGenerationService.CreateEventDeclaration(
                newEventSymbol, destination,
                options: GetCodeGenerationOptions(access, containerNode.SyntaxTree.Options));
        }

        protected SyntaxNode CreateFieldDeclaration(SyntaxNode containerNode, string name, EnvDTE.vsCMAccess access, ITypeSymbol type)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newFieldSymbol = CodeGenerationSymbolFactory.CreateFieldSymbol(
                attributes: default,
                accessibility: CodeModelService.GetAccessibility(access, SymbolKind.Field, destination),
                modifiers: new DeclarationModifiers(isWithEvents: CodeModelService.GetWithEvents(access)),
                type: type,
                name: name);

            return CodeGenerationService.CreateFieldDeclaration(
                newFieldSymbol, destination,
                options: GetCodeGenerationOptions(access, containerNode.SyntaxTree.Options));
        }

        protected SyntaxNode CreateMethodDeclaration(SyntaxNode containerNode, string name, EnvDTE.vsCMAccess access, ITypeSymbol returnType)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newMethodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                accessibility: CodeModelService.GetAccessibility(access, SymbolKind.Method, destination),
                modifiers: new DeclarationModifiers(),
                returnType: returnType,
                refKind: RefKind.None,
                explicitInterfaceImplementations: default,
                name: name,
                typeParameters: default,
                parameters: default);

            var codeGenerationOptions = GetCodeGenerationOptions(access, containerNode.SyntaxTree.Options);
            if (destination == CodeGenerationDestination.InterfaceType)
            {
                // Generating method with body is allowed when targeting an interface,
                // so we have to explicitly disable it here.
                codeGenerationOptions = codeGenerationOptions.With(generateMethodBodies: false);
            }

            return CodeGenerationService.CreateMethodDeclaration(
                newMethodSymbol, destination,
                options: codeGenerationOptions);
        }

        protected SyntaxNode CreatePropertyDeclaration(SyntaxNode containerNode, string name, bool generateGetter, bool generateSetter, EnvDTE.vsCMAccess access, ITypeSymbol type)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            IMethodSymbol getMethod = null;
            if (generateGetter)
            {
                getMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: default,
                    accessibility: Accessibility.NotApplicable,
                    modifiers: new DeclarationModifiers(),
                    returnType: null,
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: "get_" + name,
                    typeParameters: default,
                    parameters: default,
                    statements: ImmutableArray.Create(CodeModelService.CreateReturnDefaultValueStatement(type)));
            }

            IMethodSymbol setMethod = null;
            if (generateSetter)
            {
                setMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: default,
                    accessibility: Accessibility.NotApplicable,
                    modifiers: new DeclarationModifiers(),
                    returnType: null,
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: "set_" + name,
                    typeParameters: default,
                    parameters: default);
            }

            var newPropertySymbol = CodeGenerationSymbolFactory.CreatePropertySymbol(
                attributes: default,
                accessibility: CodeModelService.GetAccessibility(access, SymbolKind.Field, destination),
                modifiers: new DeclarationModifiers(),
                type: type,
                refKind: RefKind.None,
                explicitInterfaceImplementations: default,
                name: name,
                parameters: default,
                getMethod: getMethod,
                setMethod: setMethod);

            return CodeGenerationService.CreatePropertyDeclaration(
                newPropertySymbol, destination,
                options: GetCodeGenerationOptions(access, containerNode.SyntaxTree.Options));
        }

        protected SyntaxNode CreateNamespaceDeclaration(SyntaxNode containerNode, string name)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newNamespaceSymbol = CodeGenerationSymbolFactory.CreateNamespaceSymbol(name);

            var newNamespace = CodeGenerationService.CreateNamespaceDeclaration(
                newNamespaceSymbol, destination);

            return newNamespace;
        }

        protected SyntaxNode CreateTypeDeclaration(
            SyntaxNode containerNode,
            TypeKind typeKind,
            string name,
            EnvDTE.vsCMAccess access,
            INamedTypeSymbol baseType = null,
            ImmutableArray<INamedTypeSymbol> implementedInterfaces = default)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newTypeSymbol = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default,
                accessibility: CodeModelService.GetAccessibility(access, SymbolKind.NamedType, destination),
                modifiers: new DeclarationModifiers(),
                typeKind: typeKind,
                name: name,
                typeParameters: default,
                baseType: baseType,
                interfaces: implementedInterfaces,
                specialType: SpecialType.None,
                members: default);

            return CodeGenerationService.CreateNamedTypeDeclaration(
                newTypeSymbol, destination,
                options: GetCodeGenerationOptions(access, containerNode.SyntaxTree.Options));
        }
    }
}
