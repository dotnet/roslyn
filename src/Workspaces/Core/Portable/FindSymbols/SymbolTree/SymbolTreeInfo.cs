// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo : IChecksummedObject
    {
        public Checksum Checksum { get; }

        /// <summary>
        /// The list of nodes that represent symbols. The primary key into the sorting of this 
        /// list is the name. They are sorted case-insensitively with the <see cref="s_totalComparer" />.
        /// Finding case-sensitive matches can be found by binary searching for something that 
        /// matches insensitively, and then searching around that equivalence class for one that 
        /// matches.
        /// </summary>
        private readonly ImmutableArray<Node> _nodes;

        /// <summary>
        /// Inheritance information for the types in this assembly.  The mapping is between
        /// a type's simple name (like 'IDictionary') and the simple metadata names of types 
        /// that implement it or derive from it (like 'Dictionary').
        /// 
        /// Note: to save space, all names in this map are stored with simple ints.  These
        /// ints are the indices into _nodes that contain the nodes with the appropriate name.
        /// 
        /// This mapping is only produced for metadata assemblies.
        /// </summary>
        private readonly OrderPreservingMultiDictionary<int, int> _inheritanceMap;

        /// <summary>
        /// Maps the name of receiver type name to its <see cref="ExtensionMethodInfo" />.
        /// <see cref="ParameterTypeInfo"/> for the definition of simple/complex methods.
        /// For non-array simple types, the receiver type name would be its metadata name, e.g. "Int32".
        /// For any array types with simple type as element, the receiver type name would be just "ElementTypeName[]", e.g. "Int32[]" for int[][,]
        /// For non-array complex types, the receiver type name is "".
        /// For any array types with complex type as element, the receier type name is "[]"
        /// </summary>
        private readonly MultiDictionary<string, ExtensionMethodInfo>? _receiverTypeNameToExtensionMethodMap;

        public MultiDictionary<string, ExtensionMethodInfo>.ValueSet GetExtensionMethodInfoForReceiverType(string typeName)
            => _receiverTypeNameToExtensionMethodMap != null
                ? _receiverTypeNameToExtensionMethodMap[typeName]
                : new MultiDictionary<string, ExtensionMethodInfo>.ValueSet(null, null);

        public bool ContainsExtensionMethod => _receiverTypeNameToExtensionMethodMap?.Count > 0;

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

        private static readonly StringSliceComparer s_caseInsensitiveComparer =
            StringSliceComparer.OrdinalIgnoreCase;

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
            var diff = CaseInsensitiveComparison.Comparer.Compare(s1, s2);
            return diff != 0
                ? diff
                : StringComparer.Ordinal.Compare(s1, s2);
        };

        private SymbolTreeInfo(
            Checksum checksum,
            ImmutableArray<Node> sortedNodes,
            Task<SpellChecker> spellCheckerTask,
            OrderPreservingMultiDictionary<string, string> inheritanceMap,
            MultiDictionary<string, ExtensionMethodInfo> receiverTypeNameToExtensionMethodMap)
            : this(checksum, sortedNodes, spellCheckerTask,
                   CreateIndexBasedInheritanceMap(sortedNodes, inheritanceMap),
                   receiverTypeNameToExtensionMethodMap)
        {
        }

        private SymbolTreeInfo(
            Checksum checksum,
            ImmutableArray<Node> sortedNodes,
            Task<SpellChecker> spellCheckerTask,
            OrderPreservingMultiDictionary<int, int> inheritanceMap,
            MultiDictionary<string, ExtensionMethodInfo>? receiverTypeNameToExtensionMethodMap)
        {
            Checksum = checksum;
            _nodes = sortedNodes;
            _spellCheckerTask = spellCheckerTask;
            _inheritanceMap = inheritanceMap;
            _receiverTypeNameToExtensionMethodMap = receiverTypeNameToExtensionMethodMap;
        }

        public static SymbolTreeInfo CreateEmpty(Checksum checksum)
        {
            var unsortedNodes = ImmutableArray.Create(BuilderNode.RootNode);
            SortNodes(unsortedNodes, out var sortedNodes);

            return new SymbolTreeInfo(checksum, sortedNodes,
                CreateSpellCheckerAsync(checksum, sortedNodes),
                new OrderPreservingMultiDictionary<string, string>(),
                new MultiDictionary<string, ExtensionMethodInfo>());
        }

        public SymbolTreeInfo WithChecksum(Checksum checksum)
        {
            return new SymbolTreeInfo(
                checksum, _nodes, _spellCheckerTask, _inheritanceMap, _receiverTypeNameToExtensionMethodMap);
        }

        public Task<ImmutableArray<ISymbol>> FindAsync(
            SearchQuery query, IAssemblySymbol assembly, SymbolFilter filter, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            return this.FindAsync(
                query, new AsyncLazy<IAssemblySymbol>(assembly), filter, cancellationToken);
        }

        public async Task<ImmutableArray<ISymbol>> FindAsync(
            SearchQuery query, AsyncLazy<IAssemblySymbol> lazyAssembly,
            SymbolFilter filter, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            var symbols = await FindCoreAsync(query, lazyAssembly, cancellationToken).ConfigureAwait(false);

            return DeclarationFinder.FilterByCriteria(symbols, filter);
        }

        private Task<ImmutableArray<ISymbol>> FindCoreAsync(
            SearchQuery query, AsyncLazy<IAssemblySymbol> lazyAssembly, CancellationToken cancellationToken)
        {
            // All entrypoints to this function are Find functions that are only searching
            // for specific strings (i.e. they never do a custom search).
            Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            // If the query has a specific string provided, then call into the SymbolTreeInfo
            // helpers optimized for lookup based on an exact name.
            return query.Kind switch
            {
                SearchKind.Exact => this.FindAsync(lazyAssembly, query.Name, ignoreCase: false, cancellationToken: cancellationToken),
                SearchKind.ExactIgnoreCase => this.FindAsync(lazyAssembly, query.Name, ignoreCase: true, cancellationToken: cancellationToken),
                SearchKind.Fuzzy => this.FuzzyFindAsync(lazyAssembly, query.Name, cancellationToken),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Finds symbols in this assembly that match the provided name in a fuzzy manner.
        /// </summary>
        private async Task<ImmutableArray<ISymbol>> FuzzyFindAsync(
            AsyncLazy<IAssemblySymbol> lazyAssembly, string name, CancellationToken cancellationToken)
        {
            if (_spellCheckerTask.Status != TaskStatus.RanToCompletion)
            {
                // Spell checker isn't ready.  Just return immediately.
                return ImmutableArray<ISymbol>.Empty;
            }

            var spellChecker = await _spellCheckerTask.ConfigureAwait(false);
            var similarNames = spellChecker.FindSimilarWords(name, substringsAreSimilar: false);
            var result = ArrayBuilder<ISymbol>.GetInstance();

            foreach (var similarName in similarNames)
            {
                var symbols = await FindAsync(lazyAssembly, similarName, ignoreCase: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                result.AddRange(symbols);
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Get all symbols that have a name matching the specified name.
        /// </summary>
        private async Task<ImmutableArray<ISymbol>> FindAsync(
            AsyncLazy<IAssemblySymbol> lazyAssembly,
            string name,
            bool ignoreCase,
            CancellationToken cancellationToken)
        {
            var comparer = GetComparer(ignoreCase);
            IAssemblySymbol? assemblySymbol = null;

            using var results = TemporaryArray<ISymbol>.Empty;
            foreach (var node in FindNodeIndices(name, comparer))
            {
                cancellationToken.ThrowIfCancellationRequested();
                assemblySymbol ??= await lazyAssembly.GetValueAsync(cancellationToken).ConfigureAwait(false);

                Bind(node, assemblySymbol.GlobalNamespace, ref results.AsRef(), cancellationToken);
            }

            return results.ToImmutableAndClear();
        }

        private static StringSliceComparer GetComparer(bool ignoreCase)
        {
            return ignoreCase
                ? StringSliceComparer.OrdinalIgnoreCase
                : StringSliceComparer.Ordinal;
        }

        private IEnumerable<int> FindNodeIndices(string name, StringSliceComparer comparer)
            => FindNodeIndices(_nodes, name, comparer);

        /// <summary>
        /// Gets all the node indices with matching names per the <paramref name="comparer" />.
        /// </summary>
        private static IEnumerable<int> FindNodeIndices(
            ImmutableArray<Node> nodes,
            string name, StringSliceComparer comparer)
        {
            // find any node that matches case-insensitively
            var startingPosition = BinarySearch(nodes, name);
            var nameSlice = name.AsMemory();

            if (startingPosition != -1)
            {
                // yield if this matches by the actual given comparer
                if (comparer.Equals(nameSlice, GetNameSlice(nodes, startingPosition)))
                {
                    yield return startingPosition;
                }

                var position = startingPosition;
                while (position > 0 && s_caseInsensitiveComparer.Equals(GetNameSlice(nodes, position - 1), nameSlice))
                {
                    position--;
                    if (comparer.Equals(GetNameSlice(nodes, position), nameSlice))
                    {
                        yield return position;
                    }
                }

                position = startingPosition;
                while (position + 1 < nodes.Length && s_caseInsensitiveComparer.Equals(GetNameSlice(nodes, position + 1), nameSlice))
                {
                    position++;
                    if (comparer.Equals(GetNameSlice(nodes, position), nameSlice))
                    {
                        yield return position;
                    }
                }
            }
        }

        private static ReadOnlyMemory<char> GetNameSlice(
            ImmutableArray<Node> nodes, int nodeIndex)
        {
            return nodes[nodeIndex].Name.AsMemory();
        }

        private int BinarySearch(string name)
            => BinarySearch(_nodes, name);

        /// <summary>
        /// Searches for a name in the ordered list that matches per the <see cref="s_caseInsensitiveComparer" />.
        /// </summary>
        private static int BinarySearch(ImmutableArray<Node> nodes, string name)
        {
            var nameSlice = name.AsMemory();
            var max = nodes.Length - 1;
            var min = 0;

            while (max >= min)
            {
                var mid = min + ((max - min) >> 1);

                var comparison = s_caseInsensitiveComparer.Compare(
                    GetNameSlice(nodes, mid), nameSlice);
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

        /// <summary>
        /// Cache the symbol tree infos for assembly symbols that share the same underlying metadata. Generating symbol
        /// trees for metadata can be expensive (in large metadata cases).  And it's common for us to have many threads
        /// to want to search the same metadata simultaneously. As such, we use an AsyncLazy to compute the value that
        /// can be shared among all callers.
        /// </summary>
        private static readonly ConditionalWeakTable<MetadataId, AsyncLazy<SymbolTreeInfo>> s_metadataIdToInfo = new();

        private static Task<SpellChecker> GetSpellCheckerAsync(
            HostWorkspaceServices services, SolutionKey solutionKey, Checksum checksum, string filePath, ImmutableArray<Node> sortedNodes)
        {
            // Create a new task to attempt to load or create the spell checker for this 
            // SymbolTreeInfo.  This way the SymbolTreeInfo will be ready immediately
            // for non-fuzzy searches, and soon afterwards it will be able to perform
            // fuzzy searches as well.
            return Task.Run(() => LoadOrCreateSpellCheckerAsync(services, solutionKey, checksum, filePath, sortedNodes));
        }

        private static Task<SpellChecker> CreateSpellCheckerAsync(
            Checksum checksum, ImmutableArray<Node> sortedNodes)
        {
            return Task.FromResult(new SpellChecker(
                checksum, sortedNodes.Select(n => n.Name.AsMemory())));
        }

        private static void SortNodes(
            ImmutableArray<BuilderNode> unsortedNodes,
            out ImmutableArray<Node> sortedNodes)
        {
            // Generate index numbers from 0 to Count-1
            var tmp = new int[unsortedNodes.Length];
            for (var i = 0; i < tmp.Length; i++)
            {
                tmp[i] = i;
            }

            // Sort the index according to node elements
            Array.Sort<int>(tmp, (a, b) => CompareNodes(unsortedNodes[a], unsortedNodes[b], unsortedNodes));

            // Use the sort order to build the ranking table which will
            // be used as the map from original (unsorted) location to the
            // sorted location.
            var ranking = new int[unsortedNodes.Length];
            for (var i = 0; i < tmp.Length; i++)
            {
                ranking[tmp[i]] = i;
            }

            // No longer need the tmp array
            tmp = null;

            var result = ArrayBuilder<Node>.GetInstance(unsortedNodes.Length);
            result.Count = unsortedNodes.Length;

            string? lastName = null;

            // Copy nodes into the result array in the appropriate order and fixing
            // up parent indexes as we go.
            for (var i = 0; i < unsortedNodes.Length; i++)
            {
                var n = unsortedNodes[i];
                var currentName = n.Name;

                // De-duplicate identical strings
                if (currentName == lastName)
                {
                    currentName = lastName;
                }

                result[ranking[i]] = new Node(
                    currentName,
                    n.IsRoot ? n.ParentIndex : ranking[n.ParentIndex]);

                lastName = currentName;
            }

            sortedNodes = result.ToImmutableAndFree();
        }

        private static int CompareNodes(
            BuilderNode x, BuilderNode y, ImmutableArray<BuilderNode> nodeList)
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
        private void Bind(
            int index, INamespaceOrTypeSymbol rootContainer, ref TemporaryArray<ISymbol> results, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = _nodes[index];
            if (node.IsRoot)
            {
                return;
            }

            if (_nodes[node.ParentIndex].IsRoot)
            {
                var members = rootContainer.GetMembers(node.Name);
                results.AddRange(members);
            }
            else
            {
                using var containerSymbols = TemporaryArray<ISymbol>.Empty;
                Bind(node.ParentIndex, rootContainer, ref containerSymbols.AsRef(), cancellationToken);
                foreach (var containerSymbol in containerSymbols)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (containerSymbol is INamespaceOrTypeSymbol nsOrType)
                    {
                        var members = nsOrType.GetMembers(node.Name);
                        results.AddRange(members);
                    }
                }
            }
        }

        #endregion

        internal void AssertEquivalentTo(SymbolTreeInfo other)
        {
            Debug.Assert(Checksum.Equals(other.Checksum));
            Debug.Assert(_nodes.Length == other._nodes.Length);

            for (int i = 0, n = _nodes.Length; i < n; i++)
            {
                _nodes[i].AssertEquivalentTo(other._nodes[i]);
            }

            Debug.Assert(_inheritanceMap.Keys.Count == other._inheritanceMap.Keys.Count);
            var orderedKeys1 = _inheritanceMap.Keys.Order().ToList();

            for (var i = 0; i < orderedKeys1.Count; i++)
            {
                var values1 = _inheritanceMap[i];
                var values2 = other._inheritanceMap[i];

                Debug.Assert(values1.Length == values2.Length);
                for (var j = 0; j < values1.Length; j++)
                {
                    Debug.Assert(values1[j] == values2[j]);
                }
            }
        }

        private static SymbolTreeInfo CreateSymbolTreeInfo(
            HostWorkspaceServices services, SolutionKey solutionKey, Checksum checksum,
            string filePath, ImmutableArray<BuilderNode> unsortedNodes,
            OrderPreservingMultiDictionary<string, string> inheritanceMap,
            MultiDictionary<string, ExtensionMethodInfo> simpleMethods)
        {
            SortNodes(unsortedNodes, out var sortedNodes);
            var createSpellCheckerTask = GetSpellCheckerAsync(
                services, solutionKey, checksum, filePath, sortedNodes);

            return new SymbolTreeInfo(
                checksum, sortedNodes, createSpellCheckerTask, inheritanceMap, simpleMethods);
        }

        private static OrderPreservingMultiDictionary<int, int> CreateIndexBasedInheritanceMap(
            ImmutableArray<Node> nodes,
            OrderPreservingMultiDictionary<string, string> inheritanceMap)
        {
            // All names in metadata will be case sensitive.  
            var comparer = GetComparer(ignoreCase: false);
            var result = new OrderPreservingMultiDictionary<int, int>();

            foreach (var (baseName, derivedNames) in inheritanceMap)
            {
                var baseNameIndex = BinarySearch(nodes, baseName);
                Debug.Assert(baseNameIndex >= 0);

                foreach (var derivedName in derivedNames)
                {
                    foreach (var derivedNameIndex in FindNodeIndices(nodes, derivedName, comparer))
                    {
                        result.Add(baseNameIndex, derivedNameIndex);
                    }
                }
            }

            return result;
        }

        public ImmutableArray<INamedTypeSymbol> GetDerivedMetadataTypes(
            string baseTypeName, Compilation compilation, CancellationToken cancellationToken)
        {
            var baseTypeNameIndex = BinarySearch(baseTypeName);
            var derivedTypeIndices = _inheritanceMap[baseTypeNameIndex];

            using var builder = TemporaryArray<INamedTypeSymbol>.Empty;
            using var tempBuilder = TemporaryArray<ISymbol>.Empty;

            foreach (var derivedTypeIndex in derivedTypeIndices)
            {
                tempBuilder.Clear();

                Bind(derivedTypeIndex, compilation.GlobalNamespace, ref tempBuilder.AsRef(), cancellationToken);
                foreach (var symbol in tempBuilder)
                {
                    if (symbol is INamedTypeSymbol namedType)
                    {
                        builder.Add(namedType);
                    }
                }
            }

            return builder.ToImmutableAndClear();
        }
    }
}
