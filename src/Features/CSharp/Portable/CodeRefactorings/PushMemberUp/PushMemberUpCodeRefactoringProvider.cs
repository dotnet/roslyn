// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PushMember)), Shared]
    internal class PushMemberUpCodeRefactoringProvider : CodeRefactoringProvider
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
                ProcessingClassesRefactoringWithQuickAction(allTargetClasses, userSelectedNode);
                ProcessingInterfacesRefactoringQuickAction(allTargetInterfaces, userSelectedNode);
                ProcessingRefactoringViaDialogBox(allTargetClasses, allTargetInterfaces, userSelectedNodeSymbol.ContainingType);
            }
            else if (userSelectedNodeSymbol.Kind == SymbolKind.Field)
            {
                ProcessingClassesRefactoringWithQuickAction(allTargetClasses, userSelectedNode);
                ProcessingRefactoringViaDialogBox(allTargetClasses, allTargetInterfaces, userSelectedNodeSymbol.ContainingType);
            }
        }


        private void ProcessingRefactoringViaDialogBox(
            IEnumerable<INamedTypeSymbol> targetClasses,
            IEnumerable<INamedTypeSymbol> targetInterfaces,
            INamedTypeSymbol selectedNodeOwnerSymbol)
        {
            var service = Context.Document.Project.Solution.Workspace.Services.GetRequiredService<IPushMemberUpService>();

            var dialogAction = new PushMemberUpWithDialogCodeAction(SemanticModel, service, selectedNodeOwnerSymbol);
            Context.RegisterRefactoring(dialogAction);
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
       
        protected bool IsUserSelectIdentifer(SyntaxNode userSelectedSyntax)
        {
            var identifier = GetIdentifier(userSelectedSyntax);
            return identifier.Span.Contains(Context.Span);
        }

        private SyntaxToken GetIdentifier(SyntaxNode userSelectedSyntax)
        {
            switch (userSelectedSyntax)
            {
                case VariableDeclaratorSyntax variableSyntax:
                    return variableSyntax.Identifier;
                case MethodDeclarationSyntax methodSyntax:
                    return methodSyntax.Identifier;
                case PropertyDeclarationSyntax propertySyntax:
                    return propertySyntax.Identifier;
                case IndexerDeclarationSyntax indexerSyntax:
                    return indexerSyntax.ThisKeyword;
                default:
                    return default;
            }
        }

        protected void ProcessingClassesRefactoringWithQuickAction(IEnumerable<INamedTypeSymbol> targetClasses, SyntaxNode userSelectSyntax)
        {
            foreach (var eachClass in targetClasses)
            {
                var classPusher = new ClassPusherWithQuickAction(eachClass, SemanticModel, userSelectSyntax, Context.Document);
                var action = classPusher.ComputeRefactoring();
                if (action != null)
                {
                    Context.RegisterRefactoring(action);
                }
            }
        }

        protected void ProcessingInterfacesRefactoringQuickAction(IEnumerable<INamedTypeSymbol> targetInterfaces, SyntaxNode userSelectSyntax)
        {
            foreach (var eachInterface in targetInterfaces)
            {
                var interfacePusher = new InterfacePusherWithQuickAction(eachInterface, SemanticModel, userSelectSyntax, Context.Document);
                var action = interfacePusher.ComputeRefactoring();

                if (action != null)
                {
                    Context.RegisterRefactoring(action);
                }
            }
        }
    }
}
