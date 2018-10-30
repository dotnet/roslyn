// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMembrUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp.QuickAction;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract class AbstractPullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var userSelectedNode = root.FindNode(context.Span);

            if(!IsUserSelectIdentifer(userSelectedNode, context))
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

            if (allTargetInterfaces.Count == 0 && allTargetClasses.Count == 0)
            {
                return;
            }

            if (userSelectNodeSymbol.Kind == SymbolKind.Method ||
                userSelectNodeSymbol.Kind == SymbolKind.Property ||
                userSelectNodeSymbol.Kind == SymbolKind.Event)
            {
                await AddPullUpMemberToClassRefactoringViaQuickAction(allTargetClasses, userSelectedNode, userSelectNodeSymbol, context);
                await AddPullUpMemberToInterfaceRefactoringViaQuickAction(allTargetInterfaces, userSelectedNode, userSelectNodeSymbol, context);
                AddPullUpMemberRefactoringViaDialogBox(userSelectNodeSymbol, context, root, semanticModel);
            }
            else if (userSelectNodeSymbol.Kind == SymbolKind.Field)
            {
                await AddPullUpMemberToClassRefactoringViaQuickAction(allTargetClasses, userSelectedNode, userSelectNodeSymbol, context);
                AddPullUpMemberRefactoringViaDialogBox(userSelectNodeSymbol, context, root, semanticModel);
            }
        }

        private List<INamedTypeSymbol> FindAllTargetInterfaces(INamedTypeSymbol selectedNodeOwnerSymbol, Solution solution, CancellationToken cancellationToken)
        {
            return selectedNodeOwnerSymbol.AllInterfaces.
                Where(@interface => IsSymbolValid(@interface, solution, cancellationToken)).ToList();
        }

        private List<INamedTypeSymbol> FindAllTargetBaseClasses(INamedTypeSymbol selectedNodeOwnerSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var allBasesClasses = new List<INamedTypeSymbol>();
            for (var @class = selectedNodeOwnerSymbol.BaseType; @class != null; @class = @class.BaseType)
            {
                if (IsSymbolValid(@class, solution, cancellationToken))
                {
                    allBasesClasses.Add(@class);
                }
            }
            return allBasesClasses;
        }

        private bool IsSymbolValid(INamedTypeSymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            return symbol.Locations.Any(location => location.IsInSource &&
            !solution.GetDocument(location.SourceTree).IsGeneratedCode(cancellationToken)) &&
            symbol.DeclaringSyntaxReferences.Length > 0;
        }

        protected async Task AddPullUpMemberToClassRefactoringViaQuickAction(
            List<INamedTypeSymbol> targetClasses,
            SyntaxNode userSelectedNode,
            ISymbol userSelectNodeSymbol,
            CodeRefactoringContext context)
        {
            var puller = context.Document.Project.LanguageServices.GetService<AbstractClassPullerWithQuickAction>();
            foreach (var eachClass in targetClasses)
            {
                var action = await puller.ComputeRefactoring(eachClass, context, userSelectedNode, userSelectNodeSymbol);
                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }

        protected async virtual Task AddPullUpMemberToInterfaceRefactoringViaQuickAction(
            List<INamedTypeSymbol> targetInterfaces,
            SyntaxNode userSelectedNode,
            ISymbol userSelectNodeSymbol,
            CodeRefactoringContext context)
        {
            var puller = new InterfacePullerWithQuickAction();
            foreach (var eachInterface in targetInterfaces)
            {
                var action = await puller.ComputeRefactoring(eachInterface, context, userSelectedNode, userSelectNodeSymbol);

                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }

        protected void AddPullUpMemberRefactoringViaDialogBox(
            ISymbol userSelectedNodeSymbol,
            CodeRefactoringContext context,
            SyntaxNode root,
            SemanticModel semanticModel)
        {
            var dialogAction = new PullMemberUpWithDialogCodeAction(semanticModel, context, userSelectedNodeSymbol);
            context.RegisterRefactoring(dialogAction);
        }

        internal abstract bool IsUserSelectIdentifer(SyntaxNode userSelectedNode, CodeRefactoringContext context);
    }
}
