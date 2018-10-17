using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class PullMemberUpWithDialogCodeAction : CodeActionWithOptions
    {
        private IPullMemberUpService PullMemberUpService { get; }

        private IEnumerable<ISymbol> Members { get; }

        private ISymbol SelectedNodeSymbol { get; }

        private SemanticModel SemanticModel { get; }

        private Document ContextDocument { get; }

        private Dictionary<ISymbol, Lazy<List<ISymbol>>> LazyDependentsMap { get; }

        private CodeRefactoringContext Context { get; }

        private ICodeGenerationService CodeGenerationService { get; }

        // TODO: Add this title to config file??
        public override string Title => "...";

        internal PullMemberUpWithDialogCodeAction(
            SemanticModel semanticModel,
            IPullMemberUpService service,
            CodeRefactoringContext context,
            ISymbol selectedNodeSymbol,
            Document contextDocument,
            ICodeGenerationService codeGenerationService,
            CancellationToken cancellationToken)
        {
            SemanticModel = semanticModel;
            Context = context;
            PullMemberUpService = service;
            CodeGenerationService = codeGenerationService;
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
                    else
                    {
                        return true;
                    }
                });

            var membersSet = new HashSet<ISymbol>(Members);
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
                           return SymbolDependentsBuilder.Build(SemanticModel, memberSymbol, membersSet, contextDocument, cancellationToken);
                       }

                   }, false));
            SelectedNodeSymbol = selectedNodeSymbol;
            ContextDocument = contextDocument;
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
                    return ValidateSelection(new InterfaceModifiersValidator(), result, cancellationToken);
                }
                else if (result.Target.TypeKind == TypeKind.Class)
                {
                    return ValidateSelection(new ClassModifiersValidator(), result, cancellationToken);
                }
                else
                {
                    throw new ArgumentException($"{result.Target} should be interface or class");
                }
            }
        }

        private PullTargetsResult ValidateSelection(IValidator validator, PullTargetsResult result, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !validator.AreModifiersValid(result.Target, result.SelectedMembers.Select(memberSelectionPair => memberSelectionPair.member)))
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
            if (options is PullTargetsResult result && !result.IsCanceled)
            {
                if (result.Target.TypeKind == TypeKind.Interface)
                {
                    var interfacePuller 
                        = new InterfacePullerWithDialog();
                    var operation = new ApplyChangesOperation(await
                        interfacePuller.ComputeChangedSolution(result, SemanticModel, ContextDocument, CodeGenerationService, cancellationToken));
                    return new CodeActionOperation[] { operation };
                }
                else if (result.Target.TypeKind == TypeKind.Class)
                {
                    // TODO: Add class puller
                    return null;
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
