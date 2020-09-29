// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var methodDeclaration = GenerateConversionDeclaration(
                method, options, destination?.SyntaxTree.Options ?? options.ParseOptions);

            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastOperator);

            return AddMembersTo(destination, members);
        }

        internal static ConversionOperatorDeclarationSyntax GenerateConversionDeclaration(
            IMethodSymbol method,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var declaration = GenerateConversionDeclarationWorker(method, options, parseOptions);
            return AddFormatterAndCodeGeneratorAnnotationsTo(AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options)));
        }

        private static ConversionOperatorDeclarationSyntax GenerateConversionDeclarationWorker(
            IMethodSymbol method,
            CodeGenerationOptions options,
            ParseOptions parseOptions)
        {
            var hasNoBody = !options.GenerateMethodBodies || method.IsExtern;

            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<ConversionOperatorDeclarationSyntax>(method, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var keyword = method.MetadataName == WellKnownMemberNames.ImplicitConversionName
                ? SyntaxFactory.Token(SyntaxKind.ImplicitKeyword)
                : SyntaxFactory.Token(SyntaxKind.ExplicitKeyword);

            var declaration = SyntaxFactory.ConversionOperatorDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), options),
                modifiers: GenerateModifiers(),
                implicitOrExplicitKeyword: keyword,
                operatorKeyword: SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
                type: method.ReturnType.GenerateTypeSyntax(),
                parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: false, options: options),
                body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
                semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : new SyntaxToken());

            declaration = UseExpressionBodyIfDesired(options, declaration, parseOptions);

            return declaration;
        }

        private static ConversionOperatorDeclarationSyntax UseExpressionBodyIfDesired(
            CodeGenerationOptions options, ConversionOperatorDeclarationSyntax declaration, ParseOptions parseOptions)
        {
            if (declaration.ExpressionBody == null)
            {
                var expressionBodyPreference = options.Options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators).Value;

                if (declaration.Body.TryConvertToArrowExpressionBody(
                        declaration.Kind(), parseOptions, expressionBodyPreference,
                        out var expressionBody, out var semicolonToken))
                {
                    return declaration.WithBody(null)
                                      .WithExpressionBody(expressionBody)
                                      .WithSemicolonToken(semicolonToken);
                }
            }

            return declaration;
        }

        private static SyntaxTokenList GenerateModifiers()
        {
            return SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }
    }
}
