// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        protected abstract Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context);

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

            var selectedMemberNode = await GetSelectedNodeAsync(context).ConfigureAwait(false);
            if (selectedMemberNode == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedMember = semanticModel.GetDeclaredSymbol(selectedMemberNode);
            if (selectedMember == null || selectedMember.ContainingType == null)
            {
                return;
            }

            if (!MemberAndDestinationValidator.IsMemberValid(selectedMember))
            {
                return;
            }

            var allDestinations = FindAllValidDestinations(
                selectedMember,
                document.Project.Solution,
                cancellationToken);
            if (allDestinations.Length == 0)
            {
                return;
            }

            var allActions = allDestinations.Select(destination => MembersPuller.TryComputeCodeAction(document, selectedMember, destination))
                .WhereNotNull().Concat(new PullMemberUpWithDialogCodeAction(document, selectedMember, _service))
                .ToImmutableArray();

            var nestedCodeAction = new CodeActionWithNestedActions(
                string.Format(FeaturesResources.Pull_0_up, selectedMember.ToNameDisplayString()),
                allActions, isInlinable: true);
            context.RegisterRefactoring(nestedCodeAction, selectedMemberNode.Span);
        }

        private static ImmutableArray<INamedTypeSymbol> FindAllValidDestinations(
            ISymbol selectedMember,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var containingType = selectedMember.ContainingType;
            var allDestinations = selectedMember.IsKind(SymbolKind.Field)
                ? containingType.GetBaseTypes().ToImmutableArray()
                : containingType.AllInterfaces.Concat(containingType.GetBaseTypes()).ToImmutableArray();

            return allDestinations.WhereAsArray(destination => MemberAndDestinationValidator.IsDestinationValid(solution, destination, cancellationToken));
        }
    }
}
