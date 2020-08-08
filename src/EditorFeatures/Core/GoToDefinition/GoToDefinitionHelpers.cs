// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    internal static class GoToDefinitionHelpers
    {
        public static ImmutableArray<DefinitionItem> GetDefinitions(
            ISymbol symbol,
            Solution solution,
            bool thirdPartyNavigationAllowed,
            CancellationToken cancellationToken)
        {
            var alias = symbol as IAliasSymbol;
            if (alias != null)
            {
                if (alias.Target is INamespaceSymbol ns && ns.IsGlobalNamespace)
                {
                    return ImmutableArray.Create<DefinitionItem>();
                }
            }

            // VB global import aliases have a synthesized SyntaxTree.
            // We can't go to the definition of the alias, so use the target type.

            if (alias != null)
            {
                var sourceLocations = NavigableItemFactory.GetPreferredSourceLocations(
                    solution, symbol, cancellationToken);

                if (sourceLocations.All(l => solution.GetDocument(l.SourceTree) == null))
                {
                    symbol = alias.Target;
                }
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).WaitAndGetResult(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            symbol = definition ?? symbol;

            // If it is a partial method declaration with no body, choose to go to the implementation
            // that has a method body.
            if (symbol is IMethodSymbol method)
            {
                symbol = method.PartialImplementationPart ?? symbol;
            }

            using var definitionsDisposer = ArrayBuilder<DefinitionItem>.GetInstance(out var definitions);

            // Going to a symbol may end up actually showing the symbol in the Find-Usages window.
            // This happens when there is more than one location for the symbol (i.e. for partial
            // symbols) and we don't know the best place to take you to.
            //
            // The FindUsages window supports showing the classified text for an item.  It does this
            // in two ways.  Either the item can pass along its classified text (and the window will
            // defer to that), or the item will have no classified text, and the window will compute
            // it in the BG.
            //
            // Passing along the classified information is valuable for OOP scenarios where we want
            // all that expensive computation done on the OOP side and not in the VS side.
            // 
            // However, Go To Definition is all in-process, and is also synchronous.  So we do not
            // want to fetch the classifications here.  It slows down the command and leads to a 
            // measurable delay in our perf tests.
            // 
            // So, if we only have a single location to go to, this does no unnecessary work.  And,
            // if we do have multiple locations to show, it will just be done in the BG, unblocking
            // this command thread so it can return the user faster.
            var definitionItem = symbol.ToNonClassifiedDefinitionItem(solution, includeHiddenLocations: true);

            if (thirdPartyNavigationAllowed)
            {
                var factory = solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
                var thirdPartyItem = factory?.GetThirdPartyDefinitionItem(solution, definitionItem, cancellationToken);
                definitions.AddIfNotNull(thirdPartyItem);
            }

            definitions.Add(definitionItem);
            return definitions.ToImmutable();
        }

        public static bool TryGoToDefinition(
            ISymbol symbol,
            Solution solution,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true)
        {
            var definitions = GetDefinitions(symbol, solution, thirdPartyNavigationAllowed, cancellationToken);

            var title = string.Format(EditorFeaturesResources._0_declarations,
                FindUsagesHelpers.GetDisplayName(symbol));

            return threadingContext.JoinableTaskFactory.Run(
                () => streamingPresenter.TryNavigateToOrPresentItemsAsync(
                    threadingContext, solution.Workspace, title, definitions));
        }

        public static bool TryGoToDefinition(
            ImmutableArray<DefinitionItem> definitions,
            Solution solution,
            string title,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter)
        {
            if (definitions.IsDefaultOrEmpty)
                return false;

            return threadingContext.JoinableTaskFactory.Run(() =>
                streamingPresenter.TryNavigateToOrPresentItemsAsync(
                    threadingContext, solution.Workspace, title, definitions));
        }
    }
}
