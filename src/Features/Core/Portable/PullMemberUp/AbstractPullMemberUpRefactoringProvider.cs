// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract partial class AbstractPullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IPullMemberUpOptionsService _service;
        private const int None = 0;

        protected abstract Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context);

        /// <summary>
        /// Test purpose only
        /// </summary>
        protected AbstractPullMemberUpRefactoringProvider(IPullMemberUpOptionsService service)
        {
            _service = service;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var (document, _, cancellationToken) = context;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var selectedMemberNode = await GetSelectedNodeAsync(context).ConfigureAwait(false);
            if (selectedMemberNode == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
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
            context.RegisterRefactoring(nestedCodeAction);
        }

        private ImmutableArray<INamedTypeSymbol> FindAllValidDestinations(
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
