// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

using TreeMap = ConcurrentDictionary<(SyntaxTree tree, int namespaceId), ImmutableDictionary<INamespaceOrTypeSymbol, IAliasSymbol>>;

internal static class AliasSymbolCache
{
    private static readonly ConditionalWeakTable<Compilation, TreeMap> s_treeAliasMap = new();

    /// <summary>
    /// Returns <see langword="true"/> if items were already cached for this <paramref name="semanticModel"/> and
    /// <paramref name="namespaceId"/>, <see langword="false"/> otherwise.  Callers should use this value to
    /// determine if they should call <see cref="AddAliasSymbols"/> or not.  A result of <see langword="true"/> does
    /// *not* mean that <paramref name="aliasSymbol"/> is non-<see langword="null"/>.
    /// </summary>
    public static bool TryGetAliasSymbol(
        SemanticModel semanticModel,
        int namespaceId,
        INamespaceOrTypeSymbol targetSymbol,
        out IAliasSymbol? aliasSymbol)
    {
        semanticModel = semanticModel.GetOriginalSemanticModel();

        aliasSymbol = null;
        if (!s_treeAliasMap.TryGetValue(semanticModel.Compilation, out var treeMap) ||
            !treeMap.TryGetValue((semanticModel.SyntaxTree, namespaceId), out var symbolMap))
        {
            // maps aren't available.  Caller needs to call back into us to add aliases for this scope.
            return false;
        }

        // map was available.  see if it contains an alias to this target.  This is considered successful regardless
        // of whether we find a mapping or not.
        symbolMap.TryGetValue(targetSymbol, out aliasSymbol);
        return true;
    }

    public static void AddAliasSymbols(SemanticModel semanticModel, int namespaceId, IEnumerable<IAliasSymbol> aliasSymbols)
    {
        // given semantic model must be the original semantic model for now
        var treeMap = s_treeAliasMap.GetValue(semanticModel.Compilation, static _ => new TreeMap());

        // check again to see whether somebody has beaten us
        var key = (tree: semanticModel.SyntaxTree, namespaceId);
        if (treeMap.ContainsKey(key))
            return;

        var builder = ImmutableDictionary.CreateBuilder<INamespaceOrTypeSymbol, IAliasSymbol>();
        foreach (var alias in aliasSymbols)
        {
            if (builder.ContainsKey(alias.Target))
                continue;

            // only put the first one.
            builder.Add(alias.Target, alias);
        }

        // Use namespace id rather than holding onto namespace node directly, that will keep the tree alive as long
        // as the compilation is alive. In the current design, a node can come and go even if compilation is alive
        // through recoverable tree.
        treeMap.TryAdd(key, builder.ToImmutable());
    }
}
