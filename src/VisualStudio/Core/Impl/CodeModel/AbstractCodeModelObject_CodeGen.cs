// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// This is the root class for all code model objects. It contains methods that
    /// are common to everything.
    /// </summary>
    public partial class AbstractCodeModelObject
    {
        private CodeGenerationContextInfo GetCodeGenerationContextInfo(
            SyntaxNode containerNode,
            OptionSet? options = null,
            EnvDTE.vsCMAccess access = EnvDTE.vsCMAccess.vsCMAccessDefault,
            bool generateMethodBodies = true)
        {
            var generateDefaultAccessibility = (access & EnvDTE.vsCMAccess.vsCMAccessDefault) == 0;
            var globalOptions = State.ProjectCodeModelFactory.GlobalOptions;

            var languageServices = Workspace.Services.GetLanguageServices(containerNode.SyntaxTree.Options.Language);
            var fallbackOptions = globalOptions.GetCodeGenerationOptions(languageServices);
            var codeGenOptions = CodeGenerationOptions.Create(options ?? Workspace.Options, fallbackOptions, languageServices);

            return codeGenOptions.GetInfo(
                new CodeGenerationContext(
                    generateDefaultAccessibility: generateDefaultAccessibility,
                    generateMethodBodies: generateMethodBodies),
                containerNode.SyntaxTree.Options);
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

            var codeGenOptions = GetCodeGenerationContextInfo(containerNode, access: access);
            var method = CodeGenerationService.CreateMethodDeclaration(newMethodSymbol, destination, codeGenOptions, CancellationToken.None);
            Contract.ThrowIfNull(method);
            return method;
        }

        protected SyntaxNode CreateDestructorDeclaration(SyntaxNode containerNode, string typeName)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            var newMethodSymbol = CodeGenerationSymbolFactory.CreateDestructorSymbol(
                attributes: default,
                typeName: typeName);

            var options = GetCodeGenerationContextInfo(containerNode);
            var method = CodeGenerationService.CreateMethodDeclaration(newMethodSymbol, destination, options, CancellationToken.None);
            Contract.ThrowIfNull(method);
            return method;
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

            var codeGenOptions = GetCodeGenerationContextInfo(containerNode, access: access);

            return CodeGenerationService.CreateNamedTypeDeclaration(newTypeSymbol, destination, codeGenOptions, CancellationToken.None);
        }

        protected SyntaxNode CreateEventDeclaration(SyntaxNode containerNode, string name, EnvDTE.vsCMAccess access, ITypeSymbol type, bool createPropertyStyleEvent)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            IMethodSymbol? addMethod = null;
            IMethodSymbol? removeMethod = null;

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

            var codeGenOptions = GetCodeGenerationContextInfo(containerNode, access: access);
            return CodeGenerationService.CreateEventDeclaration(newEventSymbol, destination, codeGenOptions, CancellationToken.None);
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

            var codeGenOptions = GetCodeGenerationContextInfo(containerNode, access: access);
            return CodeGenerationService.CreateFieldDeclaration(newFieldSymbol, destination, codeGenOptions, CancellationToken.None);
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

            // Generating method with body is allowed when targeting an interface,
            // so we have to explicitly disable it here.
            var codeGenerationOptions = GetCodeGenerationContextInfo(
                containerNode,
                access: access,
                generateMethodBodies: destination != CodeGenerationDestination.InterfaceType);

            var method = CodeGenerationService.CreateMethodDeclaration(newMethodSymbol, destination, codeGenerationOptions, CancellationToken.None);
            Contract.ThrowIfNull(method);
            return method;
        }

        protected SyntaxNode CreatePropertyDeclaration(
            SyntaxNode containerNode,
            string name,
            bool generateGetter,
            bool generateSetter,
            EnvDTE.vsCMAccess access,
            ITypeSymbol type,
            OptionSet options)
        {
            var destination = CodeModelService.GetDestination(containerNode);

            IMethodSymbol? getMethod = null;
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

            IMethodSymbol? setMethod = null;
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

            var codeGenOptions = GetCodeGenerationContextInfo(containerNode, options, access);
            return CodeGenerationService.CreatePropertyDeclaration(newPropertySymbol, destination, codeGenOptions, CancellationToken.None);
        }

        protected SyntaxNode CreateNamespaceDeclaration(SyntaxNode containerNode, string name)
        {
            var destination = CodeModelService.GetDestination(containerNode);
            var newNamespaceSymbol = CodeGenerationSymbolFactory.CreateNamespaceSymbol(name);

            var options = GetCodeGenerationContextInfo(containerNode);
            return CodeGenerationService.CreateNamespaceDeclaration(newNamespaceSymbol, destination, options, CancellationToken.None);
        }

        protected SyntaxNode CreateTypeDeclaration(
            SyntaxNode containerNode,
            TypeKind typeKind,
            string name,
            EnvDTE.vsCMAccess access,
            INamedTypeSymbol? baseType = null,
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

            var codeGenOptions = GetCodeGenerationContextInfo(containerNode, access: access);
            return CodeGenerationService.CreateNamedTypeDeclaration(newTypeSymbol, destination, codeGenOptions, CancellationToken.None);
        }
    }
}
