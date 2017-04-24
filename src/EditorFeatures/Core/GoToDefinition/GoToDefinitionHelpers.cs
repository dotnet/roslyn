// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    internal static class GoToDefinitionHelpers
    {
        public static bool TryGoToDefinition(
            ISymbol symbol,
            Project project,
            IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            CancellationToken cancellationToken,
            bool thirdPartyNavigationAllowed = true,
            bool throwOnHiddenDefinition = false)
        {
            var alias = symbol as IAliasSymbol;
            if (alias != null)
            {
                var ns = alias.Target as INamespaceSymbol;
                if (ns != null && ns.IsGlobalNamespace)
                {
                    return false;
                }
            }

            // VB global import aliases have a synthesized SyntaxTree.
            // We can't go to the definition of the alias, so use the target type.

            var solution = project.Solution;
            if (alias != null)
            {
                var sourceLocations = NavigableItemFactory.GetPreferredSourceLocations(
                    solution, symbol, cancellationToken);

                if (sourceLocations.All(l => project.Solution.GetDocument(l.SourceTree) == null))
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

            var definitions = ArrayBuilder<DefinitionItem>.GetInstance();
            var definitionItem = symbol.ToClassifiedDefinitionItemAsync(
                solution, includeHiddenLocations: true, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

            if (thirdPartyNavigationAllowed)
            {
                var factory = solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
                var thirdPartyItem = factory?.GetThirdPartyDefinitionItem(solution, definitionItem, cancellationToken);
                definitions.AddIfNotNull(thirdPartyItem);
            }

            definitions.Add(definitionItem);

            var presenter = streamingPresenters.FirstOrDefault()?.Value;
            var title = string.Format(EditorFeaturesResources._0_declarations,
                FindUsagesHelpers.GetDisplayName(symbol));

            return presenter.TryNavigateToOrPresentItemsAsync(
                project.Solution.Workspace, title, definitions.ToImmutableAndFree()).WaitAndGetResult(cancellationToken);
        }
    }
}