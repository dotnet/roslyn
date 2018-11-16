// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        private CancellationToken _cancellationToken;

        protected abstract bool IsSelectionValid(TextSpan span, SyntaxNode userSelectedSyntax);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            _cancellationToken = context.CancellationToken;
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var userSelectedNode = root.FindNode(context.Span);


            if (!IsSelectionValid(context.Span, userSelectedNode))
            {
                return;
            }

            var userSelectNodeSymbol = semanticModel.GetDeclaredSymbol(userSelectedNode);
            if (userSelectNodeSymbol == null || userSelectNodeSymbol.ContainingType == null)
            {
                return;
            }

            var allTargets = FindAllValidTargets(userSelectNodeSymbol.ContainingType, context.Document.Project.Solution);

            if (allTargets.Length == 0)
            {
                return;
            }

            if ((userSelectNodeSymbol is IMethodSymbol methodSymbol && methodSymbol.IsOrdinaryMethod()) ||
                userSelectNodeSymbol.Kind == SymbolKind.Property ||
                userSelectNodeSymbol.Kind == SymbolKind.Event ||
                userSelectNodeSymbol.Kind == SymbolKind.Field)
            {
                await PullMemberUpViaQuickAction(context, userSelectNodeSymbol, allTargets);
            }
        }

        private ImmutableArray<INamedTypeSymbol> FindAllValidTargets(
            INamedTypeSymbol selectedNodeOwnerSymbol,
            Solution solution)
        {
            return selectedNodeOwnerSymbol.AllInterfaces.Concat(selectedNodeOwnerSymbol.GetBaseTypes()).
                Where(baseType => baseType != null &&
                    baseType.DeclaringSyntaxReferences.Length > 0 &&
                    IsLocationValid(baseType, solution)).ToImmutableArray();
        }

        private bool IsLocationValid(INamedTypeSymbol symbol, Solution solution)
        {
            return symbol.Locations.Any(location => location.IsInSource &&
                !solution.GetDocument(location.SourceTree).IsGeneratedCode(_cancellationToken));
        }

        private async Task PullMemberUpViaQuickAction(
            CodeRefactoringContext context,
            ISymbol userSelectNodeSymbol,
            ImmutableArray<INamedTypeSymbol> targets)
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

                var action = await puller.TryComputeRefactoring(context.Document, userSelectNodeSymbol, target);
                if (action != null)
                {
                    context.RegisterRefactoring(action);
                }
            }
        }
    }
}
