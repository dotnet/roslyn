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
        /// The spell checker we use for fuzzy match queries.
        /// </summary>
        private readonly SpellChecker _spellChecker;

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

        private SymbolTreeInfo(VersionStamp version, IReadOnlyList<Node> orderedNodes, SpellChecker spellChecker)
        {
            _version = version;
            _nodes = orderedNodes;
            _spellChecker = spellChecker;
        }

        public int Count => _nodes.Count;

        public Task<IEnumerable<ISymbol>> FindAsync(SearchQuery query, IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            return FindAsync(query, new AsyncLazy<IAssemblySymbol>(assembly), cancellationToken);
        }

        public Task<IEnumerable<ISymbol>> FindAsync(SearchQuery query, AsyncLazy<IAssemblySymbol> lazyAssembly, CancellationToken cancellationToken)
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
        public async Task<IEnumerable<ISymbol>> FuzzyFindAsync(AsyncLazy<IAssemblySymbol> lazyAssembly, string name, CancellationToken cancellationToken)
        {
            var similarNames = _spellChecker.FindSimilarWords(name);
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
        public async Task<IEnumerable<ISymbol>> FindAsync(
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
        public async Task<IEnumerable<ISymbol>> FindAsync(AsyncLazy<IAssemblySymbol> lazyAssembly, Func<string, bool> predicate, CancellationToken cancellationToken)
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

        /// <summary>
        /// this gives you SymbolTreeInfo for a metadata
        /// </summary>
        public static async Task<SymbolTreeInfo> TryGetInfoForMetadataAssemblyAsync(
            Solution solution,
            IAssemblySymbol assembly,
            PortableExecutableReference reference,
            bool loadOnly,
            CancellationToken cancellationToken)
        {
            var metadata = assembly.GetMetadata();
            if (metadata == null)
            {
                return null;
            }

            // Find the lock associated with this piece of metadata.  This way only one thread is
            // computing a symbol tree info for a particular piece of metadata at a time.
            var gate = s_metadataIdToGate.GetValue(metadata.Id, s_metadataIdToGateCallback);
            using (await gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                SymbolTreeInfo info;
                if (s_metadataIdToInfo.TryGetValue(metadata.Id, out info))
                {
                    return info;
                }

                info = await LoadOrCreateAsync(solution, assembly, reference.FilePath, loadOnly, cancellationToken).ConfigureAwait(false);
                if (info == null && loadOnly)
                {
                    return null;
                }

                return s_metadataIdToInfo.GetValue(metadata.Id, _ => info);
            }
        }

        public static async Task<SymbolTreeInfo> GetInfoForSourceAssemblyAsync(
            Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            return await LoadOrCreateAsync(
                project.Solution, compilation.Assembly, project.FilePath, loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        internal static SymbolTreeInfo Create(VersionStamp version, IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            if (assembly == null)
            {
                return null;
            }

            var list = new List<Node>();
            GenerateNodes(assembly.GlobalNamespace, list);

            var spellChecker = new SpellChecker(list.Select(n => n.Name));
            return new SymbolTreeInfo(version, SortNodes(list), spellChecker);
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

        // generate nodes for the global namespace an all descendants
        private static void GenerateNodes(INamespaceSymbol globalNamespace, List<Node> list)
        {
            var node = new Node(globalNamespace.Name, Node.RootNodeParentIndex);
            list.Add(node);

            // Add all child members
            var memberLookup = s_getMembers(globalNamespace).ToLookup(c => c.Name);

            foreach (var grouping in memberLookup)
            {
                GenerateNodes(grouping.Key, 0 /*index of root node*/, grouping, list);
            }
        }

        private static readonly Func<ISymbol, bool> s_useSymbol =
            s => s.CanBeReferencedByName && s.DeclaredAccessibility != Accessibility.Private;

        // generate nodes for symbols that share the same name, and all their descendants
        private static void GenerateNodes(string name, int parentIndex, IEnumerable<ISymbol> symbolsWithSameName, List<Node> list)
        {
            var node = new Node(name, parentIndex);
            var nodeIndex = list.Count;
            list.Add(node);

            // Add all child members
            var membersByName = symbolsWithSameName.SelectMany(s_getMembers).ToLookup(s => s.Name);

            foreach (var grouping in membersByName)
            {
                GenerateNodes(grouping.Key, nodeIndex, grouping, list);
            }
        }

        private static Func<ISymbol, IEnumerable<ISymbol>> s_getMembers = symbol =>
        {
            var nt = symbol as INamespaceOrTypeSymbol;
            return nt != null
                ? nt.GetMembers().Where(s_useSymbol)
                : SpecializedCollections.EmptyEnumerable<ISymbol>();
        };

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
    }
}
