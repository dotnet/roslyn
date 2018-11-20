// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PullMemberUp.QuickAction;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract class AbstractPullMemberUpRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract bool IsSelectionValid(TextSpan span, SyntaxNode userSelectedSyntax);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var userSelectedNode = root.FindNode(context.Span);

            if (userSelectedNode == null)
            {
                return;
            }

            var userSelectNodeSymbol = semanticModel.GetDeclaredSymbol(userSelectedNode);
            if (userSelectNodeSymbol == null || userSelectNodeSymbol.ContainingType == null)
            {
                return;
            }

            if (!userSelectNodeSymbol.IsKind(SymbolKind.Property) &&
                !userSelectNodeSymbol.IsKind(SymbolKind.Event) &&
                !userSelectNodeSymbol.IsKind(SymbolKind.Field) &&
                !userSelectNodeSymbol.IsKind(SymbolKind.Method))
            {
                // Static, abstract and accessiblity are not checked here but in PullMemberUpAnalyzer.cs since there are
                // two refactoring options provided for pull members up,
                // 1. Quick Action (Only allow members that don't cause error)
                // 2. Dialog box (Allow modifers may cause errors and will provide fixing)
                return;
            }

            if (userSelectNodeSymbol is IMethodSymbol methodSymbol && !methodSymbol.IsOrdinaryMethod())
            {
                return;
            }

            if (!IsSelectionValid(context.Span, userSelectedNode))
            {
                return;
            }

            var allDestinations = FindAllValidDestinations(
                userSelectNodeSymbol.ContainingType,
                document.Project.Solution,
                context.CancellationToken);
            if (allDestinations.Length == 0)
            {
                return;
            }
            
            await PullMemberUpViaQuickAction(context, userSelectNodeSymbol, allDestinations);
        }

        private ImmutableArray<INamedTypeSymbol> FindAllValidDestinations(
            INamedTypeSymbol selectedNodeOwnerSymbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return selectedNodeOwnerSymbol.AllInterfaces.Concat(selectedNodeOwnerSymbol.GetBaseTypes()).ToImmutableArray().
                WhereAsArray(baseType => baseType != null &&
                    baseType.DeclaringSyntaxReferences.Length > 0 &&
                    IsLocationValid(baseType, solution, cancellationToken));
        }

        private bool IsLocationValid(INamedTypeSymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            return symbol.Locations.Any(location => location.IsInSource &&
                !solution.GetDocument(location.SourceTree).IsGeneratedCode(cancellationToken));
        }

        private async Task PullMemberUpViaQuickAction(
            CodeRefactoringContext context,
            ISymbol userSelectNodeSymbol,
            ImmutableArray<INamedTypeSymbol> destinations)
        {
            foreach (var destination in destinations)
            {
                var puller = destination.TypeKind == TypeKind.Interface && userSelectNodeSymbol.Kind != SymbolKind.Field
                ? new InterfacePullerWithQuickAction() as AbstractMemberPullerWithQuickAction
                : new ClassPullerWithQuickAction();
                var action = await puller.TryComputeRefactoring(context.Document, userSelectNodeSymbol, destination, context.CancellationToken);
                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }
    }
}
