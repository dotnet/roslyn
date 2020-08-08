// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class PropertyGenerator
    {
        public static bool CanBeGenerated(IPropertySymbol property)
            => property.IsIndexer || property.Parameters.Length == 0;

        private static MemberDeclarationSyntax LastPropertyOrField(
            SyntaxList<MemberDeclarationSyntax> members)
        {
            var lastProperty = members.LastOrDefault(m => m is PropertyDeclarationSyntax);
            return lastProperty ?? LastField(members);
        }

        internal static CompilationUnitSyntax AddPropertyTo(
            CompilationUnitSyntax destination,
            IPropertySymbol property,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GeneratePropertyOrIndexer(
                property, CodeGenerationDestination.CompilationUnit, options,
                destination?.SyntaxTree.Options ?? options.ParseOptions);

            var members = Insert(destination.Members, declaration, options,
                availableIndices, after: LastPropertyOrField, before: FirstMember);
            return destination.WithMembers(members);
        }

        internal static TypeDeclarationSyntax AddPropertyTo(
            TypeDeclarationSyntax destination,
            IPropertySymbol property,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GeneratePropertyOrIndexer(property, GetDestination(destination),
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
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<MemberDeclarationSyntax>(property, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = property.IsIndexer
                ? GenerateIndexerDeclaration(property, destination, options, parseOptions)
                : GeneratePropertyDeclaration(property, destination, options, parseOptions);

            return ConditionallyAddDocumentationCommentTo(declaration, property, options);
        }

        private static MemberDeclarationSyntax GenerateIndexerDeclaration(
            IPropertySymbol property,
            CodeGenerationDestination destination,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(property.ExplicitInterfaceImplementations);

            var declaration = SyntaxFactory.IndexerDeclaration(
                    attributeLists: AttributeGenerator.GenerateAttributeLists(property.GetAttributes(), options),
                    modifiers: GenerateModifiers(property, destination, options),
                    type: GenerateTypeSyntax(property),
                    explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                    parameterList: ParameterGenerator.GenerateBracketedParameterList(property.Parameters, explicitInterfaceSpecifier != null, options),
                    accessorList: GenerateAccessorList(property, destination, options, parseOptions));
            declaration = UseExpressionBodyIfDesired(options, declaration, parseOptions);

            return AddFormatterAndCodeGeneratorAnnotationsTo(
                AddAnnotationsTo(property, declaration));
        }

        private static MemberDeclarationSyntax GeneratePropertyDeclaration(
           IPropertySymbol property, CodeGenerationDestination destination,
           CodeGenerationOptions options, ParseOptions parseOptions)
        {
            var initializer = CodeGenerationPropertyInfo.GetInitializer(property) is ExpressionSyntax initializerNode
                ? SyntaxFactory.EqualsValueClause(initializerNode)
                : null;

            var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(property.ExplicitInterfaceImplementations);

            var accessorList = GenerateAccessorList(property, destination, options, parseOptions);

            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(property.GetAttributes(), options),
                modifiers: GenerateModifiers(property, destination, options),
                type: GenerateTypeSyntax(property),
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                identifier: property.Name.ToIdentifierToken(),
                accessorList: accessorList,
                expressionBody: null,
                initializer: initializer);

            propertyDeclaration = UseExpressionBodyIfDesired(options, propertyDeclaration, parseOptions);

            return AddFormatterAndCodeGeneratorAnnotationsTo(
                AddAnnotationsTo(property, propertyDeclaration));
        }

        private static TypeSyntax GenerateTypeSyntax(IPropertySymbol property)
        {
            var returnType = property.Type;

            if (property.ReturnsByRef)
            {
                return returnType.GenerateRefTypeSyntax();
            }
            else if (property.ReturnsByRefReadonly)
            {
                return returnType.GenerateRefReadOnlyTypeSyntax();
            }
            else
            {
                return returnType.GenerateTypeSyntax();
            }
        }

        private static bool TryGetExpressionBody(
            BasePropertyDeclarationSyntax baseProperty, ParseOptions options, ExpressionBodyPreference preference,
            out ArrowExpressionClauseSyntax arrowExpression, out SyntaxToken semicolonToken)
        {
            var accessorList = baseProperty.AccessorList;
            if (preference != ExpressionBodyPreference.Never &&
                accessorList.Accessors.Count == 1)
            {
                var accessor = accessorList.Accessors[0];
                if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    return TryGetArrowExpressionBody(
                        baseProperty.Kind(), accessor, options, preference,
                        out arrowExpression, out semicolonToken);
                }
            }

            arrowExpression = null;
            semicolonToken = default;
            return false;
        }

        private static PropertyDeclarationSyntax UseExpressionBodyIfDesired(
            CodeGenerationOptions options, PropertyDeclarationSyntax declaration, ParseOptions parseOptions)
        {
            if (declaration.ExpressionBody == null)
            {
                var expressionBodyPreference = options.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties).Value;
                if (declaration.Initializer == null)
                {
                    if (TryGetExpressionBody(
                            declaration, parseOptions, expressionBodyPreference,
                            out var expressionBody, out var semicolonToken))
                    {
                        declaration = declaration.WithAccessorList(null)
                                                 .WithExpressionBody(expressionBody)
                                                 .WithSemicolonToken(semicolonToken);
                    }
                }
            }

            return declaration;
        }

        private static IndexerDeclarationSyntax UseExpressionBodyIfDesired(
            CodeGenerationOptions options, IndexerDeclarationSyntax declaration, ParseOptions parseOptions)
        {
            if (declaration.ExpressionBody == null)
            {
                var expressionBodyPreference = options.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers).Value;
                if (TryGetExpressionBody(
                        declaration, parseOptions, expressionBodyPreference,
                        out var expressionBody, out var semicolonToken))
                {
                    declaration = declaration.WithAccessorList(null)
                                             .WithExpressionBody(expressionBody)
                                             .WithSemicolonToken(semicolonToken);
                }
            }

            return declaration;
        }

        private static AccessorDeclarationSyntax UseExpressionBodyIfDesired(
            CodeGenerationOptions options, AccessorDeclarationSyntax declaration, ParseOptions parseOptions)
        {
            if (declaration.ExpressionBody == null)
            {
                var expressionBodyPreference = options.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;
                if (declaration.Body.TryConvertToArrowExpressionBody(
                        declaration.Kind(), parseOptions, expressionBodyPreference,
                        out var expressionBody, out var semicolonToken))
                {
                    declaration = declaration.WithBody(null)
                                             .WithExpressionBody(expressionBody)
                                             .WithSemicolonToken(semicolonToken);
                }
            }

            return declaration;
        }

        private static bool TryGetArrowExpressionBody(
            SyntaxKind declaratoinKind, AccessorDeclarationSyntax accessor, ParseOptions options, ExpressionBodyPreference preference,
            out ArrowExpressionClauseSyntax arrowExpression, out SyntaxToken semicolonToken)
        {
            // If the accessor has an expression body already, then use that as the expression body
            // for the property.
            if (accessor.ExpressionBody != null)
            {
                arrowExpression = accessor.ExpressionBody;
                semicolonToken = accessor.SemicolonToken;
                return true;
            }

            return accessor.Body.TryConvertToArrowExpressionBody(
                declaratoinKind, options, preference, out arrowExpression, out semicolonToken);
        }

        private static AccessorListSyntax GenerateAccessorList(
            IPropertySymbol property, CodeGenerationDestination destination,
            CodeGenerationOptions options, ParseOptions parseOptions)
        {
            var setAccessorKind = property.SetMethod?.IsInitOnly == true ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration;
            var accessors = new List<AccessorDeclarationSyntax>
            {
                GenerateAccessorDeclaration(property, property.GetMethod, SyntaxKind.GetAccessorDeclaration, destination, options, parseOptions),
                GenerateAccessorDeclaration(property, property.SetMethod, setAccessorKind, destination, options, parseOptions),
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
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var hasBody = options.GenerateMethodBodies && HasAccessorBodies(property, destination, accessor);
            return accessor == null
                ? null
                : GenerateAccessorDeclaration(property, accessor, kind, hasBody, options, parseOptions);
        }

        private static AccessorDeclarationSyntax GenerateAccessorDeclaration(
            IPropertySymbol property,
            IMethodSymbol accessor,
            SyntaxKind kind,
            bool hasBody,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var declaration = SyntaxFactory.AccessorDeclaration(kind)
                                           .WithModifiers(GenerateAccessorModifiers(property, accessor, options))
                                           .WithBody(hasBody ? GenerateBlock(accessor) : null)
                                           .WithSemicolonToken(hasBody ? default : SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            declaration = UseExpressionBodyIfDesired(options, declaration, parseOptions);

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
            var modifiers = ArrayBuilder<SyntaxToken>.GetInstance();

            if (accessor.DeclaredAccessibility != Accessibility.NotApplicable &&
                accessor.DeclaredAccessibility != property.DeclaredAccessibility)
            {
                AddAccessibilityModifiers(accessor.DeclaredAccessibility, modifiers, options, property.DeclaredAccessibility);
            }

            var hasNonReadOnlyAccessor = property.GetMethod?.IsReadOnly == false || property.SetMethod?.IsReadOnly == false;
            if (hasNonReadOnlyAccessor && accessor.IsReadOnly)
            {
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            }

            return modifiers.ToSyntaxTokenListAndFree();
        }

        private static SyntaxTokenList GenerateModifiers(
            IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var tokens = ArrayBuilder<SyntaxToken>.GetInstance();

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

                    // note: explicit interface impls are allowed to be 'readonly' but it never actually affects callers
                    // because of the boxing requirement in order to call the method.
                    // therefore it seems like a small oversight to leave out the keyword for an explicit impl from metadata.
                    var hasAllReadOnlyAccessors = property.GetMethod?.IsReadOnly != false && property.SetMethod?.IsReadOnly != false;
                    // Don't show the readonly modifier if the containing type is already readonly
                    if (hasAllReadOnlyAccessors && !property.ContainingType.IsReadOnly)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
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
