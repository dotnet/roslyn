// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class FieldGenerator
    {
        private static MemberDeclarationSyntax LastField(
            SyntaxList<MemberDeclarationSyntax> members,
            FieldDeclarationSyntax fieldDeclaration)
        {
            var lastConst = members.OfType<FieldDeclarationSyntax>()
                                   .Where(f => f.Modifiers.Any(SyntaxKind.ConstKeyword))
                                   .LastOrDefault();

            // Place a const after the last existing const.  If we don't have a last const
            // we'll just place the const before the first member in the type.
            if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                return lastConst;
            }

            var lastReadOnly = members.OfType<FieldDeclarationSyntax>()
                                      .Where(f => f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                                      .LastOrDefault();

            var lastNormal = members.OfType<FieldDeclarationSyntax>()
                                    .Where(f => !f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) && !f.Modifiers.Any(SyntaxKind.ConstKeyword))
                                    .LastOrDefault();

            // Place a readonly field after the last readonly field if we have one.  Otherwise
            // after the last field/const.
            return fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)
                ? lastReadOnly ?? lastConst ?? lastNormal
                : lastNormal ?? lastReadOnly ?? lastConst;
        }

        internal static CompilationUnitSyntax AddFieldTo(
            CompilationUnitSyntax destination,
            IFieldSymbol field,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GenerateFieldDeclaration(field, CodeGenerationDestination.CompilationUnit, options);

            // Place the field after the last field or const, or at the start of the type
            // declaration.
            var members = Insert(destination.Members, declaration, options, availableIndices,
                after: m => LastField(m, declaration), before: FirstMember);
            return destination.WithMembers(members.ToSyntaxList());
        }

        internal static TypeDeclarationSyntax AddFieldTo(
            TypeDeclarationSyntax destination,
            IFieldSymbol field,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GenerateFieldDeclaration(field, GetDestination(destination), options);

            // Place the field after the last field or const, or at the start of the type
            // declaration.
            var members = Insert(destination.Members, declaration, options, availableIndices,
                after: m => LastField(m, declaration), before: FirstMember);

            return AddMembersTo(destination, members);
        }

        public static FieldDeclarationSyntax GenerateFieldDeclaration(
            IFieldSymbol field, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<VariableDeclaratorSyntax>(field, options);
            if (reusableSyntax != null)
            {
                if (reusableSyntax.Parent is VariableDeclarationSyntax variableDeclaration)
                {
                    var newVariableDeclaratorsList = new SeparatedSyntaxList<VariableDeclaratorSyntax>().Add(reusableSyntax);
                    var newVariableDeclaration = variableDeclaration.WithVariables(newVariableDeclaratorsList);
                    if (variableDeclaration.Parent is FieldDeclarationSyntax fieldDecl)
                    {
                        return fieldDecl.WithDeclaration(newVariableDeclaration);
                    }
                }
            }


            var initializer = CodeGenerationFieldInfo.GetInitializer(field) is ExpressionSyntax initializerNode
                ? SyntaxFactory.EqualsValueClause(initializerNode)
                : GenerateEqualsValue(field);

            var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                AttributeGenerator.GenerateAttributeLists(field.GetAttributes(), options),
                GenerateModifiers(field, options),
                SyntaxFactory.VariableDeclaration(
                    field.Type.WithNullability(field.NullableAnnotation).GenerateTypeSyntax(),
                    SyntaxFactory.SingletonSeparatedList(
                        AddAnnotationsTo(field, SyntaxFactory.VariableDeclarator(field.Name.ToIdentifierToken(), null, initializer)))));

            return AddFormatterAndCodeGeneratorAnnotationsTo(
                ConditionallyAddDocumentationCommentTo(fieldDeclaration, field, options));
        }

        private static EqualsValueClauseSyntax GenerateEqualsValue(IFieldSymbol field)
        {
            if (field.HasConstantValue)
            {
                var canUseFieldReference = field.Type != null && !field.Type.Equals(field.ContainingType);
                return SyntaxFactory.EqualsValueClause(ExpressionGenerator.GenerateExpression(field.Type, field.ConstantValue, canUseFieldReference));
            }

            return null;
        }

        private static SyntaxTokenList GenerateModifiers(IFieldSymbol field, CodeGenerationOptions options)
        {
            var tokens = ArrayBuilder<SyntaxToken>.GetInstance();

            AddAccessibilityModifiers(field.DeclaredAccessibility, tokens, options, Accessibility.Private);
            if (field.IsConst)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.ConstKeyword));
            }
            else
            {
                if (field.IsStatic)
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                }

                if (field.IsReadOnly)
                {
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                }
            }

            if (CodeGenerationFieldInfo.GetIsUnsafe(field))
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

            return tokens.ToSyntaxTokenListAndFree();
        }
    }
}
