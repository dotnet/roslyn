// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MoveStaticMembers
{
    internal abstract class AbstractMoveStaticMembersRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var service = document.Project.Solution.Workspace.Services.GetService<IMoveStaticMembersOptionsService>();
            if (service == null)
            {
                return;
            }

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

            var selectedMember = semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);
            if (selectedMember?.ContainingType is null || !selectedMember.IsStatic || !MemberAndDestinationValidator.IsMemberValid(selectedMember))
            {
                return;
            }

            var action = new MoveStaticMembersWithDialogCodeAction(document, memberDeclaration.Span, service, selectedMember.ContainingType, context.Options, selectedMember: selectedMember);

            context.RegisterRefactoring(action, memberDeclaration.Span);
        }
    }
}
