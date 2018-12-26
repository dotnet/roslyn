// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
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

        protected abstract bool IsSelectionValid(TextSpan span, SyntaxNode selectedMemberNode);

        protected abstract Task<SyntaxNode> GetMatchedNodeAsync(Document document, TextSpan span);

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
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var selectedMemberNode = await GetMatchedNodeAsync(document, context.Span).ConfigureAwait(false);

            if (selectedMemberNode == null)
            {
                return;
            }

            var selectedMember = semanticModel.GetDeclaredSymbol(selectedMemberNode);
            if (selectedMember == null || selectedMember.ContainingType == null)
            {
                return;
            }

            if (!MemberAndDestinationValidator.IsMemberValid(selectedMember))
            {
                return;
            }

            if (selectedMember is IMethodSymbol methodSymbol && !methodSymbol.IsOrdinaryMethod())
            {
                return;
            }

            if (!IsSelectionValid(context.Span, selectedMemberNode))
            {
                return;
            }

            var allDestinations = FindAllValidDestinations(
                selectedMember,
                document.Project.Solution,
                context.CancellationToken);
            if (allDestinations.Length == 0)
            {
                return;
            }

            var allActions = allDestinations.Select(
                destination => MembersPuller.TryComputeCodeAction(document, selectedMember, destination)).
                WhereNotNull().Concat(new PullMemberUpWithDialogCodeAction(document, selectedMember, this)).
                ToImmutableArray();

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
