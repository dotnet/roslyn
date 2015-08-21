// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateEnumMember
{
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

        public async Task<IEnumerable<CodeAction>> GenerateEnumMemberAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateMember_GenerateEnumMember, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state == null)
                {
                    return SpecializedCollections.EmptyEnumerable<CodeAction>();
                }

                return GetActions(document, state);
            }
        }

        private IEnumerable<CodeAction> GetActions(Document document, State state)
        {
            yield return new GenerateEnumMemberCodeAction((TService)this, document, state);
        }
    }
}
