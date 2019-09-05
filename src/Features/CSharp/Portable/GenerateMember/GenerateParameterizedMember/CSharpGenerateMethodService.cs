// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod
{
    [ExportLanguageService(typeof(IGenerateParameterizedMemberService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpGenerateMethodService :
        AbstractGenerateMethodService<CSharpGenerateMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpGenerateMethodService()
        {
        }

        protected override bool IsExplicitInterfaceGeneration(SyntaxNode node)
            => node is MethodDeclarationSyntax;

        protected override bool IsSimpleNameGeneration(SyntaxNode node)
            => node is SimpleNameSyntax;

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
            => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

        protected override AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, AbstractGenerateParameterizedMemberService<CSharpGenerateMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
        {
            return new CSharpGenerateParameterizedMemberService<CSharpGenerateMethodService>.InvocationExpressionInfo(document, state);
        }

        protected override bool AreSpecialOptionsActive(SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.AreSpecialOptionsActive(semanticModel);

        protected override bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.IsValidSymbol(symbol, semanticModel);

        protected override bool TryInitializeExplicitInterfaceState(
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken identifierToken,
            out IMethodSymbol methodSymbol,
            out INamedTypeSymbol typeToGenerateIn)
        {
            var methodDeclaration = (MethodDeclarationSyntax)node;
            identifierToken = methodDeclaration.Identifier;

            if (methodDeclaration.ExplicitInterfaceSpecifier != null &&
                !methodDeclaration.ParameterList.OpenParenToken.IsMissing &&
                !methodDeclaration.ParameterList.CloseParenToken.IsMissing)
            {
                var semanticModel = document.SemanticModel;
                methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) as IMethodSymbol;
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
            out ExpressionSyntax simpleNameOrMemberAccessExpression,
            out InvocationExpressionSyntax invocationExpressionOpt,
            out bool isInConditionalAccessExpression)
        {
            identifierToken = simpleName.Identifier;

            var memberAccess = simpleName?.Parent as MemberAccessExpressionSyntax;
            var conditionalMemberAccess = simpleName?.Parent?.Parent?.Parent as ConditionalAccessExpressionSyntax;
            var inConditionalMemberAccess = conditionalMemberAccess != null;
            if (memberAccess != null)
            {
                simpleNameOrMemberAccessExpression = memberAccess;
            }
            else if (inConditionalMemberAccess)
            {
                simpleNameOrMemberAccessExpression = conditionalMemberAccess;
            }
            else
            {
                simpleNameOrMemberAccessExpression = simpleName;
            }

            if (memberAccess == null || memberAccess.Name == simpleName)
            {
                if (simpleNameOrMemberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression))
                {
                    invocationExpressionOpt = (InvocationExpressionSyntax)simpleNameOrMemberAccessExpression.Parent;
                    isInConditionalAccessExpression = inConditionalMemberAccess;
                    return !invocationExpressionOpt.ArgumentList.CloseParenToken.IsMissing;
                }
                // We need to check that the tree is structured like so:
                // ConditionalAccessExpressionSyntax
                //    ->  InvocationExpressionSyntax
                //          ->   MemberBindingExpressionSyntax
                // and that the name at the end of this expression matches the simple name we were given
                else if ((((simpleNameOrMemberAccessExpression as ConditionalAccessExpressionSyntax)
                           ?.WhenNotNull as InvocationExpressionSyntax)
                                ?.Expression as MemberBindingExpressionSyntax)
                                    ?.Name == simpleName)
                {
                    invocationExpressionOpt = (InvocationExpressionSyntax)((ConditionalAccessExpressionSyntax)simpleNameOrMemberAccessExpression).WhenNotNull;
                    isInConditionalAccessExpression = inConditionalMemberAccess;
                    return !invocationExpressionOpt.ArgumentList.CloseParenToken.IsMissing;
                }
                else if (simpleName.IsKind(SyntaxKind.IdentifierName))
                {
                    // If we don't have an invocation node, then see if we can infer a delegate in
                    // this location. Check if this is a place where a delegate can go.  Only do this
                    // for identifier names. for now.  It gets really funky if you have to deal with
                    // a generic name here.

                    // Can't assign into a method.
                    if (!simpleNameOrMemberAccessExpression.IsLeftSideOfAnyAssignExpression())
                    {
                        invocationExpressionOpt = null;
                        isInConditionalAccessExpression = inConditionalMemberAccess;
                        return true;
                    }
                }
            }

            identifierToken = default;
            simpleNameOrMemberAccessExpression = null;
            invocationExpressionOpt = null;
            isInConditionalAccessExpression = false;
            return false;
        }

        protected override ITypeSymbol DetermineReturnTypeForSimpleNameOrMemberAccessExpression(
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
}
