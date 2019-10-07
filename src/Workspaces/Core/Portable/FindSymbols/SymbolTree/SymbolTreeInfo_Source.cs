// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private static SimplePool<MultiDictionary<string, ISymbol>> s_symbolMapPool =
            new SimplePool<MultiDictionary<string, ISymbol>>(() => new MultiDictionary<string, ISymbol>());

        private static MultiDictionary<string, ISymbol> AllocateSymbolMap()
            => s_symbolMapPool.Allocate();

        private static void FreeSymbolMap(MultiDictionary<string, ISymbol> symbolMap)
        {
            symbolMap.Clear();
            s_symbolMapPool.Free(symbolMap);
        }

        public static Task<SymbolTreeInfo> GetInfoForSourceAssemblyAsync(
            Project project, Checksum checksum, CancellationToken cancellationToken)
        {
            var result = TryLoadOrCreateAsync(
                project.Solution,
                checksum,
                loadOnly: false,
                createAsync: () => CreateSourceSymbolTreeInfoAsync(project, checksum, cancellationToken),
                keySuffix: "_Source_" + project.FilePath,
                tryReadObject: reader => TryReadSymbolTreeInfo(reader, checksum, (names, nodes) => GetSpellCheckerTask(project.Solution, checksum, project.FilePath, names, nodes)),
                cancellationToken: cancellationToken);
            Contract.ThrowIfNull(result, "Result should never be null as we passed 'loadOnly: false'.");
            return result;
        }

        /// <summary>
        /// Cache of project to the checksum for it so that we don't have to expensively recompute
        /// this each time we get a project.
        /// </summary>
        private static ConditionalWeakTable<ProjectState, AsyncLazy<Checksum>> s_projectToSourceChecksum =
            new ConditionalWeakTable<ProjectState, AsyncLazy<Checksum>>();

        public static Task<Checksum> GetSourceSymbolsChecksumAsync(Project project, CancellationToken cancellationToken)
        {
            var lazy = s_projectToSourceChecksum.GetValue(
                project.State, p => new AsyncLazy<Checksum>(c => ComputeSourceSymbolsChecksumAsync(p, c), cacheResult: true));

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
            var serializer = projectState.LanguageServices.WorkspaceServices.GetService<ISerializerService>();
            var projectStateChecksums = await projectState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            // Order the documents by FilePath.  Default ordering in the RemoteWorkspace is
            // to be ordered by Guid (which is not consistent across VS sessions).
            var textChecksumsTasks = projectState.DocumentStates.OrderBy(d => d.Value.FilePath, StringComparer.Ordinal).Select(async d =>
            {
                var documentStateChecksum = await d.Value.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                return documentStateChecksum.Text;
            });

            var compilationOptionsChecksum = projectStateChecksums.CompilationOptions;
            var parseOptionsChecksum = projectStateChecksums.ParseOptions;
            var textChecksums = await Task.WhenAll(textChecksumsTasks).ConfigureAwait(false);

            var allChecksums = ArrayBuilder<Checksum>.GetInstance();
            try
            {
                allChecksums.AddRange(textChecksums);
                allChecksums.Add(compilationOptionsChecksum);
                allChecksums.Add(parseOptionsChecksum);

                // Include serialization format version in our checksum.  That way if the 
                // version ever changes, all persisted data won't match the current checksum
                // we expect, and we'll recompute things.
                allChecksums.Add(SerializationFormatChecksum);

                var checksum = Checksum.Create(WellKnownSynchronizationKind.SymbolTreeInfo, allChecksums);
                return checksum;
            }
            finally
            {
                allChecksums.Free();
            }
        }

        internal static async Task<SymbolTreeInfo> CreateSourceSymbolTreeInfoAsync(
            Project project, Checksum checksum, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var assembly = compilation?.Assembly;
            if (assembly == null)
            {
                return CreateEmpty(checksum);
            }

            var unsortedNodes = ArrayBuilder<BuilderNode>.GetInstance();
            unsortedNodes.Add(new BuilderNode(assembly.GlobalNamespace.Name, RootNodeParentIndex));

            GenerateSourceNodes(assembly.GlobalNamespace, unsortedNodes, s_getMembersNoPrivate);

            return CreateSymbolTreeInfo(
                project.Solution, checksum, project.FilePath, unsortedNodes.ToImmutableAndFree(),
                inheritanceMap: new OrderPreservingMultiDictionary<string, string>(),
                null,
                ImmutableArray<ExtensionMethodInfo>.Empty);
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

                foreach (var kvp in symbolMap)
                {
                    GenerateSourceNodes(kvp.Key, 0 /*index of root node*/, kvp.Value, list, lookup);
                }
            }
            finally
            {
                FreeSymbolMap(symbolMap);
            }
        }

        private static readonly Func<ISymbol, bool> s_useSymbolNoPrivate =
            s => s.CanBeReferencedByName && s.DeclaredAccessibility != Accessibility.Private;

        private static readonly Func<ISymbol, bool> s_useSymbolNoPrivateOrInternal =
            s => s.CanBeReferencedByName &&
            s.DeclaredAccessibility != Accessibility.Private &&
            s.DeclaredAccessibility != Accessibility.Internal;

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

                foreach (var kvp in symbolMap)
                {
                    GenerateSourceNodes(kvp.Key, nodeIndex, kvp.Value, list, lookup);
                }
            }
            finally
            {
                FreeSymbolMap(symbolMap);
            }
        }

        private static Action<ISymbol, MultiDictionary<string, ISymbol>> s_getMembersNoPrivate =
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
