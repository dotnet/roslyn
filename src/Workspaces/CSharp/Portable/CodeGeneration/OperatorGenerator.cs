// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
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
            Workspace workspace,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var methodDeclaration = GenerateOperatorDeclaration(
                method, GetDestination(destination), workspace, options,
                destination?.SyntaxTree.Options ?? options.ParseOptions);

            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastOperator);

            return AddMembersTo(destination, members);
        }

        internal static OperatorDeclarationSyntax GenerateOperatorDeclaration(
            IMethodSymbol method,
            CodeGenerationDestination destination,
            Workspace workspace,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<OperatorDeclarationSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = GenerateOperatorDeclarationWorker(method, destination, options);
            declaration = UseExpressionBodyIfDesired(workspace, declaration, parseOptions);

            return AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options));
        }

        private static OperatorDeclarationSyntax UseExpressionBodyIfDesired(
            Workspace workspace, OperatorDeclarationSyntax declaration, ParseOptions options)
        {
            if (declaration.ExpressionBody == null)
            {
                var expressionBodyPreference = workspace.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators).Value;
                if (declaration.Body.TryConvertToArrowExpressionBody(
                        declaration.Kind(), options, expressionBodyPreference,
                        out var expressionBody, out var semicolonToken))
                {
                    return declaration.WithBody(null)
                                      .WithExpressionBody(expressionBody)
                                      .WithSemicolonToken(semicolonToken);
                }
            }

            return declaration;
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
                throw new ArgumentException(string.Format(WorkspacesResources.Cannot_generate_code_for_unsupported_operator_0, method.Name), nameof(method));
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
