// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal abstract partial class AbstractGenerateDeconstructMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
        AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>,
        IGenerateDeconstructMemberService
        where TService : AbstractGenerateDeconstructMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
        where TSimpleNameSyntax : TExpressionSyntax
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
    {
        public async Task<ImmutableArray<CodeAction>> GenerateDeconstructMethodAsync(
            Document document,
            SyntaxNode leftSide,
            INamedTypeSymbol typeToGenerateIn,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateMethod, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = await State.GenerateDeconstructMethodStateAsync(
                    (TService)this, semanticDocument, leftSide, typeToGenerateIn, cancellationToken).ConfigureAwait(false);

                return state != null ? GetActions(document, state, cancellationToken) : ImmutableArray<CodeAction>.Empty;
            }
        }
    }
}
