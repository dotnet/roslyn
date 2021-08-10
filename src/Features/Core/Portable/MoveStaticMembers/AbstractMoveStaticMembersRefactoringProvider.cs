// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MoveStaticMembers
{
    internal abstract class AbstractMoveStaticMembersRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IMoveStaticMembersOptionsService? _service;

        protected abstract Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context);

        /// <summary>
        /// Test purpose only
        /// </summary>
        protected AbstractMoveStaticMembersRefactoringProvider(IMoveStaticMembersOptionsService? service)
            => _service = service;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var memberDeclaration = await GetSelectedNodeAsync(context).ConfigureAwait(false);
            if (memberDeclaration == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            var selectedType = semanticModel.GetEnclosingNamedType(span.Start, cancellationToken);
            if (selectedType == null)
            {
                return;
            }

            var selectedMembers = selectedType.GetMembers()
                .WhereAsArray(m => m.IsStatic &&
                    MemberAndDestinationValidator.IsMemberValid(m) &&
                    m.DeclaringSyntaxReferences.Any(sr => memberDeclaration.FullSpan.Contains(sr.Span)));
            if (selectedMembers.IsEmpty)
            {
                return;
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var action = new MoveStaticMembersWithDialogCodeAction(document, span, _service, selectedType, selectedMember: selectedMembers[0]);

            context.RegisterRefactoring(action, selectedMembers[0].DeclaringSyntaxReferences[0].Span);
        }
    }
}
