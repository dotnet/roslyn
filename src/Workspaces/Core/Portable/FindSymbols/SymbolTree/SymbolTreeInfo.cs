// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Represents a tree of names of the namespaces, types (and members within those types) within a <see
/// cref="Project"/> or <see cref="PortableExecutableReference"/>.  This tree can be used to quickly determine if
/// there is a name match, and can provide the named path to that named entity.  This path can then be used to
/// produce a corresponding <see cref="ISymbol"/> that can be used by a feature.  The primary purpose of this index
/// is to allow features to quickly determine that there is <em>no</em> name match, so that acquiring symbols is not
/// necessary.  The secondary purpose is to generate a minimal set of symbols when there is a match, though that
/// will still incur a heavy cost (for example, getting the <see cref="IAssemblySymbol"/> root symbol for a
/// particular project).
/// </summary>
internal sealed partial class SymbolTreeInfo
{
    private static readonly StringComparer s_caseInsensitiveComparer =
        CaseInsensitiveComparison.Comparer;

    public Checksum Checksum { get; }

    /// <summary>
    /// The list of nodes that represent symbols. The primary key into the sorting of this list is the name. They
    /// are sorted case-insensitively . Finding case-sensitive matches can be found by binary searching for
    /// something that matches insensitively, and then searching around that equivalence class for one that matches.
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
    /// Maps the name of receiver type name to its <see cref="ExtensionMemberInfo" />. <see cref="ParameterTypeInfo"/>
    /// for the definition of simple/complex methods. For non-array simple types, the receiver type name would be its
    /// metadata name, e.g. "Int32". For any array types with simple type as element, the receiver type name would be
    /// just "ElementTypeName[]", e.g. "Int32[]" for int[][,] For non-array complex types, the receiver type name is "".
    /// For any array types with complex type as element, the receiver type name is "[]"
    /// </summary>
    private readonly MultiDictionary<string, ExtensionMemberInfo>? _receiverTypeNameToExtensionMemberMap;

    public MultiDictionary<string, ExtensionMemberInfo>.ValueSet GetExtensionMemberInfoForReceiverType(string typeName)
        => _receiverTypeNameToExtensionMemberMap != null
            ? _receiverTypeNameToExtensionMemberMap[typeName]
            : new MultiDictionary<string, ExtensionMemberInfo>.ValueSet(null, null);

    public bool ContainsExtensionMember => _receiverTypeNameToExtensionMemberMap?.Count > 0;

    private SpellChecker? _spellChecker;

    private SymbolTreeInfo(
        Checksum checksum,
        ImmutableArray<Node> sortedNodes,
        OrderPreservingMultiDictionary<string, string> inheritanceMap,
        MultiDictionary<string, ExtensionMemberInfo>? receiverTypeNameToExtensionMemberMap)
        : this(checksum, sortedNodes,
               spellChecker: null,
               CreateIndexBasedInheritanceMap(sortedNodes, inheritanceMap),
               receiverTypeNameToExtensionMemberMap)
    {
    }

    private SymbolTreeInfo(
        Checksum checksum,
        ImmutableArray<Node> sortedNodes,
        SpellChecker? spellChecker,
        OrderPreservingMultiDictionary<int, int> inheritanceMap,
        MultiDictionary<string, ExtensionMemberInfo>? receiverTypeNameToExtensionMemberMap)
    {
        Checksum = checksum;
        _nodes = sortedNodes;
        _spellChecker = spellChecker;
        _inheritanceMap = inheritanceMap;
        _receiverTypeNameToExtensionMemberMap = receiverTypeNameToExtensionMemberMap;
    }

    public static SymbolTreeInfo CreateEmpty(Checksum checksum)
    {
        var unsortedNodes = ImmutableArray.Create(BuilderNode.RootNode);
        var sortedNodes = SortNodes(unsortedNodes);

        return new SymbolTreeInfo(checksum, sortedNodes,
            [],
            []);
    }

    public SymbolTreeInfo WithChecksum(Checksum checksum)
    {
        if (checksum == this.Checksum)
            return this;

        return new SymbolTreeInfo(
            checksum, _nodes, _spellChecker, _inheritanceMap, _receiverTypeNameToExtensionMemberMap);
    }

    public Task<ImmutableArray<ISymbol>> FindAsync(
        SearchQuery query, IAssemblySymbol assembly, SymbolFilter filter, CancellationToken cancellationToken)
    {
        // All entrypoints to this function are Find functions that are only searching
        // for specific strings (i.e. they never do a custom search).
        Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

        return this.FindAsync(
            query, AsyncLazy.Create((IAssemblySymbol?)assembly), filter, cancellationToken);
    }

    public async Task<ImmutableArray<ISymbol>> FindAsync(
        SearchQuery query, AsyncLazy<IAssemblySymbol?> lazyAssembly,
        SymbolFilter filter, CancellationToken cancellationToken)
    {
        // All entrypoints to this function are Find functions that are only searching
        // for specific strings (i.e. they never do a custom search).
        Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

        var symbols = await FindCoreAsync(query, lazyAssembly, cancellationToken).ConfigureAwait(false);

        return DeclarationFinder.FilterByCriteria(symbols, filter);
    }

    private Task<ImmutableArray<ISymbol>> FindCoreAsync(
        SearchQuery query, AsyncLazy<IAssemblySymbol?> lazyAssembly, CancellationToken cancellationToken)
    {
        // All entrypoints to this function are Find functions that are only searching
        // for specific strings (i.e. they never do a custom search).
        Contract.ThrowIfTrue(query.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

        // If the query has a specific string provided, then call into the SymbolTreeInfo
        // helpers optimized for lookup based on an exact name.

        var queryName = query.Name;
        Contract.ThrowIfNull(queryName);

        return query.Kind switch
        {
            SearchKind.Exact => this.FindAsync(lazyAssembly, queryName, ignoreCase: false, cancellationToken: cancellationToken),
            SearchKind.ExactIgnoreCase => this.FindAsync(lazyAssembly, queryName, ignoreCase: true, cancellationToken: cancellationToken),
            SearchKind.Fuzzy => this.FuzzyFindAsync(lazyAssembly, queryName, cancellationToken),
            _ => throw new InvalidOperationException(),
        };
    }

    /// <summary>
    /// Finds symbols in this assembly that match the provided name in a fuzzy manner.
    /// </summary>
    private async Task<ImmutableArray<ISymbol>> FuzzyFindAsync(
        AsyncLazy<IAssemblySymbol?> lazyAssembly, string name, CancellationToken cancellationToken)
    {
        using var similarNames = TemporaryArray<string>.Empty;
        using var result = TemporaryArray<ISymbol>.Empty;

        // Ensure the spell checker is initialized.  This is concurrency safe.  Technically multiple threads may end
        // up overwriting the field, but even if that happens, we are sure to see a fully written spell checker as
        // the runtime guarantees that the initialize of the SpellChecker instnace completely written when we read
        // our field.
        _spellChecker ??= CreateSpellChecker(_nodes);
        _spellChecker.FindSimilarWords(ref similarNames.AsRef(), name, substringsAreSimilar: false);

        foreach (var similarName in similarNames)
        {
            var symbols = await FindAsync(lazyAssembly, similarName, ignoreCase: true, cancellationToken).ConfigureAwait(false);
            result.AddRange(symbols);
        }

        return result.ToImmutableAndClear();
    }

    /// <summary>
    /// Returns <see langword="true"/> if this index contains some symbol that whose name matches <paramref
    /// name="name"/> case <em>sensitively</em>. <see langword="false"/> otherwise.
    /// </summary>
    public bool ContainsSymbolWithName(string name)
    {
        var (startIndexInclusive, endIndexExclusive) = FindCaseInsensitiveNodeIndices(_nodes, name);

        for (var index = startIndexInclusive; index < endIndexExclusive; index++)
        {
            var node = _nodes[index];

            // The find-operation found the case-insensitive range of results.  So since the caller caller wants
            // case-sensitive, then actually check that the node matches case-sensitively
            if (StringComparer.Ordinal.Equals(name, node.Name))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get all symbols that have a name matching the specified name.
    /// </summary>
    private async Task<ImmutableArray<ISymbol>> FindAsync(
        AsyncLazy<IAssemblySymbol?> lazyAssembly,
        string name,
        bool ignoreCase,
        CancellationToken cancellationToken)
    {
        using var results = TemporaryArray<ISymbol>.Empty;

        var (startIndexInclusive, endIndexExclusive) = FindCaseInsensitiveNodeIndices(_nodes, name);

        IAssemblySymbol? assemblySymbol = null;
        for (var index = startIndexInclusive; index < endIndexExclusive; index++)
        {
            var node = _nodes[index];

            // The find-operation found the case-insensitive range of results.  So if the caller wants
            // case-insensitive, then just check all of them.  If they caller wants case-sensitive, then
            // actually check that the node matches case-sensitively
            if (ignoreCase || StringComparer.Ordinal.Equals(name, node.Name))
            {
                assemblySymbol ??= await lazyAssembly.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (assemblySymbol is null)
                    return [];

                Bind(index, assemblySymbol.GlobalNamespace, ref results.AsRef(), cancellationToken);
            }
        }

        return results.ToImmutableAndClear();
    }

    private static (int startIndexInclusive, int endIndexExclusive) FindCaseInsensitiveNodeIndices(
        ImmutableArray<Node> nodes, string name)
    {
        // find any node that matches case-insensitively
        var startingPosition = BinarySearch(nodes, name);

        if (startingPosition == -1)
            return default;

        var startIndex = startingPosition;
        while (startIndex > 0 && s_caseInsensitiveComparer.Equals(nodes[startIndex - 1].Name, name))
            startIndex--;

        var endIndex = startingPosition;
        while (endIndex + 1 < nodes.Length && s_caseInsensitiveComparer.Equals(nodes[endIndex + 1].Name, name))
            endIndex++;

        return (startIndex, endIndex + 1);
    }

    private int BinarySearch(string name)
        => BinarySearch(_nodes, name);

    /// <summary>
    /// Searches for a name in the ordered list that matches per the <see cref="s_caseInsensitiveComparer" />.
    /// </summary>
    private static int BinarySearch(ImmutableArray<Node> nodes, string name)
    {
        var max = nodes.Length - 1;
        var min = 0;

        while (max >= min)
        {
            var mid = min + ((max - min) >> 1);

            var comparison = s_caseInsensitiveComparer.Compare(nodes[mid].Name, name);
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

    private static SpellChecker CreateSpellChecker(ImmutableArray<Node> sortedNodes)
        => new(sortedNodes.Select(n => n.Name));

    private static ImmutableArray<Node> SortNodes(ImmutableArray<BuilderNode> unsortedNodes)
    {
        // Generate index numbers from 0 to Count-1
        using var _1 = ArrayBuilder<int>.GetInstance(unsortedNodes.Length, out var tmp);
        tmp.Count = unsortedNodes.Length;
        for (var i = 0; i < tmp.Count; i++)
            tmp[i] = i;

        // Sort the index according to node elements
        tmp.Sort((a, b) => CompareNodes(unsortedNodes[a], unsortedNodes[b], unsortedNodes));

        // Use the sort order to build the ranking table which will
        // be used as the map from original (unsorted) location to the
        // sorted location.
        using var _2 = ArrayBuilder<int>.GetInstance(unsortedNodes.Length, out var ranking);
        ranking.Count = unsortedNodes.Length;
        for (var i = 0; i < tmp.Count; i++)
            ranking[tmp[i]] = i;

        using var _3 = ArrayBuilder<Node>.GetInstance(unsortedNodes.Length, out var result);
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

        return result.ToImmutableAndClear();
    }

    private static int CompareNodes(
        BuilderNode x, BuilderNode y, ImmutableArray<BuilderNode> nodeList)
    {
        var comp = TotalComparer(x.Name, y.Name);
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

        // We first sort in a case insensitive manner.  But, within items that match insensitively, 
        // we then sort in a case sensitive manner.  This helps for searching as we'll walk all 
        // the items of a specific casing at once.  This way features can cache values for that
        // casing and reuse them.  i.e. if we didn't do this we might get "Prop, prop, Prop, prop"
        // which might cause other features to continually recalculate if that string matches what
        // they're searching for.  However, with this sort of comparison we now get 
        // "prop, prop, Prop, Prop".  Features can take advantage of that by caching their previous
        // result and reusing it when they see they're getting the same string again.
        static int TotalComparer(string s1, string s2)
        {
            var diff = CaseInsensitiveComparison.Comparer.Compare(s1, s2);
            return diff != 0
                ? diff
                : StringComparer.Ordinal.Compare(s1, s2);
        }
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
        Checksum checksum,
        ImmutableArray<BuilderNode> unsortedNodes,
        OrderPreservingMultiDictionary<string, string> inheritanceMap,
        MultiDictionary<string, ExtensionMemberInfo>? receiverTypeNameToExtensionMemberMap)
    {
        var sortedNodes = SortNodes(unsortedNodes);

        return new SymbolTreeInfo(
            checksum, sortedNodes, inheritanceMap, receiverTypeNameToExtensionMemberMap);
    }

    private static OrderPreservingMultiDictionary<int, int> CreateIndexBasedInheritanceMap(
        ImmutableArray<Node> nodes,
        OrderPreservingMultiDictionary<string, string> inheritanceMap)
    {
        var result = new OrderPreservingMultiDictionary<int, int>();

        foreach (var (baseName, derivedNames) in inheritanceMap)
        {
            var baseNameIndex = BinarySearch(nodes, baseName);
            Debug.Assert(baseNameIndex >= 0);

            foreach (var derivedName in derivedNames)
            {
                var (startIndexInclusive, endIndexExclusive) = FindCaseInsensitiveNodeIndices(nodes, derivedName);

                for (var derivedNameIndex = startIndexInclusive; derivedNameIndex < endIndexExclusive; derivedNameIndex++)
                {
                    var node = nodes[derivedNameIndex];
                    // All names in metadata will be case sensitive.
                    if (StringComparer.Ordinal.Equals(derivedName, node.Name))
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
