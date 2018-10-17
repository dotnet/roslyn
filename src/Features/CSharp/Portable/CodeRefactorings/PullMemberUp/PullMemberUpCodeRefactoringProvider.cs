// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PullMember)), Shared]
    internal class PullMemberUpCodeRefactoringProvider : AbstractPullMemberUpRefactoringProvider
    {
        internal override bool IsUserSelectIdentifer(SyntaxNode userSelectedSyntax, CodeRefactoringContext context)
        {
            var identifier = GetIdentifier(userSelectedSyntax);
            return identifier.Span.Contains(context.Span);
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

        protected override void AddPullUpMemberToClassRefactoringViaQuickAction(
            List<INamedTypeSymbol> targetClasses,
            SyntaxNode userSelectedNode,
            SemanticModel semanticModel,
            CodeRefactoringContext context)
        {
            foreach (var eachClass in targetClasses)
            {
                var classPuller = new ClassPullerWithQuickAction(eachClass, context, semanticModel, userSelectedNode);
                var action = classPuller.ComputeRefactoring();
                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }

        protected override void AddPullUpMemberToInterfaceRefactoringViaQuickAction(
            List<INamedTypeSymbol> targetInterfaces,
            SyntaxNode userSelectedNode,
            SemanticModel semanticModel,
            CodeRefactoringContext context)
        {
            foreach (var eachInterface in targetInterfaces)
            {
                var interfacePuller = new InterfacePullerWithQuickAction(eachInterface, context, semanticModel, userSelectedNode);
                var action = interfacePuller.ComputeRefactoring();

                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }

        protected override void AddPullUpMemberRefactoringViaDialogBox(
            ISymbol userSelectedNodeSymbol,
            CodeRefactoringContext context,
            SemanticModel semanticModel)
        {
            var pullUpService = context.Document.Project.Solution.Workspace.Services.GetRequiredService<IPullMemberUpService>();
            var codeGenerationService = context.Document.Project.LanguageServices.GetService<ICodeGenerationService>();
            var dialogAction = new PullMemberUpWithDialogCodeAction(
                semanticModel, pullUpService, context, userSelectedNodeSymbol, context.Document, codeGenerationService, context.CancellationToken);
            context.RegisterRefactoring(dialogAction);
        }
    }
}
