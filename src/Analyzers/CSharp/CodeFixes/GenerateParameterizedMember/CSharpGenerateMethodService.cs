// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod;

[ExportLanguageService(typeof(IGenerateParameterizedMemberService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpGenerateMethodService() :
    AbstractGenerateMethodService<CSharpGenerateMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
{
    protected override bool IsExplicitInterfaceGeneration(SyntaxNode node)
        => node is MethodDeclarationSyntax;

    protected override bool IsSimpleNameGeneration(SyntaxNode node)
        => node is SimpleNameSyntax;

    protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

    protected override AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, AbstractGenerateParameterizedMemberService<CSharpGenerateMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
        => new CSharpGenerateParameterizedMemberService<CSharpGenerateMethodService>.InvocationExpressionInfo(document, state);

    protected override bool AreSpecialOptionsActive(SemanticModel semanticModel)
        => CSharpCommonGenerationServiceMethods.AreSpecialOptionsActive();

    protected override bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel)
        => CSharpCommonGenerationServiceMethods.IsValidSymbol();

    protected override bool TryInitializeExplicitInterfaceState(
        SemanticDocument document,
        SyntaxNode node,
        CancellationToken cancellationToken,
        out SyntaxToken identifierToken,
        [NotNullWhen(true)] out IMethodSymbol? methodSymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn)
    {
        var methodDeclaration = (MethodDeclarationSyntax)node;
        identifierToken = methodDeclaration.Identifier;

        if (methodDeclaration.ExplicitInterfaceSpecifier != null &&
            !methodDeclaration.ParameterList.OpenParenToken.IsMissing &&
            !methodDeclaration.ParameterList.CloseParenToken.IsMissing)
        {
            var semanticModel = document.SemanticModel;
            methodSymbol = semanticModel.GetRequiredDeclaredSymbol(methodDeclaration, cancellationToken);
            if (methodSymbol != null && !methodSymbol.ExplicitInterfaceImplementations.Any())
            {
                var semanticInfo = semanticModel.GetTypeInfo(methodDeclaration.ExplicitInterfaceSpecifier.Name, cancellationToken);
                typeToGenerateIn = semanticInfo.Type as INamedTypeSymbol;
                return typeToGenerateIn != null;
            }
        }

        identifierToken = default;
        methodSymbol = null;
        typeToGenerateIn = null;
        return false;
    }

    protected override bool TryInitializeSimpleNameState(
        SemanticDocument document,
        SimpleNameSyntax simpleName,
        CancellationToken cancellationToken,
        out SyntaxToken identifierToken,
        [NotNullWhen(true)] out ExpressionSyntax? simpleNameOrMemberAccessExpression,
        out InvocationExpressionSyntax? invocationExpressionOpt,
        out bool isInConditionalAccessExpression)
    {
        identifierToken = simpleName.Identifier;

        var memberAccess = simpleName.GetRequiredParent() as MemberAccessExpressionSyntax;
        var (conditionalAccessExpression, invocation) =
            simpleName is { Parent: MemberBindingExpressionSyntax { Parent: InvocationExpressionSyntax { Parent: ConditionalAccessExpressionSyntax conditionalAccessExpression1 } invocation1 } memberBinding } &&
            conditionalAccessExpression1.WhenNotNull == invocation1 &&
            invocation1.Expression == memberBinding &&
            memberBinding.Name == simpleName ? (conditionalAccessExpression1, invocation1) : default;
        if (memberAccess != null)
        {
            simpleNameOrMemberAccessExpression = memberAccess;
        }
        else if (conditionalAccessExpression != null)
        {
            simpleNameOrMemberAccessExpression = conditionalAccessExpression;
        }
        else
        {
            simpleNameOrMemberAccessExpression = simpleName;
        }

        if (memberAccess == null || memberAccess.Name == simpleName)
        {
            if (simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression, out invocationExpressionOpt))
            {
                // want to look for anything of the form:  a?.B()   a?.B.C()    a?.B.C.D()   etc
                isInConditionalAccessExpression = invocationExpressionOpt.Parent is ConditionalAccessExpressionSyntax { WhenNotNull: var whenNotNull } &&
                    whenNotNull == invocationExpressionOpt;
                return !invocationExpressionOpt.ArgumentList.CloseParenToken.IsMissing;
            }

            if (conditionalAccessExpression != null)
            {
                invocationExpressionOpt = invocation;
                isInConditionalAccessExpression = true;
                return !invocationExpressionOpt.ArgumentList.CloseParenToken.IsMissing;
            }

            // If we don't have an invocation node, then see if we can infer a delegate in
            // this location. Check if this is a place where a delegate can go.  Only do this
            // for identifier names. for now.  It gets really funky if you have to deal with
            // a generic name here.
            if (simpleName is IdentifierNameSyntax &&
                !simpleNameOrMemberAccessExpression.IsLeftSideOfAnyAssignExpression())
            {
                invocationExpressionOpt = null;
                isInConditionalAccessExpression = conditionalAccessExpression != null;
                return true;
            }
        }

        identifierToken = default;
        simpleNameOrMemberAccessExpression = null;
        invocationExpressionOpt = null;
        isInConditionalAccessExpression = false;
        return false;
    }

    protected override ITypeSymbol? DetermineReturnTypeForSimpleNameOrMemberAccessExpression(
        ITypeInferenceService typeInferenceService,
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        if (semanticModel.SyntaxTree.IsNameOfContext(expression.SpanStart, semanticModel, cancellationToken))
        {
            return typeInferenceService.InferType(semanticModel, expression, true, cancellationToken);
        }

        return null;
    }
}
