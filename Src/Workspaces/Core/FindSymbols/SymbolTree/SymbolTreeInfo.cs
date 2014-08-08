// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly VersionStamp version;
        private readonly IReadOnlyList<Node> nodes;

        private SymbolTreeInfo(VersionStamp version, IReadOnlyList<Node> orderedNodes)
        {
            this.version = version;
            this.nodes = orderedNodes;
        }

        public int Count
        {
            get { return this.nodes.Count; }
        }

        public bool HasSymbols(string name, bool ignoreCase)
        {
            return BinarySearch(name, GetComparer(ignoreCase)) != -1;
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
            // The node list is always ordered using StringComparer.InvariantCulture, which guarantees that upper and lower case
            // symbols are adjacent in the order. Because of this, it is possible to also use StringComparer.InvariantCultureIgnoreCase on 
            // this same list, because all mixed cases equivalent strings will be contiguous.
            var comparer = GetComparer(ignoreCase);

            foreach (var node in FindNodes(name, comparer))
            {
                foreach (var symbol in Bind(node, assembly.GlobalNamespace, cancellationToken))
                {
                    yield return symbol;
                }
            }
        }

        private static StringComparer GetComparer(bool ignoreCase)
        {
            return ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;
        }

        /// <summary>
        /// Gets all the node indices with matching names.
        /// </summary>
        private IEnumerable<int> FindNodes(string name, StringComparer comparer)
        {
            // find any node that matches
            var position = BinarySearch(name, comparer);

            if (position != -1)
            {
                // back up to the first node that matches.
                var start = position;
                while (start > 0 && comparer.Compare(nodes[start - 1].Name, name) == 0)
                {
                    start--;
                }

                // yield the nodes we already know that match
                for (int i = start; i <= position; i++)
                {
                    yield return i;
                }

                // also yield any following nodes that might also match
                for (int i = position + 1; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    if (comparer.Compare(node.Name, name) == 0)
                    {
                        yield return i;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // search for a node with matching name in ordered list
        private int BinarySearch(string name, StringComparer nameComparer)
        {
            int max = nodes.Count - 1;
            int min = 0;

            while (max >= min)
            {
                int mid = min + ((max - min) >> 1);

                var comparison = nameComparer.Compare(nodes[mid].Name, name);
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

        public bool HasSymbols(Func<string, bool> predicate, CancellationToken cancellationToken)
        {
            string lastName = null;

            foreach (var node in this.nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if ((object)node.Name != (object)lastName
                    && predicate(node.Name))
                {
                    return true;
                }

                lastName = node.Name;
            }

            return false;
        }

        /// <summary>
        /// Get all symbols that have a matching name as determined by the predicate.
        /// </summary>
        public IEnumerable<ISymbol> Search(
            IAssemblySymbol assembly,
            Func<string, bool> predicate,
            CancellationToken cancellationToken)
        {
            string lastName = null;
            bool lastGood = false;

            for (int i = 0; i < this.nodes.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var node = this.nodes[i];
                var isSameName = (object)node.Name == (object)lastName;
                if ((isSameName && lastGood) // check for same string instance to avoid invoking predicate when we already know the outcome (assumes no side effects of predicate.)
                    || (!string.IsNullOrEmpty(node.Name) // don't consider unnamed things like the global namespace itself.
                          && predicate(node.Name)))
                {
                    lastGood = true;

                    // yield all symbols for this node
                    foreach (var symbol in Bind(i, assembly.GlobalNamespace, cancellationToken))
                    {
                        yield return symbol;
                    }
                }
                else
                {
                    lastGood = false;
                }

                lastName = node.Name;
            }
        }

        #region Construction

        private static readonly ConditionalWeakTable<BranchId, ConditionalWeakTable<ProjectId, SymbolTreeInfo>> inMemoryCache =
            new ConditionalWeakTable<BranchId, ConditionalWeakTable<ProjectId, SymbolTreeInfo>>();

        /// <summary>
        /// this gives you SymbolTreeInfo for a project
        /// </summary>
        public static async Task<SymbolTreeInfo> GetInfoForProjectAsync(Project project, CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
            {
                return null;
            }

            var branchId = project.Solution.BranchId;
            var workspace = project.Solution.Workspace;

            ConditionalWeakTable<ProjectId, SymbolTreeInfo> infoTable;
            if (!inMemoryCache.TryGetValue(branchId, out infoTable))
            {
                infoTable = inMemoryCache.GetValue(branchId, id => new ConditionalWeakTable<ProjectId, SymbolTreeInfo>());
            }

            // version doesn't need to know about dependent projects since we only care about names of top level declarations.
            var version = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            // first look to see if we already have the info
            SymbolTreeInfo info;
            if (infoTable.TryGetValue(project.Id, out info) && info != null && info.version == version)
            {
                return info;
            }

            infoTable.Remove(project.Id);

            // next, either get the info from the persistent service or create one
            var persistAndInfo = await LoadOrCreateAsync(project, cancellationToken).ConfigureAwait(false);

            var persisted = persistAndInfo.Item1;
            var newInfo = persistAndInfo.Item2;

            if (!persisted || ShouldCache(project))
            {
                // there could be someone already have put something in here.
                infoTable.GetValue(project.Id, _ => newInfo);
            }

            // use the info we calculated. otherwise, we can encounter a race.
            return newInfo;
        }

        private static bool ShouldCache(Project project)
        {
            using (var pooledObject = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject())
            {
                var set = pooledObject.Object;

                var solution = project.Solution;
                foreach (var documentId in solution.Workspace.GetOpenDocumentIds())
                {
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        continue;
                    }

                    var projectId = document.Project.Id;
                    if (set.Contains(projectId))
                    {
                        // already processed
                        continue;
                    }

                    if (document.Project == project)
                    {
                        return true;
                    }

                    set.Add(projectId);

                    foreach (var projectReference in document.Project.ProjectReferences)
                    {
                        if (projectReference.ProjectId == project.Id)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static readonly ConditionalWeakTable<IAssemblySymbol, SymbolTreeInfo> assemblyInfos = new ConditionalWeakTable<IAssemblySymbol, SymbolTreeInfo>();

        /// <summary>
        /// this gives you SymbolTreeInfo for a metadata
        /// </summary>
        public static async Task<SymbolTreeInfo> GetInfoForAssemblyAsync(Solution solution, IAssemblySymbol assembly, string filePath, CancellationToken cancellationToken)
        {
            SymbolTreeInfo info;
            if (assemblyInfos.TryGetValue(assembly, out info))
            {
                return info;
            }

            // IAssemblySymbol is immutable, even if we encounter a race, we might do same work twice but still will be correct.
            // now, we can't use AsyncLazy here since constructing information requires a solution. if we ever get cancellation before
            // finishing calculating, async lazy will hold onto solution graph until next call (if it ever gets called)
            info = await LoadOrCreateAsync(solution, assembly, filePath, cancellationToken).ConfigureAwait(false);
            return assemblyInfos.GetValue(assembly, _ => info);
        }

        internal static SymbolTreeInfo Create(VersionStamp version, IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            if (assembly == null)
            {
                return null;
            }

            var list = new List<Node>();
            GenerateNodes(assembly.GlobalNamespace, list);

            return new SymbolTreeInfo(version, SortNodes(list));
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
            var comp = StringComparer.InvariantCulture.Compare(x.Name, y.Name);
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
            // Use GetMemberNames, because we only want the names for the symbols that INamedTypeSymbol.MemberNames returns
            var memberNames = GetMemberNames(globalNamespace).Distinct();
            var memberLookup = GetMembers(globalNamespace).ToLookup(c => c.Name);

            foreach (var memberName in memberNames)
            {
                GenerateNodes(memberName, 0 /*index of root node*/, memberLookup[memberName], list);
            }
        }

        // generate nodes for symbols that share the same name, and all their descendants
        private static void GenerateNodes(string name, int parentIndex, IEnumerable<INamespaceOrTypeSymbol> symbolsWithSameName, List<Node> list)
        {
            var node = new Node(name, parentIndex);
            var nodeIndex = list.Count;
            list.Add(node);

            // Add all child members
            // Use GetMemberNames, because we only want the names for the symbols that INamedTypeSymbol.MemberNames returns
            var memberNames = symbolsWithSameName.SelectMany(c => GetMemberNames(c)).Distinct();
            var memberLookup = symbolsWithSameName.SelectMany(c => GetMembers(c)).ToLookup(c => c.Name);

            foreach (var memberName in memberNames)
            {
                GenerateNodes(memberName, nodeIndex, memberLookup[memberName], list);
            }
        }

        private static IEnumerable<string> GetMemberNames(ISymbol symbol)
        {
            var namedType = symbol as INamedTypeSymbol;
            if (namedType != null)
            {
                return namedType.MemberNames.Concat(namedType.GetTypeMembers().Select(t => t.Name));
            }

            var ns = symbol as INamespaceSymbol;
            if (ns != null)
            {
                return ns.GetMembers().Select(m => m.Name);
            }

            return SpecializedCollections.EmptyEnumerable<string>();
        }

        private static IEnumerable<INamespaceOrTypeSymbol> GetMembers(ISymbol symbol)
        {
            var nt = symbol as INamedTypeSymbol;
            if (nt != null)
            {
                return nt.GetTypeMembers();
            }

            var ns = symbol as INamespaceSymbol;
            if (ns != null)
            {
                return ns.GetMembers();
            }

            return SpecializedCollections.EmptyEnumerable<INamespaceOrTypeSymbol>();
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

            var node = this.nodes[index];

            if (this.nodes[node.ParentIndex].IsRoot)
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
            if (!this.version.Equals(other.version) || this.nodes.Count != other.nodes.Count)
            {
                return false;
            }

            for (int i = 0, n = this.nodes.Count; i < n; i++)
            {
                if (!this.nodes[i].IsEquivalent(other.nodes[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}