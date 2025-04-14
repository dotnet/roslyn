// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

internal abstract partial class AbstractGenerateDeconstructMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
    AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>,
    IGenerateDeconstructMemberService
    where TService : AbstractGenerateDeconstructMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    where TSimpleNameSyntax : TExpressionSyntax
    where TExpressionSyntax : SyntaxNode
    where TInvocationExpressionSyntax : TExpressionSyntax
{
    public abstract ImmutableArray<IParameterSymbol> TryMakeParameters(SemanticModel semanticModel, SyntaxNode target, CancellationToken cancellationToken);

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

            return state != null ? await GetActionsAsync(document, state, cancellationToken).ConfigureAwait(false) : [];
        }
    }
}
