// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    internal static class GoToDefinitionHelpers
    {
        public static async Task<ImmutableArray<DefinitionItem>> GetDefinitionsAsync(
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

            var definition = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
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
                if (factory != null)
                {
                    var thirdPartyItem = await factory.GetThirdPartyDefinitionItemAsync(solution, definitionItem, cancellationToken).ConfigureAwait(false);
                    definitions.AddIfNotNull(thirdPartyItem);
                }
            }

            definitions.Add(definitionItem);
            return definitions.ToImmutable();
        }

        public static async Task<bool> TryNavigateToLocationAsync(
            ISymbol symbol,
            Solution solution,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true)
        {
            var location = await GetDefinitionLocationAsync(
                symbol, solution, threadingContext, streamingPresenter, cancellationToken, thirdPartyNavigationAllowed).ConfigureAwait(false);
            return await location.TryNavigateToAsync(
                threadingContext, new NavigationOptions(PreferProvisionalTab: true, ActivateTab: true), cancellationToken).ConfigureAwait(false);
        }

        public static async Task<INavigableLocation?> GetDefinitionLocationAsync(
            ISymbol symbol,
            Solution solution,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true)
        {
            var title = string.Format(EditorFeaturesResources._0_declarations,
                FindUsagesHelpers.GetDisplayName(symbol));

            var definitions = await GetDefinitionsAsync(symbol, solution, thirdPartyNavigationAllowed, cancellationToken).ConfigureAwait(false);

            return await streamingPresenter.GetStreamingLocationAsync(
                threadingContext, solution.Workspace, title, definitions, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<INavigableItem>?> GetDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // Try IFindDefinitionService first. Until partners implement this, it could fail to find a service, so fall back if it's null.
            var findDefinitionService = document.GetLanguageService<IFindDefinitionService>();
            if (findDefinitionService != null)
            {
                return await findDefinitionService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            }

            // Removal of this codepath is tracked by https://github.com/dotnet/roslyn/issues/50391. Once it is removed, this GetDefinitions method should
            // be inlined into call sites.
            var goToDefinitionsService = document.GetRequiredLanguageService<IGoToDefinitionService>();
            return await goToDefinitionsService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
        }
    }
}
