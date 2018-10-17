// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal abstract class AbstractPullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var userSelectedNode = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false)).FindNode(context.Span);

            if(!IsUserSelectIdentifer(userSelectedNode, context))
            {
                return;
            }

            var userSelectedNodeSymbol = semanticModel.GetDeclaredSymbol(userSelectedNode);
            if (userSelectedNodeSymbol == null)
            {
                return;
            }

            var allTargetClasses = FindAllTargetBaseClasses(userSelectedNodeSymbol.ContainingType);
            var allTargetInterfaces = FindAllTargetInterfaces(userSelectedNodeSymbol.ContainingType);

            if (allTargetInterfaces.Count == 0 && allTargetClasses.Count == 0)
            {
                return;
            }

            if (userSelectedNodeSymbol.Kind == SymbolKind.Method ||
                userSelectedNodeSymbol.Kind == SymbolKind.Property ||
                userSelectedNodeSymbol.Kind == SymbolKind.Event)
            {
                AddPullUpMemberToClassRefactoringViaQuickAction(allTargetClasses, userSelectedNode, semanticModel, context);
                AddPullUpMemberToInterfaceRefactoringViaQuickAction(allTargetInterfaces, userSelectedNode, semanticModel, context);
                AddPullUpMemberRefactoringViaDialogBox(userSelectedNodeSymbol, context, semanticModel);
            }
            else if (userSelectedNodeSymbol.Kind == SymbolKind.Field)
            {
                AddPullUpMemberToClassRefactoringViaQuickAction(allTargetClasses, userSelectedNode, semanticModel, context);
                AddPullUpMemberRefactoringViaDialogBox(userSelectedNodeSymbol, context, semanticModel);
            }
        }

        private List<INamedTypeSymbol> FindAllTargetInterfaces(INamedTypeSymbol selectedNodeOwnerSymbol)
        {
            return selectedNodeOwnerSymbol.AllInterfaces.Where(eachInterface => eachInterface.DeclaringSyntaxReferences.Length > 0).ToList();
        }

        private List<INamedTypeSymbol> FindAllTargetBaseClasses(INamedTypeSymbol selectedNodeOwnerSymbol)
        {
            var allBasesClasses = new List<INamedTypeSymbol>();
            for (var @class = selectedNodeOwnerSymbol.BaseType; @class != null; @class = @class.BaseType)
            {
                if (selectedNodeOwnerSymbol.BaseType.DeclaringSyntaxReferences.Length > 0)
                {
                    allBasesClasses.Add(selectedNodeOwnerSymbol.BaseType);
                }
            }
            return allBasesClasses;
        }

        protected abstract void AddPullUpMemberToInterfaceRefactoringViaQuickAction(List<INamedTypeSymbol> allTargetInterfaces, SyntaxNode userSelectedNode, SemanticModel semanticModel, CodeRefactoringContext context);

        protected abstract void AddPullUpMemberToClassRefactoringViaQuickAction(List<INamedTypeSymbol> allTargetClasses, SyntaxNode userSelectedNode, SemanticModel semanticModel, CodeRefactoringContext context);

        protected abstract void AddPullUpMemberRefactoringViaDialogBox(ISymbol userSelectedNodeSymbol, CodeRefactoringContext context, SemanticModel semanticModel);

        internal abstract bool IsUserSelectIdentifer(SyntaxNode userSelectedNode, CodeRefactoringContext context);
    }
}
