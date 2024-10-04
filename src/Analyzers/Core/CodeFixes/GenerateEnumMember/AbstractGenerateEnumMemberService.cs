// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember;

internal abstract partial class AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax> :
    AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>, IGenerateEnumMemberService
    where TService : AbstractGenerateEnumMemberService<TService, TSimpleNameSyntax, TExpressionSyntax>
    where TSimpleNameSyntax : TExpressionSyntax
    where TExpressionSyntax : SyntaxNode
{
    protected AbstractGenerateEnumMemberService()
    {
    }

    protected abstract bool IsIdentifierNameGeneration(SyntaxNode node);
    protected abstract bool TryInitializeIdentifierNameState(SemanticDocument document, TSimpleNameSyntax identifierName, CancellationToken cancellationToken, out SyntaxToken identifierToken, out TExpressionSyntax simpleNameOrMemberAccessExpression);

    public async Task<ImmutableArray<CodeAction>> GenerateEnumMemberAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateEnumMember, cancellationToken))
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
            if (state == null)
            {
                return [];
            }

            return [new GenerateEnumMemberCodeAction(document, state)];
        }
    }
}
