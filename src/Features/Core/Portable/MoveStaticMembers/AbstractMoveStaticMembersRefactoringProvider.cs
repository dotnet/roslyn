// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MoveStaticMembers
{
    internal abstract class AbstractMoveStaticMembersRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract Task<ImmutableArray<SyntaxNode>> GetSelectedNodesAsync(CodeRefactoringContext context);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var service = document.Project.Solution.Workspace.Services.GetService<IMoveStaticMembersOptionsService>();
            if (service == null)
            {
                return;
            }

            var selectedMemberNodes = await GetSelectedNodesAsync(context).ConfigureAwait(false);
            if (selectedMemberNodes == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            var memberNodeSymbolPairs = selectedMemberNodes
                .Select(m => (node: m, symbol: semanticModel.GetDeclaredSymbol(m, cancellationToken)))
                // Use same logic as pull members up for determining if a selected member
                // is valid to be moved into a base
                .Where(pair => pair.symbol != null && MemberAndDestinationValidator.IsMemberValid(pair.symbol) && pair.symbol.IsStatic)
                .AsImmutable();

            var selectedMembers = memberNodeSymbolPairs.SelectAsArray(pair => pair.symbol!);

            if (selectedMembers.IsEmpty)
            {
                return;
            }

            var containingType = selectedMembers.First().ContainingType;
            if (containingType == null || selectedMembers.Any(static (m, containingType) => m.ContainingType != containingType, containingType))
            {
                return;
            }

            // we want to use a span which covers all the selected viable member nodes, so that more specific nodes have priority
            var memberSpan = new SyntaxList<SyntaxNode>(memberNodeSymbolPairs.Select(pair => pair.node)).FullSpan;

            var action = new MoveStaticMembersWithDialogCodeAction(document, service, containingType, context.Options, selectedMembers);

            context.RegisterRefactoring(action, memberSpan);
        }
    }
}
