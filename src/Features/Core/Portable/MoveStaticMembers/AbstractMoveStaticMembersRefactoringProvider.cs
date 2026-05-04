// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveStaticMembers;

internal abstract class AbstractMoveStaticMembersRefactoringProvider : CodeRefactoringProvider
{
    protected abstract Task<ImmutableArray<SyntaxNode>> GetSelectedNodesAsync(CodeRefactoringContext context);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;

        var service = document.Project.Solution.Services.GetService<IMoveStaticMembersOptionsService>();
        if (service == null)
        {
            return;
        }

        var selectedMemberNodes = await GetSelectedNodesAsync(context).ConfigureAwait(false);
        if (selectedMemberNodes.IsEmpty)
        {
            return;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var memberNodeSymbolPairs = selectedMemberNodes
            .SelectAsArray(m => (node: m, symbol: semanticModel.GetDeclaredSymbol(m, cancellationToken)))
            // Use same logic as pull members up for determining if a selected member
            // is valid to be moved into a base
            .WhereAsArray(pair => MemberAndDestinationValidator.IsMemberValid(pair.symbol) && pair.symbol.IsStatic);

        if (memberNodeSymbolPairs.IsEmpty)
        {
            return;
        }

        var selectedMembers = memberNodeSymbolPairs.SelectAsArray(pair => pair.symbol!);

        var containingType = selectedMembers.First().ContainingType;
        Contract.ThrowIfNull(containingType);
        if (selectedMembers.Any(m => !m.ContainingType.Equals(containingType)))
        {
            return;
        }

        // Don't offer refactoring for enum members
        if (containingType.TypeKind == TypeKind.Enum)
        {
            return;
        }

        // we want to use a span which covers all the selected viable member nodes, so that more specific nodes have priority
        var memberSpan = TextSpan.FromBounds(
            memberNodeSymbolPairs.First().node.FullSpan.Start,
            memberNodeSymbolPairs.Last().node.FullSpan.End);

        var action = new MoveStaticMembersWithDialogCodeAction(document, service, containingType, selectedMembers);

        context.RegisterRefactoring(action, memberSpan);
    }
}
