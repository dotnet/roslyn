// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

internal static class OperatorGenerator
{
    internal static TypeDeclarationSyntax AddOperatorTo(
        TypeDeclarationSyntax destination,
        IMethodSymbol method,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var methodDeclaration = GenerateOperatorDeclaration(method, GetDestination(destination), info, cancellationToken);
        var members = Insert(destination.Members, methodDeclaration, info, availableIndices, after: LastOperator);

        return AddMembersTo(destination, members, cancellationToken);
    }

    internal static OperatorDeclarationSyntax GenerateOperatorDeclaration(
        IMethodSymbol method,
        CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<OperatorDeclarationSyntax>(method, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var declaration = GenerateOperatorDeclarationWorker(method, destination, info, cancellationToken);
        declaration = UseExpressionBodyIfDesired(info, declaration, cancellationToken);

        return AddAnnotationsTo(method,
            ConditionallyAddDocumentationCommentTo(declaration, method, info, cancellationToken));
    }

    private static OperatorDeclarationSyntax UseExpressionBodyIfDesired(
        CSharpCodeGenerationContextInfo info, OperatorDeclarationSyntax declaration, CancellationToken cancellationToken)
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

    private static OperatorDeclarationSyntax GenerateOperatorDeclarationWorker(
        IMethodSymbol method,
        CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info,
        CancellationToken cancellationToken)
    {
        var hasNoBody = !info.Context.GenerateMethodBodies || method.IsExtern || method.IsAbstract;

        var operatorSyntaxKind = SyntaxFacts.GetOperatorKind(method.MetadataName);
        if (operatorSyntaxKind == SyntaxKind.None)
        {
            throw new ArgumentException(string.Format(WorkspaceExtensionsResources.Cannot_generate_code_for_unsupported_operator_0, method.Name), nameof(method));
        }

        var operatorToken = SyntaxFactory.Token(operatorSyntaxKind);
        var checkedToken = SyntaxFacts.IsCheckedOperator(method.MetadataName)
            ? SyntaxFactory.Token(SyntaxKind.CheckedKeyword)
            : default;

        var operatorDecl = SyntaxFactory.OperatorDeclaration(
            attributeLists: AttributeGenerator.GenerateAttributeLists(method.GetAttributes(), info),
            modifiers: GenerateModifiers(method, destination, hasNoBody),
            returnType: method.ReturnType.GenerateTypeSyntax(),
            explicitInterfaceSpecifier: GenerateExplicitInterfaceSpecifier(method.ExplicitInterfaceImplementations),
            operatorKeyword: SyntaxFactory.Token(SyntaxKind.OperatorKeyword),
            checkedKeyword: checkedToken,
            operatorToken: operatorToken,
            parameterList: ParameterGenerator.GenerateParameterList(method.Parameters, isExplicit: false, info: info),
            body: hasNoBody ? null : StatementGenerator.GenerateBlock(method),
            expressionBody: null,
            semicolonToken: hasNoBody ? SyntaxFactory.Token(SyntaxKind.SemicolonToken) : new SyntaxToken());

        operatorDecl = UseExpressionBodyIfDesired(info, operatorDecl, cancellationToken);
        return operatorDecl;
    }

    private static SyntaxTokenList GenerateModifiers(IMethodSymbol method, CodeGenerationDestination destination, bool hasNoBody)
    {
        using var tokens = TemporaryArray<SyntaxToken>.Empty;

        if (method.ExplicitInterfaceImplementations.Length == 0 &&
            !(destination is CodeGenerationDestination.InterfaceType && hasNoBody))
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
