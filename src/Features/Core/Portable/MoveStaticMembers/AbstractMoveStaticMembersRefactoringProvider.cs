// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MoveStaticMembers
{
    internal abstract class AbstractMoveStaticMembersRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IMoveStaticMembersOptionsService _service;

        protected abstract Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context);

        /// <summary>
        /// Test purpose only
        /// </summary>
        protected AbstractMoveStaticMembersRefactoringProvider(IMoveStaticMembersOptionsService service)
            => _service = service;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var selectedMemberNode = await GetSelectedNodeAsync(context).ConfigureAwait(false);
            if (selectedMemberNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedMember = semanticModel.GetDeclaredSymbol(selectedMemberNode);
            if (selectedMember == null || selectedMember.ContainingType == null || !selectedMember.IsStatic)
            {
                return;
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var selectedType = selectedMember.ContainingType;
            var selectedTypeDeclaration = selectedMemberNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration);

            var action = new MoveStaticMembersWithDialogCodeAction(document, span, _service, selectedType, selectedMember: selectedMember);

            context.RegisterRefactoring(action, selectedMemberNode.Span);
        }
    }
}
