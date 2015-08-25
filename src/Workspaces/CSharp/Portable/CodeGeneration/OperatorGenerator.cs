// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class OperatorGenerator
    {
        internal static TypeDeclarationSyntax AddOperatorTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol method,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var methodDeclaration = GenerateOperatorDeclaration(method, GetDestination(destination), options);

            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastOperator);

            return AddMembersTo(destination, members);
        }

        internal static OperatorDeclarationSyntax GenerateOperatorDeclaration(
            IMethodSymbol method,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<OperatorDeclarationSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = GenerateOperatorDeclarationWorker(method, destination, options);

            return AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options));
        }

        private static OperatorDeclarationSyntax GenerateOperatorDeclarationWorker(
            IMethodSymbol method,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            var hasNoBody = !options.GenerateMethodBodies || method.IsExtern;

            var operatorSyntaxKind = SyntaxFacts.GetOperatorKind(method.MetadataName);
            if (operatorSyntaxKind == SyntaxKind.None)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.CannotCodeGenUnsupportedOperator, method.Name), "method");
            }

            var operatorToken = SyntaxFactory.Token(operatorSyntaxKind);

            return SyntaxFactory.OperatorDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), options),
                modifiers: GenerateModifiers(method),
                returnType: method.ReturnType.GenerateTypeSyntax(),
                operatorKeyword: SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                operatorToken: operatorToken,
                parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: false, options: options),
                body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : new SyntaxToken());
        }

        private static SyntaxTokenList GenerateModifiers(IMethodSymbol method)
        {
            return SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }
    }
}
