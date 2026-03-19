// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

internal abstract partial class AbstractGenerateConversionService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax> :
    AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>, IGenerateConversionService
    where TService : AbstractGenerateConversionService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    where TSimpleNameSyntax : TExpressionSyntax
    where TExpressionSyntax : SyntaxNode
    where TInvocationExpressionSyntax : TExpressionSyntax
{
    protected abstract bool IsImplicitConversionGeneration(SyntaxNode node);
    protected abstract bool IsExplicitConversionGeneration(SyntaxNode node);
    protected abstract bool TryInitializeImplicitConversionState(SemanticDocument document, SyntaxNode expression, ISet<TypeKind> classInterfaceModuleStructTypes, CancellationToken cancellationToken, out SyntaxToken identifierToken, [NotNullWhen(true)] out IMethodSymbol? methodSymbol, [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn);
    protected abstract bool TryInitializeExplicitConversionState(SemanticDocument document, SyntaxNode expression, ISet<TypeKind> classInterfaceModuleStructTypes, CancellationToken cancellationToken, out SyntaxToken identifierToken, [NotNullWhen(true)] out IMethodSymbol? methodSymbol, [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn);

    public async Task<ImmutableArray<CodeAction>> GenerateConversionAsync(
        Document document,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateMethod, cancellationToken))
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var state = await State.GenerateConversionStateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
            if (state == null)
            {
                return [];
            }

            return await GetActionsAsync(document, state, cancellationToken).ConfigureAwait(false);
        }
    }
}
