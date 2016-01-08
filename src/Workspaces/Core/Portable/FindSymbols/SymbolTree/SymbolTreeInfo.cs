// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        /// <summary>
        /// Finds symbols in this assembly that match the provided name in a fuzzy manner.
        /// </summary>
        public IEnumerable<ISymbol> FuzzyFind(IAssemblySymbol assembly, string name, CancellationToken cancellationToken)
        {
            var similarNames = _spellChecker.FindSimilarWords(name);
            return similarNames.SelectMany(n => Find(assembly, n, ignoreCase: true, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Get all symbols that have a name matching the specified name.
        /// </summary>
        public IEnumerable<ISymbol> Find(
            IAssemblySymbol assembly,
            string name,
            bool ignoreCase,
            CancellationToken cancellationToken)
        {
            var comparer = GetComparer(ignoreCase);

            foreach (var node in FindNodes(name, comparer))
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var symbol in Bind(node, assembly.GlobalNamespace, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return symbol;
                }
            }
        }

        /// <summary>
        /// Slow, linear scan of all the symbols in this assembly to look for matches.
        /// </summary>
        public IEnumerable<ISymbol> Find(IAssemblySymbol assembly, Func<string, bool> predicate, CancellationToken cancellationToken)
        {
            for (int i = 0, n = _nodes.Count; i < n; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var node = _nodes[i];
                if (predicate(node.Name))
                {
                    foreach (var symbol in Bind(i, assembly.GlobalNamespace, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return symbol;
                    }
                }
            }
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

                var comparison =  s_caseInsensitiveComparer.Compare(_nodes[mid].Name, name);
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
        private static readonly ConditionalWeakTable<MetadataId, SymbolTreeInfo> s_assemblyInfos = new ConditionalWeakTable<MetadataId, SymbolTreeInfo>();

        private static readonly SemaphoreSlim s_assemblyInfosGate = new SemaphoreSlim(1);

        /// <summary>
        /// this gives you SymbolTreeInfo for a metadata
        /// </summary>
        public static async Task<SymbolTreeInfo> TryGetInfoForAssemblyAsync(Solution solution, IAssemblySymbol assembly, PortableExecutableReference reference, CancellationToken cancellationToken)
        {
            using (await s_assemblyInfosGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var metadataId = assembly.GetMetadata()?.Id;
                if (metadataId == null)
                {
                    return null;
                }

                SymbolTreeInfo info;
                if (s_assemblyInfos.TryGetValue(metadataId, out info))
                {
                    return info;
                }

                info = await LoadOrCreateAsync(solution, assembly, reference.FilePath, cancellationToken).ConfigureAwait(false);
                return s_assemblyInfos.GetValue(metadataId, _ => info);
            }
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
            var memberLookup = GetMembers(globalNamespace).ToLookup(c => c.Name);

            foreach (var grouping in memberLookup)
            {
                GenerateNodes(grouping.Key, 0 /*index of root node*/, grouping, list);
            }
        }

        private static readonly Func<ISymbol, bool> UseSymbol = 
            s => s.CanBeReferencedByName && s.DeclaredAccessibility != Accessibility.Private;

        // generate nodes for symbols that share the same name, and all their descendants
        private static void GenerateNodes(string name, int parentIndex, IEnumerable<ISymbol> symbolsWithSameName, List<Node> list)
        {
            var node = new Node(name, parentIndex);
            var nodeIndex = list.Count;
            list.Add(node);

            // Add all child members
            var membersByName = symbolsWithSameName.SelectMany(GetMembers).ToLookup(s => s.Name);

            foreach (var grouping in membersByName)
            {
                GenerateNodes(grouping.Key, nodeIndex, grouping, list);
            }
        }

        private static Func<ISymbol, IEnumerable<ISymbol>> GetMembers = symbol =>
        {
            var nt = symbol as INamespaceOrTypeSymbol;
            return nt != null 
                ? nt.GetMembers().Where(UseSymbol)
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
