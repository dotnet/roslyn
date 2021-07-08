// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
        /// <item>Symbol specific cascading.  For example, when searching for a named type, reference to that named type
        /// will be found through its constructors.</item>
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
            /// { void Goo(); }</c>, and a project 'B' with class <c>class Goo : IGoo { public void Goo() { } }</c>, then
            /// initially the symbol set will only contain IGoo.Goo.  However, when project 'B' is processed, this will
            /// ensure that Goo.Goo is added to the set so references to it can be found.
            /// </summary>
            /// <remarks>
            /// This method is non threadsafe *and* it mutates the symbol set instance.  As such, it should only be called
            /// serially.  <see cref="GetAllSymbols"/> should not be called concurrently with this.
            /// </remarks>
            public abstract Task InheritanceCascadeAsync(Project project, CancellationToken cancellationToken);

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
                var upSymbols = await GetAllUpSymbolsAsync(engine, initialSymbols, cancellationToken).ConfigureAwait(false);

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

                var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(searchSymbol, solution, cancellationToken).ConfigureAwait(false);
                return sourceSymbol ?? searchSymbol;
            }

            private static async Task<HashSet<ISymbol>> DetermineInitialSearchSymbolsAsync(
                FindReferencesSearchEngine engine, ISymbol symbol, CancellationToken cancellationToken)
            {
                var result = new HashSet<ISymbol>();
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

            protected static void PushAll(Stack<ISymbol> stack, ImmutableArray<ISymbol> symbols)
            {
                foreach (var symbol in symbols)
                    stack.Push(symbol);
            }

            protected static void PushAll(Stack<ISymbol> stack, HashSet<ISymbol> symbols)
            {
                foreach (var symbol in symbols)
                    stack.Push(symbol);
            }

            protected static async Task AddCascadedAndLinkedSymbolsToAsync(
                FindReferencesSearchEngine engine, ImmutableArray<ISymbol> symbols, HashSet<ISymbol> to, Stack<ISymbol> workQueue, CancellationToken cancellationToken)
            {
                foreach (var symbol in symbols)
                    await AddCascadedAndLinkedSymbolsToAsync(engine, symbol, to, workQueue, cancellationToken).ConfigureAwait(false);
            }

            protected static async Task AddCascadedAndLinkedSymbolsToAsync(
                FindReferencesSearchEngine engine, ISymbol symbol, HashSet<ISymbol> to, Stack<ISymbol> workQueue, CancellationToken cancellationToken)
            {
                foreach (var finder in engine._finders)
                {
                    var cascaded = await finder.DetermineCascadedSymbolsAsync(symbol, engine._solution, engine._options, cancellationToken).ConfigureAwait(false);
                    foreach (var cascade in cascaded)
                        await AddLinkedSymbolsToAsync(cascade).ConfigureAwait(false);
                }

                await AddLinkedSymbolsToAsync(symbol).ConfigureAwait(false);

                return;

                // Adds all the symbols 'lniked' to <paramref name="symbol"/> in the given. The symbols found are added
                // to <paramref name="to"/>.  If <paramref name="to"/> did not contain that symbol, then it is also
                // added to <paramref name="workQueue"/> to allow fixed point algorithms to continue.
                async Task AddLinkedSymbolsToAsync(ISymbol symbol)
                {
                    foreach (var linked in await SymbolFinder.FindLinkedSymbolsAsync(symbol, engine._solution, cancellationToken).ConfigureAwait(false))
                    {
                        if (to.Add(linked))
                            workQueue.Push(linked);
                    }
                }
            }

            private static bool InvolvesInheritance(ISymbol symbol)
                => symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event;

            /// <summary>
            /// Finds all the symbols 'down' the inheritance hierarchy of <paramref name="symbol"/> in the given
            /// project.  The symbols found are added to <paramref name="to"/>.  If <paramref name="to"/> did not
            /// contain that symbol, then it is also added to <paramref name="workQueue"/> to allow fixed point
            /// algorithms to continue.
            /// </summary>
            protected async Task AddDownSymbolsAsync(
                ISymbol symbol, HashSet<ISymbol> to, Stack<ISymbol> workQueue,
                ImmutableHashSet<Project> projects, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(projects.Count == 1, "Only a single project should be passed in");

                // Don't bother on symbols that aren't even involved in inheritance computations.
                if (!InvolvesInheritance(symbol))
                    return;

                if (symbol.IsImplementableMember())
                {
                    var implementations = await SymbolFinder.FindMemberImplementationsArrayAsync(
                        symbol, this.Solution, projects, cancellationToken).ConfigureAwait(false);

                    await AddCascadedAndLinkedSymbolsToAsync(this.Engine, implementations, to, workQueue, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var overrrides = await SymbolFinder.FindOverridesArrayAsync(
                        symbol, this.Solution, projects, cancellationToken).ConfigureAwait(false);

                    await AddCascadedAndLinkedSymbolsToAsync(this.Engine, overrrides, to, workQueue, cancellationToken).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Finds all the symbols 'up' the inheritance hierarchy of <paramref name="symbol"/> in the solution.  The
            /// symbols found are added to <paramref name="to"/>.  If <paramref name="to"/> did not contain that symbol,
            /// then it is also added to <paramref name="workQueue"/> to allow fixed point algorithms to continue.
            /// </summary>
            protected static async Task AddUpSymbolsAsync(
                FindReferencesSearchEngine engine, ISymbol symbol, HashSet<ISymbol> to, Stack<ISymbol> workQueue,
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
                        await AddSymbolsIfMissingAsync(match).ConfigureAwait(false);
                }

                if (symbol.GetOverriddenMember() is { } overriddenMember)
                    await AddSymbolsIfMissingAsync(overriddenMember).ConfigureAwait(false);

                return;

                async Task AddSymbolsIfMissingAsync(ISymbol symbol)
                {
                    // Ensure that if we walked up and found the symbol in another project that we map it to a source
                    // symbol if possible.
                    symbol = await MapToAppropriateSymbolAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
                    await AddCascadedAndLinkedSymbolsToAsync(engine, symbol, to, workQueue, cancellationToken).ConfigureAwait(false);
                }
            }

            private static async Task<HashSet<ISymbol>> GetAllUpSymbolsAsync(
                FindReferencesSearchEngine engine, HashSet<ISymbol> initialSymbols, CancellationToken cancellationToken)
            {
                var workQueue = new Stack<ISymbol>();
                PushAll(workQueue, initialSymbols);

                var upSymbols = new HashSet<ISymbol>();

                var solution = engine._solution;
                var allProjects = solution.Projects.ToImmutableHashSet();
                while (workQueue.Count > 0)
                {
                    var currentSymbol = workQueue.Pop();
                    await AddUpSymbolsAsync(engine, currentSymbol, upSymbols, workQueue, allProjects, cancellationToken).ConfigureAwait(false);
                }

                return upSymbols;
            }
        }
    }
}
