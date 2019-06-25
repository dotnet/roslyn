// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal abstract partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
        AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>
        where TService : AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
        where TSimpleNameSyntax : TExpressionSyntax
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
    {
        protected AbstractGenerateParameterizedMemberService()
        {
        }

        protected abstract AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, State abstractState);

        protected abstract bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel);
        protected abstract bool AreSpecialOptionsActive(SemanticModel semanticModel);

        protected virtual bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
        {
            return false;
        }

        protected virtual string GetImplicitConversionDisplayText(State state)
        {
            return string.Empty;
        }

        protected virtual string GetExplicitConversionDisplayText(State state)
        {
            return string.Empty;
        }

        protected ImmutableArray<CodeAction> GetActions(Document document, State state, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<CodeAction>.GetInstance();
            result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: false, generateProperty: false));

            // If we're trying to generate an instance method into an abstract class (but not a
            // static class or an interface), then offer to generate it abstractly.
            var canGenerateAbstractly = state.TypeToGenerateIn.IsAbstract &&
                !state.TypeToGenerateIn.IsStatic &&
                state.TypeToGenerateIn.TypeKind != TypeKind.Interface &&
                !state.IsStatic;

            if (canGenerateAbstractly)
            {
                result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: true, generateProperty: false));
            }

            var semanticFacts = document.Project.Solution.Workspace.Services.GetLanguageServices(state.TypeToGenerateIn.Language).GetService<ISemanticFactsService>();

            if (semanticFacts.SupportsParameterizedProperties &&
                state.InvocationExpressionOpt != null)
            {
                var typeParameters = state.SignatureInfo.DetermineTypeParameters(cancellationToken);
                var returnType = state.SignatureInfo.DetermineReturnType(cancellationToken);

                if (typeParameters.Length == 0 && returnType.SpecialType != SpecialType.System_Void)
                {
                    result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: false, generateProperty: true));

                    if (canGenerateAbstractly)
                    {
                        result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, isAbstract: true, generateProperty: true));
                    }
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}
