// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private static readonly SimplePool<MultiDictionary<string, ISymbol>> s_symbolMapPool =
            new(() => new MultiDictionary<string, ISymbol>());

        private static MultiDictionary<string, ISymbol> AllocateSymbolMap()
            => s_symbolMapPool.Allocate();

        private static void FreeSymbolMap(MultiDictionary<string, ISymbol> symbolMap)
        {
            symbolMap.Clear();
            s_symbolMapPool.Free(symbolMap);
        }

        private static string GetSourceKeySuffix(Project project)
            => "_Source_" + project.FilePath;

        public static Task<SymbolTreeInfo> GetInfoForSourceAssemblyAsync(
            Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            return LoadOrCreateAsync(
                solution.Services,
                SolutionKey.ToSolutionKey(solution),
                getChecksumAsync: async () => await GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false),
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
                project.State, static p => new AsyncLazy<Checksum>(c => ComputeSourceSymbolsChecksumAsync(p, c), cacheResult: true));

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
            var serializer = projectState.LanguageServices.LanguageServices.SolutionServices.GetService<ISerializerService>();
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

            var unsortedNodes = ArrayBuilder<BuilderNode>.GetInstance();
            unsortedNodes.Add(new BuilderNode(assembly.GlobalNamespace.Name, RootNodeParentIndex));

            GenerateSourceNodes(assembly.GlobalNamespace, unsortedNodes, s_getMembersNoPrivate);

            return CreateSymbolTreeInfo(
                checksum,
                unsortedNodes.ToImmutableAndFree(),
                inheritanceMap: new OrderPreservingMultiDictionary<string, string>(),
                receiverTypeNameToExtensionMethodMap: null);
        }

        // generate nodes for the global namespace an all descendants
        private static void GenerateSourceNodes(
            INamespaceSymbol globalNamespace,
            ArrayBuilder<BuilderNode> list,
            Action<ISymbol, MultiDictionary<string, ISymbol>> lookup)
        {
            // Add all child members
            var symbolMap = AllocateSymbolMap();
            try
            {
                lookup(globalNamespace, symbolMap);

                foreach (var (name, symbols) in symbolMap)
                    GenerateSourceNodes(name, 0 /*index of root node*/, symbols, list, lookup);
            }
            finally
            {
                FreeSymbolMap(symbolMap);
            }
        }

        private static readonly Func<ISymbol, bool> s_useSymbolNoPrivate =
            s => s.CanBeReferencedByName && s.DeclaredAccessibility != Accessibility.Private;

        // generate nodes for symbols that share the same name, and all their descendants
        private static void GenerateSourceNodes(
            string name,
            int parentIndex,
            MultiDictionary<string, ISymbol>.ValueSet symbolsWithSameName,
            ArrayBuilder<BuilderNode> list,
            Action<ISymbol, MultiDictionary<string, ISymbol>> lookup)
        {
            var node = new BuilderNode(name, parentIndex);
            var nodeIndex = list.Count;
            list.Add(node);

            var symbolMap = AllocateSymbolMap();
            try
            {
                // Add all child members
                foreach (var symbol in symbolsWithSameName)
                {
                    lookup(symbol, symbolMap);
                }

                foreach (var (symbolName, symbols) in symbolMap)
                    GenerateSourceNodes(symbolName, nodeIndex, symbols, list, lookup);
            }
            finally
            {
                FreeSymbolMap(symbolMap);
            }
        }

        private static readonly Action<ISymbol, MultiDictionary<string, ISymbol>> s_getMembersNoPrivate =
            (symbol, symbolMap) => AddSymbol(symbol, symbolMap, s_useSymbolNoPrivate);

        private static void AddSymbol(ISymbol symbol, MultiDictionary<string, ISymbol> symbolMap, Func<ISymbol, bool> useSymbol)
        {
            if (symbol is INamespaceOrTypeSymbol nt)
            {
                foreach (var member in nt.GetMembers())
                {
                    if (useSymbol(member))
                    {
                        symbolMap.Add(member.Name, member);
                    }
                }
            }
        }
    }
}
