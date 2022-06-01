// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        /// <summary>
        /// Represents the set of symbols that the engine is searching for.  While the find-refs engine is passed an
        /// initial symbol to find results for, the engine will often have to 'cascade' that symbol to many more symbols
        /// that clients will also need.  This includes:
        /// <list type="number">
        /// <item>Cascading to all linked symbols for the requested symbol.  This ensures a unified set of results for a
        /// particular symbol, regardless of what project context it was originally found in.</item>
        /// <item>Symbol specific cascading.  For example, when searching for a named type, references to that named
        /// type will be found through its constructors.</item>
        /// <item>Cascading up and down the inheritance hierarchy for members (e.g. methods, properties, events). This
        /// is controllable through the <see cref="FindReferencesSearchOptions.UnidirectionalHierarchyCascade"/>
        /// option.</item>
        /// </list>
        /// </summary>
        private abstract class SymbolSet
        {
            protected readonly FindReferencesSearchEngine Engine;

            protected SymbolSet(FindReferencesSearchEngine engine)
            {
                Engine = engine;
            }

            protected Solution Solution => Engine._solution;

            /// <summary>
            /// Get a copy of all the symbols in the set.  Cannot be called concurrently with <see
            /// cref="InheritanceCascadeAsync"/>
            /// </summary>
            public abstract ImmutableArray<ISymbol> GetAllSymbols();

            /// <summary>
            /// Update the set of symbols in this set with any appropriate symbols in the inheritance hierarchy brought
            /// in within <paramref name="project"/>.  For example, given a project 'A' with interface <c>interface IGoo
            /// { void Goo(); }</c>, and a project 'B' with class <c>class Goo : IGoo { public void Goo() { } }</c>,
            /// then initially the symbol set will only contain IGoo.Goo.  However, when project 'B' is processed, this
            /// will add Goo.Goo is added to the set as well so that references to it can be found.
            /// </summary>
            /// <remarks>
            /// This method is non threadsafe as it mutates the symbol set instance.  As such, it should only be
            /// called serially.  <see cref="GetAllSymbols"/> should not be called concurrently with this.
            /// </remarks>
            public abstract Task InheritanceCascadeAsync(Project project, CancellationToken cancellationToken);

            private static bool InvolvesInheritance(ISymbol symbol)
                => symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event;

            public static async Task<SymbolSet> CreateAsync(
                FindReferencesSearchEngine engine, ISymbol symbol, CancellationToken cancellationToken)
            {
                var solution = engine._solution;
                var options = engine._options;

                // Start by mapping the initial symbol to the appropriate source symbol in originating project if possible.
                var searchSymbol = await MapToAppropriateSymbolAsync(solution, symbol, cancellationToken).ConfigureAwait(false);

                // If the caller doesn't want any cascading then just return an appropriate set that will just point at
                // only the search symbol and won't cascade to any related symbols, linked symbols, or inheritance
                // symbols.
                if (!options.Cascade)
                    return new NonCascadingSymbolSet(engine, searchSymbol);

                // Keep track of the initial symbol group corresponding to search-symbol.  Any references to this group
                // will always be reported.
                //
                // Depending on what type of search we're doing, return an appropriate set that will have those
                // inheritance cascading semantics.
                var initialSymbols = await DetermineInitialSearchSymbolsAsync(engine, searchSymbol, cancellationToken).ConfigureAwait(false);

                // Walk and find all the symbols above the starting symbol set. 
                var upSymbols = await DetermineInitialUpSymbolsAsync(engine, initialSymbols, cancellationToken).ConfigureAwait(false);

                return options.UnidirectionalHierarchyCascade
                    ? new UnidirectionalSymbolSet(engine, initialSymbols, upSymbols)
                    : new BidirectionalSymbolSet(engine, initialSymbols, upSymbols);
            }

            private static async Task<ISymbol> MapToAppropriateSymbolAsync(
                Solution solution, ISymbol symbol, CancellationToken cancellationToken)
            {
                // Never search for an alias.  Always search for it's target.  Note: if the caller was
                // actually searching for an alias, they can always get that information out in the end
                // by checking the ReferenceLocations that are returned.
                var searchSymbol = symbol;

                if (searchSymbol is IAliasSymbol aliasSymbol)
                    searchSymbol = aliasSymbol.Target;

                searchSymbol = searchSymbol.GetOriginalUnreducedDefinition();

                // If they're searching for a delegate constructor, then just search for the delegate
                // itself.  They're practically interchangeable for consumers.
                if (searchSymbol.IsConstructor() && searchSymbol.ContainingType.TypeKind == TypeKind.Delegate)
                    searchSymbol = symbol.ContainingType;

                Contract.ThrowIfNull(searchSymbol);

                // Attempt to map this symbol back to a source symbol if possible as we always prefer the original
                // source definition as the 'truth' of a symbol versus seeing it projected into dependent cross language
                // projects as a metadata symbol.  If there is no source symbol, then continue to just use the metadata
                // symbol as the one to be looking for.
                var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(searchSymbol, solution, cancellationToken).ConfigureAwait(false);
                return sourceSymbol ?? searchSymbol;
            }

            /// <summary>
            /// Determines the initial set of symbols that we should actually be finding references for given a request
            /// to find refs to <paramref name="symbol"/>.  This will include any symbols that a specific <see
            /// cref="IReferenceFinder"/> cascades to, as well as all the linked symbols to those across any
            /// multi-targetting/shared-project documents.  This will not include symbols up or down the inheritance
            /// hierarchy.
            /// </summary>
            private static async Task<MetadataUnifyingSymbolHashSet> DetermineInitialSearchSymbolsAsync(
                FindReferencesSearchEngine engine, ISymbol symbol, CancellationToken cancellationToken)
            {
                var result = new MetadataUnifyingSymbolHashSet();
                var workQueue = new Stack<ISymbol>();

                // Start with the initial symbol we're searching for.
                workQueue.Push(symbol);

                // As long as there's work in the queue, keep going.
                while (workQueue.Count > 0)
                {
                    var currentSymbol = workQueue.Pop();
                    await AddCascadedAndLinkedSymbolsToAsync(engine, currentSymbol, result, workQueue, cancellationToken).ConfigureAwait(false);
                }

                return result;
            }

            private static async Task<HashSet<ISymbol>> DetermineInitialUpSymbolsAsync(
                FindReferencesSearchEngine engine, HashSet<ISymbol> initialSymbols, CancellationToken cancellationToken)
            {
                var upSymbols = new MetadataUnifyingSymbolHashSet();
                var workQueue = new Stack<ISymbol>();
                workQueue.Push(initialSymbols);

                var solution = engine._solution;
                var allProjects = solution.Projects.ToImmutableHashSet();
                while (workQueue.Count > 0)
                {
                    var currentSymbol = workQueue.Pop();
                    await AddUpSymbolsAsync(engine, currentSymbol, upSymbols, workQueue, allProjects, cancellationToken).ConfigureAwait(false);
                }

                return upSymbols;
            }

            protected static async Task AddCascadedAndLinkedSymbolsToAsync(
                FindReferencesSearchEngine engine, ImmutableArray<ISymbol> symbols, MetadataUnifyingSymbolHashSet seenSymbols, Stack<ISymbol> workQueue, CancellationToken cancellationToken)
            {
                foreach (var symbol in symbols)
                    await AddCascadedAndLinkedSymbolsToAsync(engine, symbol, seenSymbols, workQueue, cancellationToken).ConfigureAwait(false);
            }

            protected static async Task AddCascadedAndLinkedSymbolsToAsync(
                FindReferencesSearchEngine engine, ISymbol symbol, MetadataUnifyingSymbolHashSet seenSymbols, Stack<ISymbol> workQueue, CancellationToken cancellationToken)
            {
                var solution = engine._solution;
                symbol = await MapAndAddLinkedSymbolsAsync(symbol).ConfigureAwait(false);

                foreach (var finder in engine._finders)
                {
                    var cascaded = await finder.DetermineCascadedSymbolsAsync(symbol, solution, engine._options, cancellationToken).ConfigureAwait(false);
                    foreach (var cascade in cascaded)
                        await MapAndAddLinkedSymbolsAsync(cascade).ConfigureAwait(false);
                }

                return;

                async Task<ISymbol> MapAndAddLinkedSymbolsAsync(ISymbol symbol)
                {
                    symbol = await MapToAppropriateSymbolAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
                    foreach (var linked in await SymbolFinder.FindLinkedSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false))
                    {
                        if (seenSymbols.Add(linked))
                            workQueue.Push(linked);
                    }

                    return symbol;
                }
            }

            /// <summary>
            /// Finds all the symbols 'down' the inheritance hierarchy of <paramref name="symbol"/> in the given
            /// project.  The symbols found are added to <paramref name="seenSymbols"/>.  If <paramref name="seenSymbols"/> did not
            /// contain that symbol, then it is also added to <paramref name="workQueue"/> to allow fixed point
            /// algorithms to continue.
            /// </summary>
            /// <remarks><paramref name="projects"/> will always be a single project.  We just pass this in as a set to
            /// avoid allocating a fresh set every time this calls into FindMemberImplementationsArrayAsync.
            /// </remarks>
            protected static async Task AddDownSymbolsAsync(
                FindReferencesSearchEngine engine, ISymbol symbol,
                MetadataUnifyingSymbolHashSet seenSymbols, Stack<ISymbol> workQueue,
                ImmutableHashSet<Project> projects, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(projects.Count == 1, "Only a single project should be passed in");

                // Don't bother on symbols that aren't even involved in inheritance computations.
                if (!InvolvesInheritance(symbol))
                    return;

                var solution = engine._solution;
                if (symbol.IsImplementableMember())
                {
                    var implementations = await SymbolFinder.FindMemberImplementationsArrayAsync(
                        symbol, solution, projects, cancellationToken).ConfigureAwait(false);

                    await AddCascadedAndLinkedSymbolsToAsync(engine, implementations, seenSymbols, workQueue, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var overrrides = await SymbolFinder.FindOverridesArrayAsync(
                        symbol, solution, projects, cancellationToken).ConfigureAwait(false);

                    await AddCascadedAndLinkedSymbolsToAsync(engine, overrrides, seenSymbols, workQueue, cancellationToken).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Finds all the symbols 'up' the inheritance hierarchy of <paramref name="symbol"/> in the solution.  The
            /// symbols found are added to <paramref name="seenSymbols"/>.  If <paramref name="seenSymbols"/> did not contain that symbol,
            /// then it is also added to <paramref name="workQueue"/> to allow fixed point algorithms to continue.
            /// </summary>
            protected static async Task AddUpSymbolsAsync(
                FindReferencesSearchEngine engine, ISymbol symbol,
                MetadataUnifyingSymbolHashSet seenSymbols, Stack<ISymbol> workQueue,
                ImmutableHashSet<Project> projects, CancellationToken cancellationToken)
            {
                if (!InvolvesInheritance(symbol))
                    return;

                var solution = engine._solution;
                var originatingProject = solution.GetOriginatingProject(symbol);
                if (originatingProject != null)
                {
                    // We have a normal method.  Find any interface methods up the inheritance hierarchy that it implicitly
                    // or explicitly implements and cascade to those.
                    foreach (var match in await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false))
                        await AddCascadedAndLinkedSymbolsToAsync(engine, match, seenSymbols, workQueue, cancellationToken).ConfigureAwait(false);
                }

                // If we're overriding a member, then add it to the up-set
                if (symbol.GetOverriddenMember() is ISymbol overriddenMember)
                    await AddCascadedAndLinkedSymbolsToAsync(engine, overriddenMember, seenSymbols, workQueue, cancellationToken).ConfigureAwait(false);

                // An explicit interface method will cascade to all the methods that it implements in the up direction.
                await AddCascadedAndLinkedSymbolsToAsync(engine, symbol.ExplicitInterfaceImplementations(), seenSymbols, workQueue, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
