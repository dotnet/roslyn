// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp.QuickAction;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract partial class AbstractPullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        protected readonly IPullMemberUpOptionsService _pullMemberUpOptionsService;

        protected AbstractPullMemberUpRefactoringProvider(IPullMemberUpOptionsService pullMemberUpService)
        {
            _pullMemberUpOptionsService = pullMemberUpService;
        }

        protected abstract bool IsSelectionValid(TextSpan span, SyntaxNode userSelectedSyntax);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var userSelectedNode = root.FindNode(context.Span);

            if (!IsSelectionValid(context.Span, userSelectedNode))
            {
                return;
            }

            var userSelectNodeSymbol = semanticModel.GetDeclaredSymbol(userSelectedNode);
            if (userSelectNodeSymbol == null || userSelectNodeSymbol.ContainingType == null)
            {
                return;
            }

            var allTargets = FindAllValidTargets(userSelectNodeSymbol.ContainingType, context.Document.Project.Solution, context.CancellationToken);

            if (allTargets.Count() == 0)
            {
                return;
            }

            if ((userSelectNodeSymbol is IMethodSymbol methodSymbol && methodSymbol.IsOrdinaryMethod()) ||
                userSelectNodeSymbol.Kind == SymbolKind.Property ||
                userSelectNodeSymbol.Kind == SymbolKind.Event ||
                userSelectNodeSymbol.Kind == SymbolKind.Field)
            {
                PullMemberUpViaQuickAction(context, userSelectNodeSymbol, allTargets);
                AddPullUpMemberRefactoringViaDialogBox(context, semanticModel, userSelectNodeSymbol);
            }
        }

        private IEnumerable<INamedTypeSymbol> FindAllValidTargets(
            INamedTypeSymbol selectedNodeOwnerSymbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return selectedNodeOwnerSymbol.AllInterfaces.Concat(selectedNodeOwnerSymbol.GetBaseTypes()).
                Where(baseType => baseType != null &&
                    baseType.DeclaringSyntaxReferences.Length > 0 &&
                    IsLocationValid(baseType, solution, cancellationToken));
        }

        private bool IsLocationValid(INamedTypeSymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            return symbol.Locations.Any(location => location.IsInSource &&
                !solution.GetDocument(location.SourceTree).IsGeneratedCode(cancellationToken));
        }

        private void PullMemberUpViaQuickAction(
            CodeRefactoringContext context,
            ISymbol userSelectNodeSymbol,
            IEnumerable<INamedTypeSymbol> targets)
        {
            foreach (var target in targets)
            {
                AbstractMemberPullerWithQuickAction puller = default;
                if (target.TypeKind == TypeKind.Interface &&
                    userSelectNodeSymbol.Kind != SymbolKind.Field)
                {
                    puller = new InterfacePullerWithQuickAction();
                }
                else
                {
                    puller = new ClassPullerWithQuickAction();
                }

                var action = puller.ComputeRefactoring(context.Document, userSelectNodeSymbol, target);
                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }

        private void AddPullUpMemberRefactoringViaDialogBox(
            CodeRefactoringContext context,
            SemanticModel semanticModel,
            ISymbol userSelectedNodeSymbol)
        {
            var dialogAction = new PullMemberUpWithDialogCodeAction(context.Document, semanticModel, userSelectedNodeSymbol, this);
            context.RegisterRefactoring(dialogAction);
        }
    }
}
