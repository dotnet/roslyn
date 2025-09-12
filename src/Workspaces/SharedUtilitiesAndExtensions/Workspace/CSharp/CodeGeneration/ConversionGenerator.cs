// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class ConversionGenerator
{
    internal static TypeDeclarationSyntax AddConversionTo(
        TypeDeclarationSyntax destination,
        IMethodSymbol method,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var methodDeclaration = GenerateConversionDeclaration(method, GetDestination(destination), info, cancellationToken);
        var members = Insert(destination.Members, methodDeclaration, info, availableIndices, after: LastOperator);

        return AddMembersTo(destination, members, cancellationToken);
    }

    internal static ConversionOperatorDeclarationSyntax GenerateConversionDeclaration(
        IMethodSymbol method,
        CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var declaration = GenerateConversionDeclarationWorker(method, destination, info, cancellationToken);
        return AddFormatterAndCodeGeneratorAnnotationsTo(AddAnnotationsTo(method,
            ConditionallyAddDocumentationCommentTo(declaration, method, info, cancellationToken)));
    }

    private static ConversionOperatorDeclarationSyntax GenerateConversionDeclarationWorker(
        IMethodSymbol method,
        CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<ConversionOperatorDeclarationSyntax>(method, info);
        if (reusableSyntax != null)
            return reusableSyntax;

        var keyword = method.MetadataName == WellKnownMemberNames.ImplicitConversionName
            ? ImplicitKeyword
            : ExplicitKeyword;

        var checkedKeyword = SyntaxFacts.IsCheckedOperator(method.MetadataName)
            ? CheckedKeyword
            : default;

        var isExplicit = method.ExplicitInterfaceImplementations.Length > 0;
        var hasNoBody = !info.Context.GenerateMethodBodies || method.IsExtern;

        var declaration = ConversionOperatorDeclaration(
            attributeLists: AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), info),
            modifiers: GenerateModifiers(method, destination),
            implicitOrExplicitKeyword: keyword,
            explicitInterfaceSpecifier: GenerateExplicitInterfaceSpecifier(method.ExplicitInterfaceImplementations),
            operatorKeyword: OperatorKeyword,
            checkedKeyword: checkedKeyword,
            type: method.ReturnType.GenerateTypeSyntax(),
            parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: isExplicit, info: info),
            body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
            expressionBody: null,
            semicolonToken: hasNoBody ? SemicolonToken : default);

        declaration = UseExpressionBodyIfDesired(info, declaration, cancellationToken);

        return declaration;
    }

    private static ConversionOperatorDeclarationSyntax UseExpressionBodyIfDesired(
        CSharpCodeGenerationContextInfo info, ConversionOperatorDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        if (declaration.ExpressionBody == null)
        {
            if (declaration.Body?.TryConvertToArrowExpressionBody(
                declaration.Kind(), info.LanguageVersion, info.Options.PreferExpressionBodiedOperators.Value, cancellationToken,
                out var expressionBody, out var semicolonToken) == true)
            {
                return declaration.WithBody(null)
                                  .WithExpressionBody(expressionBody)
                                  .WithSemicolonToken(semicolonToken);
            }
        }

        return declaration;
    }

    private static SyntaxTokenList GenerateModifiers(IMethodSymbol method, CodeGenerationDestination destination)
    {
        // If these appear in interfaces they must be static abstract
        if (destination is CodeGenerationDestination.InterfaceType)
            return [StaticKeyword, AbstractKeyword];

        using var tokens = TemporaryArray<SyntaxToken>.Empty;

        if (method.ExplicitInterfaceImplementations.Length == 0)
            tokens.Add(PublicKeyword);

        tokens.Add(StaticKeyword);

        if (method.IsAbstract)
            tokens.Add(AbstractKeyword);

        return [.. tokens.ToImmutableAndClear()];
    }
}
