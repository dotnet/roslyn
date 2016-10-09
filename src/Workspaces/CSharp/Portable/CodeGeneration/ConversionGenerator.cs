// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class ConversionGenerator
    {
        internal static TypeDeclarationSyntax AddConversionTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol method,
            Workspace workspace,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var methodDeclaration = GenerateConversionDeclaration(
                method, GetDestination(destination), workspace, options);

            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastOperator);

            return AddMembersTo(destination, members);
        }

        internal static ConversionOperatorDeclarationSyntax GenerateConversionDeclaration(
            IMethodSymbol method,
            CodeGenerationDestination destination,
            Workspace workspace,
            CodeGenerationOptions options)
        {
            var declaration = GenerateConversionDeclarationWorker(method, destination, workspace, options);
            return AddCleanupAnnotationsTo(AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options)));
        }

        private static ConversionOperatorDeclarationSyntax GenerateConversionDeclarationWorker(
            IMethodSymbol method,
            CodeGenerationDestination destination,
            Workspace workspace,
            CodeGenerationOptions options)
        {
            var hasNoBody = !options.GenerateMethodBodies || method.IsExtern;

            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<ConversionOperatorDeclarationSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var operatorToken = SyntaxFactory.Token(SyntaxFacts.GetOperatorKind(method.MetadataName));
            var keyword = method.MetadataName == WellKnownMemberNames.ImplicitConversionName
                ? SyntaxFactory.Token(SyntaxKind.ImplicitKeyword)
                : SyntaxFactory.Token(SyntaxKind.ExplicitKeyword);

            var declaration = SyntaxFactory.ConversionOperatorDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), options),
                modifiers: GenerateModifiers(method),
                implicitOrExplicitKeyword: keyword,
                operatorKeyword: SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                type: method.ReturnType.GenerateTypeSyntax(),
                parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: false, options: options),
                body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : new SyntaxToken());

            declaration = UseExpressionBodyIfDesired(workspace, declaration);

            return declaration;
        }

        private static ConversionOperatorDeclarationSyntax UseExpressionBodyIfDesired(
            Workspace workspace, ConversionOperatorDeclarationSyntax declaration)
        {
            if (declaration.ExpressionBody == null)
            {
                var preferExpressionBody = workspace.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods).Value;
                if (preferExpressionBody)
                {
                    var expressionBody = CodeGenerationHelpers.TryConvertToExpressionBody(declaration.Body);
                    if (expressionBody != null)
                    {
                        return declaration.WithBody(null)
                                          .WithExpressionBody(expressionBody)
                                          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                    }
                }
            }

            return declaration;
        }

        private static SyntaxTokenList GenerateModifiers(IMethodSymbol method)
        {
            return SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }
    }
}
