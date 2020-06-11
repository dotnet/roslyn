// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService
    {
        public async Task FindImplementationsAsync(Document document, int position, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;

            // If this is a symbol from a metadata-as-source project, then map that symbol back to a symbol in the primary workspace.
            var symbolAndProjectOpt = await FindUsagesHelpers.GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProjectOpt == null)
            {
                await context.ReportMessageAsync(
                    EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret).ConfigureAwait(false);
                return;
            }

            var symbolAndProject = symbolAndProjectOpt.Value;
            await FindImplementationsAsync(
                symbolAndProject.symbol, symbolAndProject.project, context).ConfigureAwait(false);
        }

        public static async Task FindImplementationsAsync(
            ISymbol symbol, Project project, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;
            var solution = project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                // Create a callback that we can pass to the server process to hear about the 
                // results as it finds them.  When we hear about results we'll forward them to
                // the 'progress' parameter which will then update the UI.
                var serverCallback = new FindUsagesServerCallback(solution, context);

                await client.RunRemoteAsync(
                    WellKnownServiceHubService.CodeAnalysis,
                    nameof(IRemoteFindUsagesService.FindImplementationsAsync),
                    solution,
                    new object[]
                    {
                        SerializableSymbolAndProjectId.Create(symbol, project, cancellationToken),
                    },
                    serverCallback,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Couldn't effectively search in OOP. Perform the search in-process.
                await FindImplementationsInCurrentProcessAsync(
                    symbol, project, context).ConfigureAwait(false);
            }
        }

        private static async Task FindImplementationsInCurrentProcessAsync(
            ISymbol symbol, Project project, IFindUsagesContext context)
        {
            var cancellationToken = context.CancellationToken;

            var solution = project.Solution;
            var (implementations, message) = await FindSourceImplementationsAsync(
                solution, symbol, cancellationToken).ConfigureAwait(false);

            if (message != null)
            {
                await context.ReportMessageAsync(message).ConfigureAwait(false);
                return;
            }

            await context.SetSearchTitleAsync(
                string.Format(EditorFeaturesResources._0_implementations,
                FindUsagesHelpers.GetDisplayName(symbol))).ConfigureAwait(false);

            foreach (var implementation in implementations)
            {
                var definitionItem = await implementation.ToClassifiedDefinitionItemAsync(
                    solution, isPrimary: true, includeHiddenLocations: false, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);

                await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }
        }

        private static async Task<(ImmutableArray<ISymbol> implementations, string? message)> FindSourceImplementationsAsync(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var builder = new HashSet<ISymbol>(SymbolEquivalenceComparer.Instance);

            // If we're in a linked file, try to find all the symbols this links to, and find all the implementations of
            // each of those linked symbols. De-dupe the results so the user only gets unique results.
            var linkedSymbols = await SymbolFinder.FindLinkedSymbolsAsync(
                symbol, solution, cancellationToken).ConfigureAwait(false);

            foreach (var linkedSymbol in linkedSymbols)
            {
                builder.AddRange(await FindSourceImplementationsWorkerAsync(
                    solution, linkedSymbol, cancellationToken).ConfigureAwait((bool)false));
            }

            var result = builder.ToImmutableArray();
            var message = result.IsEmpty ? EditorFeaturesResources.The_symbol_has_no_implementations : null;

            return (result, message);
        }

        private static async Task<ImmutableArray<ISymbol>> FindSourceImplementationsWorkerAsync(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var implementations = await FindSourceAndMetadataImplementationsAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
            return implementations.WhereAsArray(s => s.Locations.Any(l => l.IsInSource));
        }

        private static async Task<ImmutableArray<ISymbol>> FindSourceAndMetadataImplementationsAsync(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            if (symbol.IsInterfaceType() || symbol.IsImplementableMember())
            {
                var implementations = await SymbolFinder.FindImplementationsAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

                // It's important we use a HashSet here -- we may have cases in an inheritance hierarchy where more than one method
                // in an overrides chain implements the same interface method, and we want to duplicate those. The easiest way to do it
                // is to just use a HashSet.
                var implementationsAndOverrides = new HashSet<ISymbol>();

                foreach (var implementation in implementations)
                {
                    implementationsAndOverrides.Add(implementation);

                    // FindImplementationsAsync will only return the base virtual/abstract method, not that method and the overrides
                    // of the method. We should also include those.
                    if (implementation.IsOverridable())
                    {
                        var overrides = await SymbolFinder.FindOverridesAsync(
                            implementation, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                        implementationsAndOverrides.AddRange(overrides);
                    }
                }

                if (!symbol.IsInterfaceType() &&
                    !symbol.IsAbstract)
                {
                    implementationsAndOverrides.Add(symbol);
                }

                return implementationsAndOverrides.ToImmutableArray();
            }
            else if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Class } namedType)
            {
                var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                    namedType, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

                return derivedClasses.Concat(symbol).ToImmutableArray();
            }
            else if (symbol.IsOverridable())
            {
                var overrides = await SymbolFinder.FindOverridesAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                return overrides.Concat(symbol).ToImmutableArray();
            }
            else
            {
                // This is something boring like a regular method or type, so we'll just go there directly
                return ImmutableArray.Create(symbol);
            }
        }
    }
}
