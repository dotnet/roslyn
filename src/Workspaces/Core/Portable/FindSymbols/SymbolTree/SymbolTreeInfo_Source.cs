using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private static SimplePool<MultiDictionary<string, ISymbol>> s_symbolMapPool =
            new SimplePool<MultiDictionary<string, ISymbol>>(() => new MultiDictionary<string, ISymbol>());

        private static MultiDictionary<string, ISymbol> AllocateSymbolMap()
        {
            return s_symbolMapPool.Allocate();
        }

        private static void FreeSymbolMap(MultiDictionary<string, ISymbol> symbolMap)
        {
            symbolMap.Clear();
            s_symbolMapPool.Free(symbolMap);
        }

        public static async Task<SymbolTreeInfo> GetInfoForSourceAssemblyAsync(
            Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            return await LoadOrCreateSourceSymbolTreeInfoAsync(
                project.Solution, compilation.Assembly, project.FilePath,
                loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        internal static SymbolTreeInfo CreateSourceSymbolTreeInfo(
            Solution solution, VersionStamp version, IAssemblySymbol assembly,
            string filePath, CancellationToken cancellationToken)
        {
            if (assembly == null)
            {
                return null;
            }

            var unsortedNodes = ArrayBuilder<BuilderNode>.GetInstance();
            unsortedNodes.Add(new BuilderNode(assembly.GlobalNamespace.Name, RootNodeParentIndex));

            GenerateSourceNodes(assembly.GlobalNamespace, unsortedNodes, s_getMembersNoPrivate);

            return CreateSymbolTreeInfo(
                solution, version, filePath, unsortedNodes.ToImmutableAndFree(), 
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