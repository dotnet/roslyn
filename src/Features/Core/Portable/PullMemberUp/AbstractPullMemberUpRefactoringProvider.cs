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

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var userSelectedNode = root.FindNode(context.Span);

            if(!IsSelectionValid(context.Span, userSelectedNode))
            {
                return;
            }

            var userSelectNodeSymbol = semanticModel.GetDeclaredSymbol(userSelectedNode);
            if (userSelectNodeSymbol == null)
            {
                return;
            }

            var allTargetClasses = FindAllTargetBaseClasses(userSelectNodeSymbol.ContainingType, context.Document.Project.Solution, context.CancellationToken);
            var allTargetInterfaces = FindAllTargetInterfaces(userSelectNodeSymbol.ContainingType, context.Document.Project.Solution, context.CancellationToken);

            if (allTargetInterfaces.Count() == 0 && allTargetClasses.Count() == 0)
            {
                return;
            }

            if ((userSelectNodeSymbol is IMethodSymbol methodSymbol && methodSymbol.IsOrdinaryMethod()) ||
                userSelectNodeSymbol.Kind == SymbolKind.Property ||
                userSelectNodeSymbol.Kind == SymbolKind.Event ||
                userSelectNodeSymbol.Kind == SymbolKind.Field)
            {
                PullMemberUpViaQuickAction(allTargetClasses.Concat(allTargetInterfaces), userSelectNodeSymbol, context);
                AddPullUpMemberRefactoringViaDialogBox(userSelectNodeSymbol, context, semanticModel);
            }
        }

        private IEnumerable<INamedTypeSymbol> FindAllTargetInterfaces(INamedTypeSymbol selectedNodeOwnerSymbol, Solution solution, CancellationToken cancellationToken)
        {
            return selectedNodeOwnerSymbol.AllInterfaces.
                Where(@interface => @interface.DeclaringSyntaxReferences.Length > 0 && IsSymbolValid(solution, @interface, cancellationToken));
        }

        private IEnumerable<INamedTypeSymbol> FindAllTargetBaseClasses(INamedTypeSymbol selectedNodeOwnerSymbol, Solution solution, CancellationToken cancellationToken)
        {
            return selectedNodeOwnerSymbol.GetBaseTypes().Where(@class => @class.DeclaringSyntaxReferences.Length > 0 && IsSymbolValid(solution, @class, cancellationToken));
        }

        private bool IsSymbolValid(Solution solution, INamedTypeSymbol symbol, CancellationToken cancellationToken)
        {
            return symbol.Locations.Any(location => location.IsInSource &&
                   !solution.GetDocument(location.SourceTree).IsGeneratedCode(cancellationToken));
        }

        protected void PullMemberUpViaQuickAction(
            IEnumerable<INamedTypeSymbol> targets,
            ISymbol userSelectNodeSymbol,
            CodeRefactoringContext context)
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

                var action = puller.ComputeRefactoring(target, context.Document, userSelectNodeSymbol);
                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }

        protected void AddPullUpMemberRefactoringViaDialogBox(
            ISymbol userSelectedNodeSymbol,
            CodeRefactoringContext context,
            SemanticModel semanticModel)
        {
            var dialogAction = new PullMemberUpWithDialogCodeAction(semanticModel, context, userSelectedNodeSymbol, this);
            context.RegisterRefactoring(dialogAction);
        }

        protected abstract bool IsSelectionValid(TextSpan span, SyntaxNode userSelectedSyntax);
    }
}
