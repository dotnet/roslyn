// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    using SymbolMap = ImmutableDictionary<INamespaceOrTypeSymbol, IAliasSymbol>;
    using TreeMap = ConcurrentDictionary<(SyntaxTree tree, int namespaceId), ImmutableDictionary<INamespaceOrTypeSymbol, IAliasSymbol>>;

    internal static class AliasSymbolCache
    {
        // NOTE : I chose to cache on compilation assuming this cache will be quite small. usually number of times alias is used is quite small.
        //        but if that turns out not true, we can move this cache to be based on semantic model. unlike compilation that would be cached
        //        in compilation cache in certain host (VS), semantic model comes and goes more frequently which will release cache more often.
        private static readonly ConditionalWeakTable<Compilation, TreeMap> s_treeAliasMap = new ConditionalWeakTable<Compilation, TreeMap>();
        private static readonly ConditionalWeakTable<Compilation, TreeMap>.CreateValueCallback s_createTreeMap = c => new TreeMap();
        private static readonly Func<ISymbol, string> s_symbolToName = s => s.Name;

        public static bool TryGetAliasSymbol(SemanticModel semanticModel, int namespaceId, INamespaceOrTypeSymbol targetSymbol, out IAliasSymbol aliasSymbol)
        {
            // TODO: given semantic model must be not speculative semantic model for now. 
            // currently it can't be checked since it is not exposed to common layer yet.
            // once exposed, this method itself will make sure it use original semantic model
            aliasSymbol = null;
            if (!s_treeAliasMap.TryGetValue(semanticModel.Compilation, out var treeMap) ||
                !treeMap.TryGetValue((semanticModel.SyntaxTree, namespaceId), out var symbolMap))
            {
                return false;
            }

            symbolMap.TryGetValue(targetSymbol, out aliasSymbol);
            return true;
        }

        public static void AddAliasSymbols(SemanticModel semanticModel, int namespaceId, IEnumerable<IAliasSymbol> aliasSymbols)
        {
            // given semantic model must be the original semantic model for now
            var treeMap = s_treeAliasMap.GetValue(semanticModel.Compilation, s_createTreeMap);

            // check again to see whether somebody has beaten us
            var key = (tree: semanticModel.SyntaxTree, namespaceId);
            if (treeMap.ContainsKey(key))
            {
                return;
            }

            var builder = ImmutableDictionary.CreateBuilder<INamespaceOrTypeSymbol, IAliasSymbol>();
            foreach (var alias in aliasSymbols)
            {
                if (builder.ContainsKey(alias.Target))
                {
                    continue;
                }

                // only put the first one.
                builder.Add(alias.Target, alias);
            }

            // Use namespace id rather than holding onto namespace node directly, that will keep the tree alive as long as
            // the compilation is alive. In the current design, a node can come and go even if compilation is alive through recoverable tree.
            treeMap.TryAdd(key, builder.ToImmutable());
        }
    }
}
