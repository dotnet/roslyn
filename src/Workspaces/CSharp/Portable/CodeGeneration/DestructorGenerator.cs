// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class DestructorGenerator
    {
        private static MemberDeclarationSyntax LastConstructorOrField(SyntaxList<MemberDeclarationSyntax> members)
        {
            return LastConstructor(members) ?? LastField(members);
        }

        internal static TypeDeclarationSyntax AddDestructorTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol destructor,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var destructorDeclaration = GenerateDestructorDeclaration(destructor, GetDestination(destination), options);

            // Generate after the last constructor, or after the last field, or at the start of the
            // type.
            var members = Insert(destination.Members, destructorDeclaration, options,
                availableIndices, after: LastConstructorOrField, before: FirstMember);

            return AddMembersTo(destination, members);
        }

        internal static DestructorDeclarationSyntax GenerateDestructorDeclaration(
            IMethodSymbol destructor, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            options ??= CodeGenerationOptions.Default;

            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<DestructorDeclarationSyntax>(destructor, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var hasNoBody = !options.GenerateMethodBodies;

            var declaration = SyntaxFactory.DestructorDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(destructor.GetAttributes(), options),
                modifiers: default,
                tildeToken: SyntaxFactory.Token(SyntaxKind.TildeToken),
                identifier: CodeGenerationDestructorInfo.GetTypeName(destructor).ToIdentifierToken(),
                parameterList: SyntaxFactory.ParameterList(),
                body: hasNoBody ? null : GenerateBlock(destructor),
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : default);

            return AddFormatterAndCodeGeneratorAnnotationsTo(
                ConditionallyAddDocumentationCommentTo(declaration, destructor, options));
        }

        private static BlockSyntax GenerateBlock(
            IMethodSymbol constructor)
        {
            var statements = CodeGenerationDestructorInfo.GetStatements(constructor) == null
                ? default
                : StatementGenerator.GenerateStatements(CodeGenerationDestructorInfo.GetStatements(constructor));

            return SyntaxFactory.Block(statements);
        }
    }
}
