// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
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
                tryReadObject: reader => TryReadSymbolTreeInfo(reader, (names, nodes) => GetSpellCheckerTask(project.Solution, checksum, project.FilePath, names, nodes)),
                cancellationToken: cancellationToken);
            Contract.ThrowIfNull(result, "Result should never be null as we passed 'loadOnly: false'.");
            return result;
        }

        public static Task<Checksum> GetSourceSymbolsChecksumAsync(Project project, CancellationToken cancellationToken)
            => project.State.GetChecksumAsync(cancellationToken);

        internal static async Task<SymbolTreeInfo> CreateSourceSymbolTreeInfoAsync(
            Project project, Checksum checksum, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Note: we build our index from the source assembly for a compilation.  As such,
            // the index only contains information about the source symbols defined in this 
            // compilation.  If that ever changes then we need to change how we cache and recompute
            // data in SymbolTreeInfoIncrementalAnalyzerProvider.
            var assembly = compilation.Assembly;
            if (assembly == null)
            {
                return CreateEmpty(checksum);
            }

            var unsortedNodes = ArrayBuilder<BuilderNode>.GetInstance();
            unsortedNodes.Add(new BuilderNode(assembly.GlobalNamespace.Name, RootNodeParentIndex));

            GenerateSourceNodes(assembly.GlobalNamespace, unsortedNodes, s_getMembersNoPrivate);

            return CreateSymbolTreeInfo(
                project.Solution, checksum, project.FilePath, unsortedNodes.ToImmutableAndFree(), 
                inheritanceMap: new OrderPreservingMultiDictionary<string, string>());
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
            var nt = symbol as INamespaceOrTypeSymbol;
            if (nt != null)
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
