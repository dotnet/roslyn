// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class OperatorGenerator
    {
        internal static TypeDeclarationSyntax AddOperatorTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol method,
            CSharpCodeGenerationOptions options,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = GenerateOperatorDeclaration(method, options, cancellationToken);
            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastOperator);

            return AddMembersTo(destination, members, cancellationToken);
        }

        internal static OperatorDeclarationSyntax GenerateOperatorDeclaration(
            IMethodSymbol method,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<OperatorDeclarationSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = GenerateOperatorDeclarationWorker(method, options);
            declaration = UseExpressionBodyIfDesired(options, declaration);

            return AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options, cancellationToken));
        }

        private static OperatorDeclarationSyntax UseExpressionBodyIfDesired(
            CSharpCodeGenerationOptions options, OperatorDeclarationSyntax declaration)
        {
            if (declaration.ExpressionBody == null)
            {
                if (declaration.Body?.TryConvertToArrowExpressionBody(
                    declaration.Kind(), options.Preferences.LanguageVersion, options.Preferences.PreferExpressionBodiedOperators,
                    out var expressionBody, out var semicolonToken) == true)
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
            CSharpCodeGenerationOptions options)
        {
            var hasNoBody = !options.Context.GenerateMethodBodies || method.IsExtern || method.IsAbstract;

            var operatorSyntaxKind = SyntaxFacts.GetOperatorKind(method.MetadataName);
            if (operatorSyntaxKind == SyntaxKind.None)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.Cannot_generate_code_for_unsupported_operator_0, method.Name), nameof(method));
            }

            var operatorToken = SyntaxFactory.Token(operatorSyntaxKind);

            var operatorDecl = SyntaxFactory.OperatorDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), options),
                modifiers: GenerateModifiers(method),
                returnType: method.ReturnType.GenerateTypeSyntax(),
                explicitInterfaceSpecifier: GenerateExplicitInterfaceSpecifier(method.ExplicitInterfaceImplementations),
                operatorKeyword: SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                operatorToken: operatorToken,
                parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: false, options: options),
                body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
                expressionBody: null,
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : new SyntaxToken());

            operatorDecl = UseExpressionBodyIfDesired(options, operatorDecl);
            return operatorDecl;
        }

        private static SyntaxTokenList GenerateModifiers(IMethodSymbol method)
        {
            using var tokens = TemporaryArray<SyntaxToken>.Empty;

            if (method.ExplicitInterfaceImplementations.Length == 0)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }

            tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            if (method.IsAbstract)
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
            }

            return tokens.ToImmutableAndClear().ToSyntaxTokenList();
        }
    }
}
