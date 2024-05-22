// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal partial class SymbolTreeInfo
{
    private static readonly SimplePool<MultiDictionary<string, INamespaceOrTypeSymbol>> s_symbolMapPool = new(() => []);

    private static MultiDictionary<string, INamespaceOrTypeSymbol> AllocateSymbolMap()
        => s_symbolMapPool.Allocate();

    private static void FreeSymbolMap(MultiDictionary<string, INamespaceOrTypeSymbol> symbolMap)
    {
        symbolMap.Clear();
        s_symbolMapPool.Free(symbolMap);
    }

    private static string GetSourceKeySuffix(Project project)
        => "_Source_" + project.FilePath;

    public static Task<SymbolTreeInfo> GetInfoForSourceAssemblyAsync(
        Project project, Checksum checksum, CancellationToken cancellationToken)
    {
        var solution = project.Solution;

        return LoadOrCreateAsync(
            solution.Services,
            SolutionKey.ToSolutionKey(solution),
            checksum,
            createAsync: checksum => CreateSourceSymbolTreeInfoAsync(project, checksum, cancellationToken),
            keySuffix: GetSourceKeySuffix(project),
            cancellationToken);
    }

    /// <summary>
    /// Loads any info we have for this project from our persistence store.  Will succeed regardless of the
    /// checksum of the <paramref name="project"/>.  Should only be used by clients that are ok with potentially
    /// stale data.
    /// </summary>
    public static async Task<SymbolTreeInfo?> LoadAnyInfoForSourceAssemblyAsync(
        Project project, CancellationToken cancellationToken)
    {
        return await LoadAsync(
            project.Solution.Services,
            SolutionKey.ToSolutionKey(project.Solution),
            checksum: await GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false),
            checksumMustMatch: false,
            GetSourceKeySuffix(project),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cache of project to the checksum for it so that we don't have to expensively recompute
    /// this each time we get a project.
    /// </summary>
    private static readonly ConditionalWeakTable<ProjectState, AsyncLazy<Checksum>> s_projectToSourceChecksum = new();

    public static Task<Checksum> GetSourceSymbolsChecksumAsync(Project project, CancellationToken cancellationToken)
    {
        var lazy = s_projectToSourceChecksum.GetValue(
            project.State,
            static p => AsyncLazy.Create(
                static (p, c) => ComputeSourceSymbolsChecksumAsync(p, c),
                arg: p));

        return lazy.GetValueAsync(cancellationToken);
    }

    private static async Task<Checksum> ComputeSourceSymbolsChecksumAsync(ProjectState projectState, CancellationToken cancellationToken)
    {
        // The SymbolTree for source is built from the source-symbols from the project's compilation's
        // assembly.  Specifically, we only get the name, kind and parent/child relationship of all the
        // child symbols.  So we want to be able to reuse the index as long as none of these have 
        // changed.  The only thing that can make those source-symbols change in that manner are if
        // the text of any document changes, or if options for the project change.  So we build our
        // checksum out of that data.
        var serializer = projectState.LanguageServices.SolutionServices.GetService<ISerializerService>();
        var projectStateChecksums = await projectState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

        // Order the documents by FilePath.  Default ordering in the RemoteWorkspace is
        // to be ordered by Guid (which is not consistent across VS sessions).
        var textChecksumsTasks = projectState.DocumentStates.States.Values.OrderBy(state => state.FilePath, StringComparer.Ordinal).Select(async state =>
        {
            var documentStateChecksum = await state.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            return documentStateChecksum.Text;
        });

        var compilationOptionsChecksum = projectStateChecksums.CompilationOptions;
        var parseOptionsChecksum = projectStateChecksums.ParseOptions;
        var textChecksums = await Task.WhenAll(textChecksumsTasks).ConfigureAwait(false);

        using var _ = ArrayBuilder<Checksum>.GetInstance(out var allChecksums);

        allChecksums.AddRange(textChecksums);
        allChecksums.Add(compilationOptionsChecksum);
        allChecksums.Add(parseOptionsChecksum);

        // Include serialization format version in our checksum.  That way if the 
        // version ever changes, all persisted data won't match the current checksum
        // we expect, and we'll recompute things.
        allChecksums.Add(SerializationFormatChecksum);

        return Checksum.Create(allChecksums);
    }

    internal static async ValueTask<SymbolTreeInfo> CreateSourceSymbolTreeInfoAsync(
        Project project, Checksum checksum, CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var assembly = compilation?.Assembly;
        if (assembly == null)
            return CreateEmpty(checksum);

        var symbolMap = AllocateSymbolMap();
        try
        {
            // generate nodes for the global namespace and all descendants
            using var _ = ArrayBuilder<BuilderNode>.GetInstance(out var unsortedBuilderNodes);

            var globalNamespaceName = assembly.GlobalNamespace.Name;
            symbolMap.Add(globalNamespaceName, assembly.GlobalNamespace);
            GenerateSourceNodes(globalNamespaceName, RootNodeParentIndex, symbolMap[globalNamespaceName], unsortedBuilderNodes);

            return CreateSymbolTreeInfo(
                checksum,
                unsortedBuilderNodes.ToImmutable(),
                inheritanceMap: [],
                receiverTypeNameToExtensionMethodMap: null);
        }
        finally
        {
            FreeSymbolMap(symbolMap);
        }
    }

    private static void GenerateSourceNodes(
        string name,
        int parentIndex,
        MultiDictionary<string, INamespaceOrTypeSymbol>.ValueSet symbolsWithSameName,
        ArrayBuilder<BuilderNode> list)
    {
        // Add the node for this name, and record which parent it points at.  And keep track of the index of the
        // node we just added.
        var node = new BuilderNode(name, parentIndex);
        var nodeIndex = list.Count;
        list.Add(node);

        var symbolMap = AllocateSymbolMap();
        using var _ = PooledHashSet<string>.GetInstance(out var seenNames);
        try
        {
            // Walk the symbols with this name, and add all their child namespaces and types, grouping them together
            // based on their name.  There may be multiple (for example, Action<T1>, Action<T1, T2>, etc.)
            foreach (var symbol in symbolsWithSameName)
                AddChildNamespacesAndTypes(symbol, symbolMap);

            // Now, go through all those groups and make the single mapping from their name to the builder-node we
            // just created above, and recurse into their children as well.
            foreach (var (childName, childSymbols) in symbolMap)
            {
                seenNames.Add(childName);
                GenerateSourceNodes(childName, nodeIndex, childSymbols, list);
            }

            // The above loops only create nodes for namespaces and types.  we also want nodes for members as well.
            // However, we do not want to force the symbols for those members to be created just to get the names.
            //
            // So walk through the symbols again, and for the named-types grab all the member-names contained
            // therein.  If we didn't already see that child name when recursing above, then make a builder-node for
            // it that points to the builder-node we just created above.

            foreach (var symbol in symbolsWithSameName)
            {
                if (symbol is INamedTypeSymbol namedType)
                {
                    foreach (var childMemberName in namedType.MemberNames)
                    {
                        if (seenNames.Add(childMemberName))
                            list.Add(new BuilderNode(childMemberName, nodeIndex));
                    }
                }
            }
        }
        finally
        {
            FreeSymbolMap(symbolMap);
        }
    }

    private static void AddChildNamespacesAndTypes(INamespaceOrTypeSymbol symbol, MultiDictionary<string, INamespaceOrTypeSymbol> symbolMap)
    {
        if (symbol is INamespaceSymbol namespaceSymbol)
        {
            foreach (var childNamespaceOrType in namespaceSymbol.GetMembers())
                symbolMap.Add(childNamespaceOrType.Name, childNamespaceOrType);
        }
        else if (symbol is INamedTypeSymbol namedTypeSymbol)
        {
            // for named-types, we only need to recurse into child types.  Call GetTypeMembers instead of GetMembers
            // so we do not cause all child symbols to be created.
            foreach (var childType in namedTypeSymbol.GetTypeMembers())
                symbolMap.Add(childType.Name, childType);
        }
    }
}
