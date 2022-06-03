// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
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
            => false;

        protected virtual string GetImplicitConversionDisplayText(State state)
            => string.Empty;

        protected virtual string GetExplicitConversionDisplayText(State state)
            => string.Empty;

        protected async ValueTask<ImmutableArray<CodeAction>> GetActionsAsync(Document document, State state, CodeAndImportGenerationOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var result);
            result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, fallbackOptions, isAbstract: false, generateProperty: false));

            // If we're trying to generate an instance method into an abstract class (but not a
            // static class or an interface), then offer to generate it abstractly.
            var canGenerateAbstractly = state.TypeToGenerateIn.IsAbstract &&
                !state.TypeToGenerateIn.IsStatic &&
                state.TypeToGenerateIn.TypeKind != TypeKind.Interface &&
                !state.IsStatic;

            if (canGenerateAbstractly)
                result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, fallbackOptions, isAbstract: true, generateProperty: false));

            var semanticFacts = document.Project.Solution.Workspace.Services.GetLanguageServices(state.TypeToGenerateIn.Language).GetService<ISemanticFactsService>();

            if (semanticFacts.SupportsParameterizedProperties &&
                state.InvocationExpressionOpt != null)
            {
                var typeParameters = state.SignatureInfo.DetermineTypeParameters(cancellationToken);
                var returnType = await state.SignatureInfo.DetermineReturnTypeAsync(cancellationToken).ConfigureAwait(false);

                if (typeParameters.Length == 0 && returnType.SpecialType != SpecialType.System_Void)
                {
                    result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, fallbackOptions, isAbstract: false, generateProperty: true));

                    if (canGenerateAbstractly)
                        result.Add(new GenerateParameterizedMemberCodeAction((TService)this, document, state, fallbackOptions, isAbstract: true, generateProperty: true));
                }
            }

            return result.ToImmutable();
        }
    }
}
