// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class MethodGenerator
    {
        internal static NamespaceDeclarationSyntax AddMethodTo(
            NamespaceDeclarationSyntax destination,
            IMethodSymbol method,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GenerateMethodDeclaration(method, CodeGenerationDestination.Namespace, options);
            var members = Insert(destination.Members, declaration, options, availableIndices, after: LastMethod);
            return destination.WithMembers(members.ToSyntaxList());
        }

        internal static CompilationUnitSyntax AddMethodTo(
            CompilationUnitSyntax destination,
            IMethodSymbol method,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GenerateMethodDeclaration(method, CodeGenerationDestination.CompilationUnit, options);
            var members = Insert(destination.Members, declaration, options, availableIndices, after: LastMethod);
            return destination.WithMembers(members.ToSyntaxList());
        }

        internal static TypeDeclarationSyntax AddMethodTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol method,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var methodDeclaration = GenerateMethodDeclaration(method, GetDestination(destination), options);

            // Create a clone of the original type with the new method inserted. 
            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastMethod);

            return AddMembersTo(destination, members);
        }

        public static MethodDeclarationSyntax GenerateMethodDeclaration(
            IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            options = options ?? CodeGenerationOptions.Default;

            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<MethodDeclarationSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = GenerateMethodDeclarationWorker(method, destination, options);

            return AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options));
        }

        private static MethodDeclarationSyntax GenerateMethodDeclarationWorker(
            IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var hasNoBody = !options.GenerateMethodBodies ||
                            destination == CodeGenerationDestination.InterfaceType ||
                            method.IsAbstract;

            var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(method.ExplicitInterfaceImplementations);

            return AddCleanupAnnotationsTo(SyntaxFactory.MethodDeclaration(
                attributeLists: GenerateAttributes(method, options, explicitInterfaceSpecifier != null),
                modifiers: GenerateModifiers(method, destination, options),
                refKeyword: method.ReturnsByRef ? SyntaxFactory.Token(SyntaxKind.RefKeyword) : default(SyntaxToken),
                returnType: method.ReturnType.GenerateTypeSyntax(),
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                identifier: method.Name.ToIdentifierToken(),
                typeParameterList: GenerateTypeParameterList(method, options),
                parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, explicitInterfaceSpecifier != null, options),
                constraintClauses: GenerateConstraintClauses(method),
                body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
                expressionBody: default(ArrowExpressionClauseSyntax),
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : new SyntaxToken()));
        }

        private static SyntaxList<AttributeListSyntax> GenerateAttributes(
            IMethodSymbol method, CodeGenerationOptions options, bool isExplicit)
        {
            var attributes = new List<AttributeListSyntax>();

            if (!isExplicit)
            {
                attributes.AddRange(AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), options));
                attributes.AddRange(AttributeGenerator.GenerateAttributeLists(method.GetReturnTypeAttributes(), options, SyntaxFactory.Token(SyntaxKind.ReturnKeyword)));
            }

            return attributes.ToSyntaxList();
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> GenerateConstraintClauses(
            IMethodSymbol method)
        {
            return !method.ExplicitInterfaceImplementations.Any() && !method.IsOverride
                ? method.TypeParameters.GenerateConstraintClauses()
                : default(SyntaxList<TypeParameterConstraintClauseSyntax>);
        }

        private static TypeParameterListSyntax GenerateTypeParameterList(
            IMethodSymbol method, CodeGenerationOptions options)
        {
            return TypeParameterGenerator.GenerateTypeParameterList(method.TypeParameters, options);
        }

        private static SyntaxTokenList GenerateModifiers(
            IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var tokens = new List<SyntaxToken>();

            // Only "unsafe" modifier allowed if we're an explicit impl.
            if (method.ExplicitInterfaceImplementations.Any())
            {
                if (CodeGenerationMethodInfo.GetIsUnsafe(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
                }
            }
            else
            {
                // If we're generating into an interface, then we don't use any modifiers.
                if (destination != CodeGenerationDestination.CompilationUnit &&
                    destination != CodeGenerationDestination.Namespace &&
                    destination != CodeGenerationDestination.InterfaceType)
                {
                    AddAccessibilityModifiers(method.DeclaredAccessibility, tokens, options, Accessibility.Private);

                    if (method.IsAbstract)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
                    }

                    if (method.IsSealed)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
                    }

                    if (method.IsStatic)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    }

                    if (method.IsOverride)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
                    }

                    if (method.IsVirtual)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
                    }

                    if (CodeGenerationMethodInfo.GetIsPartial(method) && !method.IsAsync)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                    }
                }

                if (CodeGenerationMethodInfo.GetIsUnsafe(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
                }

                if (CodeGenerationMethodInfo.GetIsNew(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.NewKeyword));
                }
            }

            if (destination != CodeGenerationDestination.InterfaceType)
            {
                if (CodeGenerationMethodInfo.GetIsAsync(method))
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
                }
            }

            if (CodeGenerationMethodInfo.GetIsPartial(method) && method.IsAsync)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }

            return tokens.ToSyntaxTokenList();
        }
    }
}
