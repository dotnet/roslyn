using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp.Dialog;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class PushMemberUpWithDialogCodeAction : CodeActionWithOptions
    {
        private IPushMemberUpService Service { get; }

        private IEnumerable<ISymbol> Members { get; }

        private INamedTypeSymbol SelectedNodeOwnerSymbol { get; }

        private SemanticModel SemanticModel { get; }

        // TODO: Add this title to config file??
        public override string Title => "...";

        internal PushMemberUpWithDialogCodeAction(
            SemanticModel semanticModel,
            IPushMemberUpService service,
            INamedTypeSymbol selectedNodeOwnerSymbol)
        {
            SemanticModel = semanticModel;
            Service = service;
            Members = selectedNodeOwnerSymbol.GetMembers().Where(
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
            SelectedNodeOwnerSymbol = selectedNodeOwnerSymbol;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            return Service.GetPushTargetAndMembers(SelectedNodeOwnerSymbol, Members);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            if (options is PushTargetsResult result && !result.IsCanceled)
            {
                var target = result.Target;
                var members = result.SelectedMembers;
                if (target.TypeKind == TypeKind.Interface)
                {
                    var interfacePusher = new InterfacePusherWithDialog(target);
                    return null;
                }
                else if (target.TypeKind == TypeKind.Class)
                {
                    return null;
                }
                else
                {
                    throw new ArgumentException($"{nameof(target)}'s TypeKind should be interface or class");
                }
            }
            else
            {
                IEnumerable<CodeActionOperation> actions = new List<CodeActionOperation>();
                return Task.FromResult(actions);
            }
        }
    }
}
