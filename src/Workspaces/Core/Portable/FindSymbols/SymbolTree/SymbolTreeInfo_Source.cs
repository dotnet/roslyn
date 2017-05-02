// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Serialization;
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
            Project project, Checksum checksum, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            return await LoadOrCreateSourceSymbolTreeInfoAsync(
                project.Solution, compilation.Assembly, checksum, project.FilePath,
                loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public static async Task<Checksum> GetSourceSymbolsChecksumAsync(Project project, CancellationToken cancellationToken)
        {
            // The SymbolTree for source is built from the source-symbols from the project's compilation's
            // assembly.  Specifically, we only get the name, kind and parent/child relationship of all the
            // child symbols.  So we want to be able to reuse the index as long as none of these have 
            // changed.  The only thing that can make those source-symbols change in that manner are if
            // the text of any document changes, or if options for the project change.  So we build our
            // checksum out of that data.
            var serializer = new Serializer(project.Solution.Workspace);
            var projectStateChecksums = await project.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            var textChecksumsTasks = project.Documents.Select(async d =>
            {
                var documentStateChecksum = await d.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
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

                var checksum = Checksum.Create(nameof(SymbolTreeInfo), allChecksums);
                return checksum;
            }
            finally
            {
                allChecksums.Free();
            }
        }

        internal static SymbolTreeInfo CreateSourceSymbolTreeInfo(
            Solution solution, Checksum checksum, IAssemblySymbol assembly,
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
                solution, checksum, filePath, unsortedNodes.ToImmutableAndFree(), 
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