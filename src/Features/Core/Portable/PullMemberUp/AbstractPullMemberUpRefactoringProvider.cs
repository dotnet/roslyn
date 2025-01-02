// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;

internal abstract partial class AbstractPullMemberUpRefactoringProvider(IPullMemberUpOptionsService? service) : CodeRefactoringProvider
{
    private IPullMemberUpOptionsService? _service = service;

    protected abstract Task<ImmutableArray<SyntaxNode>> GetSelectedNodesAsync(CodeRefactoringContext context);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        // Currently support to pull field, method, event, property and indexer up,
        // constructor, operator and finalizer are excluded.
        var (document, _, cancellationToken) = context;

        _service ??= document.Project.Solution.Services.GetService<IPullMemberUpOptionsService>();
        if (_service == null)
            return;

        var selectedMemberNodes = await GetSelectedNodesAsync(context).ConfigureAwait(false);
        if (selectedMemberNodes.IsEmpty)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var memberNodeSymbolPairs = selectedMemberNodes
            .SelectAsArray(m => (node: m, symbol: semanticModel.GetRequiredDeclaredSymbol(m, cancellationToken)))
            .WhereAsArray(pair => MemberAndDestinationValidator.IsMemberValid(pair.symbol));

        if (memberNodeSymbolPairs.IsEmpty)
            return;

        var selectedMembers = memberNodeSymbolPairs.SelectAsArray(pair => pair.symbol);

        var containingType = selectedMembers.First().ContainingType;
        Contract.ThrowIfNull(containingType);
        if (selectedMembers.Any(m => !m.ContainingType.Equals(containingType)))
            return;

        var allDestinations = FindAllValidDestinations(
            selectedMembers,
            containingType,
            document.Project.Solution,
            cancellationToken);
        if (allDestinations.Length == 0)
            return;

        context.RegisterRefactoring(CodeAction.Create(
                selectedMembers.IsSingle()
                    ? string.Format(FeaturesResources.Pull_0_up_to, selectedMembers.Single().ToNameDisplayString())
                    : FeaturesResources.Pull_selected_members_up,
                [
                    .. allDestinations.Select(destination => MembersPuller.TryComputeCodeAction(document, selectedMembers, destination))
                        .WhereNotNull()
,
                    new PullMemberUpWithDialogCodeAction(document, selectedMembers, _service),
                ],
                isInlinable: false),
            // we want to use a span which covers all the selected viable member nodes, so that more specific nodes have priority
            TextSpan.FromBounds(
                memberNodeSymbolPairs.First().node.FullSpan.Start,
                memberNodeSymbolPairs.Last().node.FullSpan.End));
    }

    private static ImmutableArray<INamedTypeSymbol> FindAllValidDestinations(
        ImmutableArray<ISymbol> selectedMembers,
        INamedTypeSymbol containingType,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var allDestinations = selectedMembers.All(m => m.IsKind(SymbolKind.Field))
            ? containingType.GetBaseTypes().ToImmutableArray()
            : [.. containingType.AllInterfaces, .. containingType.GetBaseTypes()];

        return allDestinations.WhereAsArray(destination => MemberAndDestinationValidator.IsDestinationValid(solution, destination, cancellationToken));
    }
}
