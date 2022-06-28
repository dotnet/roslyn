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
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract partial class AbstractPullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        private IPullMemberUpOptionsService? _service;

        protected abstract Task<ImmutableArray<SyntaxNode>> GetSelectedNodesAsync(CodeRefactoringContext context);

        /// <summary>
        /// Test purpose only
        /// </summary>
        protected AbstractPullMemberUpRefactoringProvider(IPullMemberUpOptionsService? service)
            => _service = service;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var (document, _, cancellationToken) = context;

            _service ??= document.Project.Solution.Workspace.Services.GetService<IPullMemberUpOptionsService>();
            if (_service == null)
            {
                return;
            }

            var selectedMemberNodes = await GetSelectedNodesAsync(context).ConfigureAwait(false);
            if (selectedMemberNodes.IsEmpty)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedMembers = selectedMemberNodes
                .Select(memberNode => semanticModel.GetDeclaredSymbol(memberNode))
                .WhereNotNull()
                .Where(memberNode => MemberAndDestinationValidator.IsMemberValid(memberNode))
                .AsImmutable();

            if (selectedMembers.IsEmpty)
            {
                return;
            }

            var containingType = selectedMembers.First().ContainingType;
            if (containingType == null || selectedMembers.Any(m => m.ContainingType != containingType))
            {
                return;
            }

            var allDestinations = FindAllValidDestinations(
                selectedMembers,
                containingType,
                document.Project.Solution,
                cancellationToken);
            if (allDestinations.Length == 0)
            {
                return;
            }

            var allActions = allDestinations.Select(destination => MembersPuller.TryComputeCodeAction(document, selectedMembers, destination, context.Options))
            .WhereNotNull().Concat(new PullMemberUpWithDialogCodeAction(document, selectedMembers, _service, context.Options))
            .ToImmutableArray();

            var title = selectedMembers.IsSingle() ?
                string.Format(FeaturesResources.Pull_0_up, selectedMembers.Single().ToNameDisplayString()) :
                FeaturesResources.Pull_selected_members_up;

            var nestedCodeAction = CodeActionWithNestedActions.Create(
                title,
                allActions, isInlinable: true);

            context.RegisterRefactoring(nestedCodeAction, context.Span);
        }

        private static ImmutableArray<INamedTypeSymbol> FindAllValidDestinations(
            ImmutableArray<ISymbol> selectedMembers,
            INamedTypeSymbol containingType,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var allDestinations = selectedMembers.All(m => m.IsKind(SymbolKind.Field))
                ? containingType.GetBaseTypes().ToImmutableArray()
                : containingType.AllInterfaces.Concat(containingType.GetBaseTypes()).ToImmutableArray();

            return allDestinations.WhereAsArray(destination => MemberAndDestinationValidator.IsDestinationValid(solution, destination, cancellationToken));
        }
    }
}
