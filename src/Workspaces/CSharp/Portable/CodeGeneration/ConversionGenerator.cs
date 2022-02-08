// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal static class ConversionGenerator
    {
        internal static TypeDeclarationSyntax AddConversionTo(
            TypeDeclarationSyntax destination,
            IMethodSymbol method,
            CSharpCodeGenerationOptions options,
            IList<bool>? availableIndices,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = GenerateConversionDeclaration(method, options, cancellationToken);
            var members = Insert(destination.Members, methodDeclaration, options, availableIndices, after: LastOperator);

            return AddMembersTo(destination, members, cancellationToken);
        }

        internal static ConversionOperatorDeclarationSyntax GenerateConversionDeclaration(
            IMethodSymbol method,
            CSharpCodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var declaration = GenerateConversionDeclarationWorker(method, options);
            return AddFormatterAndCodeGeneratorAnnotationsTo(AddAnnotationsTo(method,
                ConditionallyAddDocumentationCommentTo(declaration, method, options, cancellationToken)));
        }

        private static ConversionOperatorDeclarationSyntax GenerateConversionDeclarationWorker(
            IMethodSymbol method,
            CSharpCodeGenerationOptions options)
        {
            var hasNoBody = !options.Context.GenerateMethodBodies || method.IsExtern;

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

            declaration = UseExpressionBodyIfDesired(options, declaration);

            return declaration;
        }

        private static ConversionOperatorDeclarationSyntax UseExpressionBodyIfDesired(
            CSharpCodeGenerationOptions options, ConversionOperatorDeclarationSyntax declaration)
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

        private static SyntaxTokenList GenerateModifiers()
        {
            return SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }
    }
}
