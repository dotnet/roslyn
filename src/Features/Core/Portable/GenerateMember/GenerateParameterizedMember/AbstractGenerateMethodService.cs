// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal abstract partial class AbstractGenerateMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
        AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>, IGenerateParameterizedMemberService
        where TService : AbstractGenerateMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
        where TSimpleNameSyntax : TExpressionSyntax
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
    {
        protected abstract bool IsSimpleNameGeneration(SyntaxNode node);
        protected abstract bool IsExplicitInterfaceGeneration(SyntaxNode node);
        protected abstract bool TryInitializeExplicitInterfaceState(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken, out SyntaxToken identifierToken, out IMethodSymbol methodSymbol, out INamedTypeSymbol typeToGenerateIn);
        protected abstract bool TryInitializeSimpleNameState(SemanticDocument document, TSimpleNameSyntax simpleName, CancellationToken cancellationToken, out SyntaxToken identifierToken, out TExpressionSyntax simpleNameOrMemberAccessExpression, out TInvocationExpressionSyntax invocationExpressionOpt, out bool isInConditionalExpression);
        protected abstract ITypeSymbol CanGenerateMethodForSimpleNameOrMemberAccessExpression(ITypeInferenceService typeInferenceService, SemanticModel semanticModel, TExpressionSyntax expression, CancellationToken cancellationToken);

        public async Task<IEnumerable<CodeAction>> GenerateMethodAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateMethod, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                var state = await State.GenerateMethodStateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state == null)
                {
                    return SpecializedCollections.EmptyEnumerable<CodeAction>();
                }

                return GetActions(document, state, cancellationToken);
            }
        }
    }
}
