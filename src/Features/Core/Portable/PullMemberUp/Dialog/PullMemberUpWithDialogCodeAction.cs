// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp.Dialog;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMembrUp.Dialog
{
    internal class PullMemberUpWithDialogCodeAction : CodeActionWithOptions
    {
        private IPullMemberUpService PullMemberUpService { get; }

        private IEnumerable<ISymbol> Members { get; }

        private ISymbol SelectedNodeSymbol { get; }

        private Document ContextDocument { get; }

        private Dictionary<ISymbol, Lazy<List<ISymbol>>> LazyDependentsMap { get; }

        public override string Title => "...";

        internal PullMemberUpWithDialogCodeAction(
            SemanticModel semanticModel,
            CodeRefactoringContext context,
            ISymbol selectedNodeSymbol)
        {
            PullMemberUpService = context.Document.Project.Solution.Workspace.Services.GetService<IPullMemberUpService>();
            Members = selectedNodeSymbol.ContainingType.GetMembers().Where(
                member => {
                    if (member is IMethodSymbol methodSymbol)
                    {
                        return methodSymbol.MethodKind == MethodKind.Ordinary;
                    }
                    else if (member is IFieldSymbol fieldSymbol)
                    {
                        return !member.IsImplicitlyDeclared;
                    }
                    else if (member.Kind == SymbolKind.Property || member.Kind == SymbolKind.Event)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });

            var membersSet = new HashSet<ISymbol>(Members);

            // This map contains the content used by select dependents button
            LazyDependentsMap = Members.ToDictionary(
                memberSymbol => memberSymbol,
                memberSymbol => new Lazy<List<ISymbol>>(
                   () =>
                   {
                       if (memberSymbol.Kind == SymbolKind.Field || memberSymbol.Kind == SymbolKind.Event)
                       {
                           return new List<ISymbol>();
                       }
                       else
                       {
                           return SymbolDependentsBuilder.Build(semanticModel, memberSymbol, membersSet, context.Document, context.CancellationToken);
                       }

                   }, false));
            SelectedNodeSymbol = selectedNodeSymbol;
            ContextDocument = context.Document;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            var result = PullMemberUpService.GetPullTargetAndMembers(SelectedNodeSymbol, Members, LazyDependentsMap);
            if (result.IsCanceled)
            {
                return result;
            }
            else
            {
                if (result.Target.TypeKind == TypeKind.Interface)
                {
                    return ValidateSelection(new InterfaceModifiersValidator(generateWarningMessage: true), result, cancellationToken);
                }
                else if (result.Target.TypeKind == TypeKind.Class)
                {
                    return ValidateSelection(new ClassModifiersValidator(generateWarningMessage: true), result, cancellationToken);
                }
                else
                {
                    throw new ArgumentException($"{result.Target} should be interface or class");
                }
            }
        }

        private PullMemberDialogResult ValidateSelection(IValidator validator, PullMemberDialogResult result, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested &&
                   !result.IsCanceled &&
                   !validator.AreModifiersValid(result.Target, result.SelectedMembers.Select(memberSelectionPair => memberSelectionPair.member)))
            {
                if (PullMemberUpService.CreateWarningDialog(validator.WarningMessageList))
                {
                    validator.WarningMessageList.Clear();
                    return result;
                }
                else
                {
                    validator.WarningMessageList.Clear();
                    result = PullMemberUpService.RestoreSelectionDialog();
                }
            }
            return result;
        }
        
        protected async override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is PullMemberDialogResult result && !result.IsCanceled)
            {
                var service = ContextDocument.Project.LanguageServices.GetRequiredService<IPullMemberUpWithDialogService>();
                if (result.Target.TypeKind == TypeKind.Interface)
                {
                    var operation = new ApplyChangesOperation(
                        await service.ComputeInterfaceRefactoring(result, ContextDocument, cancellationToken));
                    return new CodeActionOperation[] { operation };
                }
                else if (result.Target.TypeKind == TypeKind.Class)
                {
                    var operation = new ApplyChangesOperation(
                        await service.ComputeClassRefactoring(result, ContextDocument, cancellationToken));
                    return new CodeActionOperation[] { operation };
                }
                else
                {
                    throw new ArgumentException($"{nameof(result.Target)} should be interface or class");
                }
            }
            else
            {
                return new CodeActionOperation[0];
            }
        }
    }
}
