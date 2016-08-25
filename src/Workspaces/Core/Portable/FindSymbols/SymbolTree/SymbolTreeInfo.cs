﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private readonly VersionStamp _version;

        /// <summary>
        /// The list of nodes that represent symbols. The primary key into the sorting of this list is the name.
        /// They are sorted case-insensitively with the <see cref="s_totalComparer" />. Finding case-sensitive
        /// matches can be found by binary searching for something that matches insensitively, and then searching
        /// around that equivalence class for one that matches.
        /// </summary>
        private readonly IReadOnlyList<Node> _nodes;

        /// <summary>
        /// The task that produces the spell checker we use for fuzzy match queries.
        /// We use a task so that we can generate the <see cref="SymbolTreeInfo"/> 
        /// without having to wait for the spell checker construction to finish.
        /// 
        /// Features that don't need fuzzy matching don't want to incur the cost of 
        /// the creation of this value.  And the only feature which does want fuzzy
        /// matching (add-using) doesn't want to block waiting for the value to be
        /// created.
        /// </summary>
        private readonly Task<SpellChecker> _spellCheckerTask;

        private static readonly StringComparer s_caseInsensitiveComparer = CaseInsensitiveComparison.Comparer;

        // We first sort in a case insensitive manner.  But, within items that match insensitively, 
        // we then sort in a case sensitive manner.  This helps for searching as we'll walk all 
        // the items of a specific casing at once.  This way features can cache values for that
        // casing and reuse them.  i.e. if we didn't do this we might get "Prop, prop, Prop, prop"
        // which might cause other features to continually recalculate if that string matches what
        // they're searching for.  However, with this sort of comparison we now get 
        // "prop, prop, Prop, Prop".  Features can take advantage of that by caching their previous
        // result and reusing it when they see they're getting the same string again.
        private static readonly Comparison<string> s_totalComparer = (s1, s2) =>
        {
            var diff = s_caseInsensitiveComparer.Compare(s1, s2);
            return diff != 0
                ? diff
                : StringComparer.Ordinal.Compare(s1, s2);
        };

        private SymbolTreeInfo(VersionStamp version, IReadOnlyList<Node> orderedNodes, Task<SpellChecker> spellCheckerTask)
        {
            _version = version;
            _nodes = orderedNodes;
            _spellCheckerTask = spellCheckerTask;
        }

        public Task<IEnumerable<ISymbol>> FindAsync(
            SearchQuery query, IAssemblySymbol assembly, SymbolFilter filter, CancellationToken cancellationToken)
        {
            return this.FindAsync(query, new AsyncLazy<IAssemblySymbol>(assembly), filter, cancellationToken);
        }

        public async Task<IEnumerable<ISymbol>> FindAsync(
            SearchQuery query, AsyncLazy<IAssemblySymbol> lazyAssembly, SymbolFilter filter, CancellationToken cancellationToken)
        {
            return SymbolFinder.FilterByCriteria(
                await FindAsyncWorker(query, lazyAssembly, cancellationToken).ConfigureAwait(false),
                filter);
        }

        private Task<IEnumerable<ISymbol>> FindAsyncWorker(SearchQuery query, AsyncLazy<IAssemblySymbol> lazyAssembly, CancellationToken cancellationToken)
        {
            // If the query has a specific string provided, then call into the SymbolTreeInfo
            // helpers optimized for lookup based on an exact name.
            switch (query.Kind)
            {
                case SearchKind.Exact:
                    return this.FindAsync(lazyAssembly, query.Name, ignoreCase: false, cancellationToken: cancellationToken);
                case SearchKind.ExactIgnoreCase:
                    return this.FindAsync(lazyAssembly, query.Name, ignoreCase: true, cancellationToken: cancellationToken);
                case SearchKind.Fuzzy:
                    return this.FuzzyFindAsync(lazyAssembly, query.Name, cancellationToken);
                case SearchKind.Custom:
                    // Otherwise, we'll have to do a slow linear search over all possible symbols.
                    return this.FindAsync(lazyAssembly, query.GetPredicate(), cancellationToken);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Finds symbols in this assembly that match the provided name in a fuzzy manner.
        /// </summary>
        private async Task<IEnumerable<ISymbol>> FuzzyFindAsync(
            AsyncLazy<IAssemblySymbol> lazyAssembly, string name, CancellationToken cancellationToken)
        {
            if (_spellCheckerTask.Status != TaskStatus.RanToCompletion)
            {
                // Spell checker isn't ready.  Just return immediately.
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            var spellChecker = _spellCheckerTask.Result;
            var similarNames = spellChecker.FindSimilarWords(name, substringsAreSimilar: false);
            var result = new List<ISymbol>();

            foreach (var similarName in similarNames)
            {
                var symbols = await FindAsync(lazyAssembly, similarName, ignoreCase: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                result.AddRange(symbols);
            }

            return result;
        }

        /// <summary>
        /// Get all symbols that have a name matching the specified name.
        /// </summary>
        private async Task<IEnumerable<ISymbol>> FindAsync(
            AsyncLazy<IAssemblySymbol> lazyAssembly,
            string name,
            bool ignoreCase,
            CancellationToken cancellationToken)
        {
            var comparer = GetComparer(ignoreCase);
            var result = new List<ISymbol>();
            IAssemblySymbol assemblySymbol = null;

            foreach (var node in FindNodes(name, comparer))
            {
                cancellationToken.ThrowIfCancellationRequested();
                assemblySymbol = assemblySymbol ?? await lazyAssembly.GetValueAsync(cancellationToken).ConfigureAwait(false);

                result.AddRange(Bind(node, assemblySymbol.GlobalNamespace, cancellationToken));
            }

            return result;
        }

        /// <summary>
        /// Slow, linear scan of all the symbols in this assembly to look for matches.
        /// </summary>
        private async Task<IEnumerable<ISymbol>> FindAsync(AsyncLazy<IAssemblySymbol> lazyAssembly, Func<string, bool> predicate, CancellationToken cancellationToken)
        {
            var result = new List<ISymbol>();
            IAssemblySymbol assembly = null;
            for (int i = 0, n = _nodes.Count; i < n; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var node = _nodes[i];
                if (predicate(node.Name))
                {
                    assembly = assembly ?? await lazyAssembly.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    result.AddRange(Bind(i, assembly.GlobalNamespace, cancellationToken));
                }
            }

            return result;
        }

        private static StringComparer GetComparer(bool ignoreCase)
        {
            return ignoreCase ? CaseInsensitiveComparison.Comparer : StringComparer.Ordinal;
        }

        /// <summary>
        /// Gets all the node indices with matching names per the <paramref name="comparer" />.
        /// </summary>
        private IEnumerable<int> FindNodes(string name, StringComparer comparer)
        {
            // find any node that matches case-insensitively
            var startingPosition = BinarySearch(name);

            if (startingPosition != -1)
            {
                // yield if this matches by the actual given comparer
                if (comparer.Equals(name, _nodes[startingPosition].Name))
                {
                    yield return startingPosition;
                }

                int position = startingPosition;
                while (position > 0 && s_caseInsensitiveComparer.Equals(_nodes[position - 1].Name, name))
                {
                    position--;
                    if (comparer.Equals(_nodes[position].Name, name))
                    {
                        yield return position;
                    }
                }

                position = startingPosition;
                while (position + 1 < _nodes.Count && s_caseInsensitiveComparer.Equals(_nodes[position + 1].Name, name))
                {
                    position++;
                    if (comparer.Equals(_nodes[position].Name, name))
                    {
                        yield return position;
                    }
                }
            }
        }

        /// <summary>
        /// Searches for a name in the ordered list that matches per the <see cref="s_caseInsensitiveComparer" />.
        /// </summary>
        private int BinarySearch(string name)
        {
            int max = _nodes.Count - 1;
            int min = 0;

            while (max >= min)
            {
                int mid = min + ((max - min) >> 1);

                var comparison = s_caseInsensitiveComparer.Compare(_nodes[mid].Name, name);
                if (comparison < 0)
                {
                    min = mid + 1;
                }
                else if (comparison > 0)
                {
                    max = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            return -1;
        }

        #region Construction

        // Cache the symbol tree infos for assembly symbols that share the same underlying metadata.
        // Generating symbol trees for metadata can be expensive (in large metadata cases).  And it's
        // common for us to have many threads to want to search the same metadata simultaneously.
        // As such, we want to only allow one thread to produce the tree for some piece of metadata
        // at a time.  
        //
        // AsyncLazy would normally be an ok choice here.  However, in the case where all clients
        // cancel their request, we don't want ot keep the AsyncLazy around.  It may capture a lot
        // of immutable state (like a Solution) that we don't want kept around indefinitely.  So we
        // only cache results (the symbol tree infos) if they successfully compute to completion.
        private static readonly ConditionalWeakTable<MetadataId, SemaphoreSlim> s_metadataIdToGate = new ConditionalWeakTable<MetadataId, SemaphoreSlim>();
        private static readonly ConditionalWeakTable<MetadataId, SymbolTreeInfo> s_metadataIdToInfo = new ConditionalWeakTable<MetadataId, SymbolTreeInfo>();

        private static readonly ConditionalWeakTable<MetadataId, SemaphoreSlim>.CreateValueCallback s_metadataIdToGateCallback =
            _ => new SemaphoreSlim(1);

        private static Task<SpellChecker> GetSpellCheckerTask(
            Solution solution, VersionStamp version, string filePath, Node[] nodes)
        {
            // Create a new task to attempt to load or create the spell checker for this 
            // SymbolTreeInfo.  This way the SymbolTreeInfo will be ready immediately
            // for non-fuzzy searches, and soon afterwards it will be able to perform
            // fuzzy searches as well.
            return Task.Run(() => LoadOrCreateSpellCheckerAsync(solution, filePath,
                v => new SpellChecker(v, nodes.Select(n => n.Name))));
        }

        private static Node[] SortNodes(List<Node> nodes)
        {
            // Generate index numbers from 0 to Count-1
            int[] tmp = new int[nodes.Count];
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] = i;
            }

            // Sort the index according to node elements
            Array.Sort<int>(tmp, (a, b) => CompareNodes(nodes[a], nodes[b], nodes));

            // Use the sort order to build the ranking table which will
            // be used as the map from original (unsorted) location to the
            // sorted location.
            int[] ranking = new int[nodes.Count];
            for (int i = 0; i < tmp.Length; i++)
            {
                ranking[tmp[i]] = i;
            }

            // No longer need the tmp array
            tmp = null;

            Node[] result = new Node[nodes.Count];

            // Copy nodes into the result array in the appropriate order and fixing
            // up parent indexes as we go.
            for (int i = 0; i < result.Length; i++)
            {
                Node n = nodes[i];
                result[ranking[i]] = new Node(n.Name, n.IsRoot ? n.ParentIndex : ranking[n.ParentIndex]);
            }

            return result;
        }

        private static int CompareNodes(Node x, Node y, IReadOnlyList<Node> nodeList)
        {
            var comp = s_totalComparer(x.Name, y.Name);
            if (comp == 0)
            {
                if (x.ParentIndex != y.ParentIndex)
                {
                    if (x.IsRoot)
                    {
                        return -1;
                    }
                    else if (y.IsRoot)
                    {
                        return 1;
                    }
                    else
                    {
                        return CompareNodes(nodeList[x.ParentIndex], nodeList[y.ParentIndex], nodeList);
                    }
                }
            }

            return comp;
        }

        #endregion

        #region Binding 

        // returns all the symbols in the container corresponding to the node
        private IEnumerable<ISymbol> Bind(int index, INamespaceOrTypeSymbol rootContainer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var symbols = SharedPools.Default<List<ISymbol>>().GetPooledObject())
            {
                Bind(index, rootContainer, symbols.Object, cancellationToken);

                foreach (var symbol in symbols.Object)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return symbol;
                }
            }
        }

        // returns all the symbols in the container corresponding to the node
        private void Bind(int index, INamespaceOrTypeSymbol rootContainer, List<ISymbol> results, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = _nodes[index];
            if (node.IsRoot)
            {
                return;
            }

            if (_nodes[node.ParentIndex].IsRoot)
            {
                results.AddRange(rootContainer.GetMembers(node.Name));
            }
            else
            {
                using (var containerSymbols = SharedPools.Default<List<ISymbol>>().GetPooledObject())
                {
                    Bind(node.ParentIndex, rootContainer, containerSymbols.Object, cancellationToken);

                    foreach (var containerSymbol in containerSymbols.Object.OfType<INamespaceOrTypeSymbol>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        results.AddRange(containerSymbol.GetMembers(node.Name));
                    }
                }
            }
        }
        #endregion

        internal bool IsEquivalent(SymbolTreeInfo other)
        {
            if (!_version.Equals(other._version) || _nodes.Count != other._nodes.Count)
            {
                return false;
            }

            for (int i = 0, n = _nodes.Count; i < n; i++)
            {
                if (!_nodes[i].IsEquivalent(other._nodes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static SymbolTreeInfo CreateSymbolTreeInfo(
            Solution solution, VersionStamp version, string filePath, List<Node> unsortedNodes)
        {
            var sortedNodes = SortNodes(unsortedNodes);
            var createSpellCheckerTask = GetSpellCheckerTask(solution, version, filePath, sortedNodes);
            return new SymbolTreeInfo(version, sortedNodes, createSpellCheckerTask);
        }
    }
}