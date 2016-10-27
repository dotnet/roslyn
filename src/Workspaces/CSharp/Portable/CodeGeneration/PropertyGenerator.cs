// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class PropertyGenerator
    {
        public static bool CanBeGenerated(IPropertySymbol property)
        {
            return property.IsIndexer || property.Parameters.Length == 0;
        }

        private static MemberDeclarationSyntax LastPropertyOrField(
            SyntaxList<MemberDeclarationSyntax> members)
        {
            var lastProperty = members.LastOrDefault(m => m is PropertyDeclarationSyntax);
            return lastProperty ?? LastField(members);
        }

        internal static CompilationUnitSyntax AddPropertyTo(
            CompilationUnitSyntax destination,
            IPropertySymbol property,
            Workspace workspace,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GeneratePropertyOrIndexer(
                property, CodeGenerationDestination.CompilationUnit, workspace, options,
                destination?.SyntaxTree.Options ?? options.ParseOptions);

            var members = Insert(destination.Members, declaration, options,
                availableIndices, after: LastPropertyOrField, before: FirstMember);
            return destination.WithMembers(members);
        }

        internal static TypeDeclarationSyntax AddPropertyTo(
            TypeDeclarationSyntax destination,
            IPropertySymbol property,
            Workspace workspace,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GeneratePropertyOrIndexer(property, GetDestination(destination), workspace,
                options, destination?.SyntaxTree.Options ?? options.ParseOptions);

            // Create a clone of the original type with the new method inserted. 
            var members = Insert(destination.Members, declaration, options,
                availableIndices, after: LastPropertyOrField, before: FirstMember);

            // Find the best place to put the field.  It should go after the last field if we already
            // have fields, or at the beginning of the file if we don't.
            return AddMembersTo(destination, members);
        }

        public static MemberDeclarationSyntax GeneratePropertyOrIndexer(
            IPropertySymbol property,
            CodeGenerationDestination destination,
            Workspace workspace,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<MemberDeclarationSyntax>(property, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = property.IsIndexer
                ? GenerateIndexerDeclaration(property, destination, workspace, options, parseOptions)
                : GeneratePropertyDeclaration(property, destination, workspace, options, parseOptions);

            return ConditionallyAddDocumentationCommentTo(declaration, property, options);
        }

        private static MemberDeclarationSyntax GenerateIndexerDeclaration(
            IPropertySymbol property,
            CodeGenerationDestination destination,
            Workspace workspace,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(property.ExplicitInterfaceImplementations);

            var declaration = SyntaxFactory.IndexerDeclaration(
                    attributeLists: AttributeGenerator.GenerateAttributeLists(property.GetAttributes(), options),
                    modifiers: GenerateModifiers(property, destination, options),
                    type: property.Type.GenerateTypeSyntax(),
                    explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                    parameterList: ParameterGenerator.GenerateBracketedParameterList(property.Parameters, explicitInterfaceSpecifier != null, options),
                    accessorList: GenerateAccessorList(property, destination, workspace, options, parseOptions));
            declaration = UseExpressionBodyIfDesired(workspace, declaration, parseOptions);

            return AddCleanupAnnotationsTo(
                AddAnnotationsTo(property, declaration));
        }

        private static MemberDeclarationSyntax GeneratePropertyDeclaration(
           IPropertySymbol property, CodeGenerationDestination destination,
           Workspace workspace, CodeGenerationOptions options, ParseOptions parseOptions)
        {
            var initializerNode = CodeGenerationPropertyInfo.GetInitializer(property) as ExpressionSyntax;

            var initializer = initializerNode != null
                ? SyntaxFactory.EqualsValueClause(initializerNode)
                : default(EqualsValueClauseSyntax);

            var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(property.ExplicitInterfaceImplementations);

            var accessorList = GenerateAccessorList(property, destination, workspace, options, parseOptions);

            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(property.GetAttributes(), options),
                modifiers: GenerateModifiers(property, destination, options),
                type: property.Type.GenerateTypeSyntax(),
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                identifier: property.Name.ToIdentifierToken(),
                accessorList: accessorList,
                expressionBody: default(ArrowExpressionClauseSyntax),
                initializer: initializer);

            propertyDeclaration = UseExpressionBodyIfDesired(
                workspace, propertyDeclaration, parseOptions);

            return AddCleanupAnnotationsTo(
                AddAnnotationsTo(property, propertyDeclaration));
        }

        private static ArrowExpressionClauseSyntax TryGetExpressionBody(
            AccessorListSyntax accessorList, ParseOptions options)
        {
            if (accessorList.Accessors.Count == 1)
            {
                var accessor = accessorList.Accessors[0];
                if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    return TryGetExpressionBody(accessor, options);
                }
            }

            return null;
        }

        private static PropertyDeclarationSyntax UseExpressionBodyIfDesired(
            Workspace workspace, PropertyDeclarationSyntax declaration, ParseOptions options)
        {
            if (declaration.ExpressionBody == null)
            {
                var preferExpressionBody = workspace.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties).Value;
                if (preferExpressionBody)
                {
                    if (declaration.Initializer == null)
                    {
                        var expressionBody = TryGetExpressionBody(declaration.AccessorList, options);
                        if (expressionBody != null)
                        {
                            declaration = declaration.WithAccessorList(null)
                                                     .WithExpressionBody(expressionBody)
                                                     .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                        }
                    }
                }
            }

            return declaration;
        }

        private static IndexerDeclarationSyntax UseExpressionBodyIfDesired(
            Workspace workspace, IndexerDeclarationSyntax declaration, ParseOptions options)
        {
            if (declaration.ExpressionBody == null)
            {
                var preferExpressionBody = workspace.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers).Value;
                if (preferExpressionBody)
                {
                    var expressionBody = TryGetExpressionBody(declaration.AccessorList, options);
                    if (expressionBody != null)
                    {
                        declaration = declaration.WithAccessorList(null)
                                                 .WithExpressionBody(expressionBody)
                                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                }
            }

            return declaration;
        }

        private static AccessorDeclarationSyntax UseExpressionBodyIfDesired(
            Workspace workspace, AccessorDeclarationSyntax declaration, ParseOptions options)
        {
            if (declaration.ExpressionBody == null)
            {
                var preferExpressionBody = workspace.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;
                if (preferExpressionBody)
                {
                    var expressionBody = declaration.Body.TryConvertToExpressionBody(options);
                    if (expressionBody != null)
                    {
                        declaration = declaration.WithBody(null)
                                                 .WithExpressionBody(expressionBody)
                                                 .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                }
            }

            return declaration;
        }

        private static ArrowExpressionClauseSyntax TryGetExpressionBody(
            AccessorDeclarationSyntax accessor, ParseOptions options)
        {
            // If the accessor has an expression body already, then use that as the expression body
            // for the property.
            return accessor.ExpressionBody != null
                ? accessor.ExpressionBody
                : accessor.Body.TryConvertToExpressionBody(options);
        }

        private static AccessorListSyntax GenerateAccessorList(
            IPropertySymbol property, CodeGenerationDestination destination, 
            Workspace workspace, CodeGenerationOptions options, ParseOptions parseOptions)
        {
            var accessors = new List<AccessorDeclarationSyntax>
            {
                GenerateAccessorDeclaration(property, property.GetMethod, SyntaxKind.GetAccessorDeclaration, destination, workspace, options, parseOptions),
                GenerateAccessorDeclaration(property, property.SetMethod, SyntaxKind.SetAccessorDeclaration, destination, workspace, options, parseOptions),
            };

            return accessors[0] == null && accessors[1] == null
                ? null
                : SyntaxFactory.AccessorList(accessors.WhereNotNull().ToSyntaxList());
        }

        private static AccessorDeclarationSyntax GenerateAccessorDeclaration(
            IPropertySymbol property,
            IMethodSymbol accessor,
            SyntaxKind kind,
            CodeGenerationDestination destination,
            Workspace workspace,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var hasBody = options.GenerateMethodBodies && HasAccessorBodies(property, destination, accessor);
            return accessor == null
                ? null
                : GenerateAccessorDeclaration(property, accessor, kind, hasBody, workspace, options, parseOptions);
        }

        private static AccessorDeclarationSyntax GenerateAccessorDeclaration(
            IPropertySymbol property,
            IMethodSymbol accessor,
            SyntaxKind kind,
            bool hasBody,
            Workspace workspace,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var declaration = SyntaxFactory.AccessorDeclaration(kind)
                                           .WithModifiers(GenerateAccessorModifiers(property, accessor, options))
                                           .WithBody(hasBody ? GenerateBlock(accessor) : null)
                                           .WithSemicolonToken(hasBody ? default(SyntaxToken) : SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            declaration = UseExpressionBodyIfDesired(workspace, declaration, parseOptions);

            return AddAnnotationsTo(accessor, declaration);
        }

        private static BlockSyntax GenerateBlock(IMethodSymbol accessor)
        {
            return SyntaxFactory.Block(
                StatementGenerator.GenerateStatements(CodeGenerationMethodInfo.GetStatements(accessor)));
        }

        private static bool HasAccessorBodies(
            IPropertySymbol property,
            CodeGenerationDestination destination,
            IMethodSymbol accessor)
        {
            return destination != CodeGenerationDestination.InterfaceType &&
                !property.IsAbstract &&
                accessor != null &&
                !accessor.IsAbstract;
        }

        private static SyntaxTokenList GenerateAccessorModifiers(
            IPropertySymbol property,
            IMethodSymbol accessor,
            CodeGenerationOptions options)
        {
            if (accessor.DeclaredAccessibility == Accessibility.NotApplicable ||
                accessor.DeclaredAccessibility == property.DeclaredAccessibility)
            {
                return new SyntaxTokenList();
            }

            var modifiers = new List<SyntaxToken>();
            AddAccessibilityModifiers(accessor.DeclaredAccessibility, modifiers, options, property.DeclaredAccessibility);

            return modifiers.ToSyntaxTokenList();
        }

        private static SyntaxTokenList GenerateModifiers(
            IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var tokens = new List<SyntaxToken>();

            // Most modifiers not allowed if we're an explicit impl.
            if (!property.ExplicitInterfaceImplementations.Any())
            {
                if (destination != CodeGenerationDestination.CompilationUnit &&
                    destination != CodeGenerationDestination.InterfaceType)
                {
                    AddAccessibilityModifiers(property.DeclaredAccessibility, tokens, options, Accessibility.Private);

                    if (property.IsStatic)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    }

                    if (property.IsSealed)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
                    }

                    if (property.IsOverride)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
                    }

                    if (property.IsVirtual)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
                    }

                    if (property.IsAbstract)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
                    }
                }
            }

            if (CodeGenerationPropertyInfo.GetIsUnsafe(property))
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

            return tokens.ToSyntaxTokenList();
        }
    }
}
