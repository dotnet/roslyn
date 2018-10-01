// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal abstract class AbstractPushMemberUpCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected SemanticModel SemanticModel { get; set; }

        protected CodeRefactoringContext Context { get; set; }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            Context = context;
            SemanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var userSelectedNode = root.FindNode(context.Span);

            if(!IsUserSelectIdentifer(userSelectedNode))
            {
                return;
            }

            var userSelectedNodeSymbol = SemanticModel.GetDeclaredSymbol(userSelectedNode);
            if (userSelectedNodeSymbol == null)
            {
                return;
            }

            var allTargetClasses = FindAllTargetBaseClasses(userSelectedNodeSymbol.ContainingType);
            var allTargetInterfaces = FindAllTargetInterfaces(userSelectedNodeSymbol.ContainingType);
                
            if (userSelectedNodeSymbol.Kind == SymbolKind.Method ||
                userSelectedNodeSymbol.Kind == SymbolKind.Property ||
                userSelectedNodeSymbol.Kind == SymbolKind.Event)
            {
                ProcessingClassesRefactoring(allTargetClasses, userSelectedNode);
                ProcessingInterfacesRefactoring(allTargetInterfaces, userSelectedNode);
            }
            else if (userSelectedNodeSymbol.Kind == SymbolKind.Field)
            {
                ProcessingClassesRefactoring(allTargetClasses, userSelectedNode);
            }
        }

        private IEnumerable<INamedTypeSymbol> FindAllTargetInterfaces(INamedTypeSymbol selectedNodeOwnerSymbol)
        {
            return selectedNodeOwnerSymbol.AllInterfaces.Where(eachInterface => eachInterface.DeclaringSyntaxReferences.Length > 0);
        }

        private IEnumerable<INamedTypeSymbol> FindAllTargetBaseClasses(INamedTypeSymbol selectedNodeOwnerSymbol)
        {
            var allBasesClasses = new List<INamedTypeSymbol>();
            while (selectedNodeOwnerSymbol.BaseType != null)
            {
                if (selectedNodeOwnerSymbol.BaseType.DeclaringSyntaxReferences.Length > 0)
                {
                    allBasesClasses.Add(selectedNodeOwnerSymbol.BaseType);
                }
                selectedNodeOwnerSymbol = selectedNodeOwnerSymbol.BaseType;
            }
            return allBasesClasses;
        }

        abstract protected bool IsUserSelectIdentifer(SyntaxNode userSelectedSyntax);

        abstract protected void ProcessingClassesRefactoring(IEnumerable<INamedTypeSymbol> targetClasses, SyntaxNode userSelectSyntax);

        abstract protected void ProcessingInterfacesRefactoring(IEnumerable<INamedTypeSymbol> targetInterfaces, SyntaxNode userSelectSyntax);
    }
}
