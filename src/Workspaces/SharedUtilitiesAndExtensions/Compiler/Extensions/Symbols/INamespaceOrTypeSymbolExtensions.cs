// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class INamespaceOrTypeSymbolExtensions
{
    private static readonly ConditionalWeakTable<INamespaceOrTypeSymbol, List<string>> s_namespaceOrTypeToNameMap = new();

    private static readonly SymbolDisplayFormat s_shortNameFormat = new(
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable);

    public static string GetShortName(this INamespaceOrTypeSymbol symbol)
        => symbol.ToDisplayString(s_shortNameFormat);

    public static IEnumerable<IPropertySymbol> GetIndexers(this INamespaceOrTypeSymbol? symbol)
    {
        return symbol == null
            ? []
            : symbol.GetMembers(WellKnownMemberNames.Indexer).OfType<IPropertySymbol>().Where(p => p.IsIndexer);
    }

    public static IReadOnlyList<string> GetNameParts(this INamespaceOrTypeSymbol symbol)
        => s_namespaceOrTypeToNameMap.GetValue(symbol, static symbol =>
        {
            var result = new List<string>();
            GetNameParts(symbol, result);
            return result;
        });

    public static int CompareNameParts(
        IReadOnlyList<string> names1, IReadOnlyList<string> names2,
        bool placeSystemNamespaceFirst)
    {
        for (var i = 0; i < Math.Min(names1.Count, names2.Count); i++)
        {
            var name1 = names1[i];
            var name2 = names2[i];

            if (i == 0 && placeSystemNamespaceFirst)
            {
                var name1IsSystem = name1 == nameof(System);
                var name2IsSystem = name2 == nameof(System);

                if (name1IsSystem && !name2IsSystem)
                {
                    return -1;
                }
                else if (!name1IsSystem && name2IsSystem)
                {
                    return 1;
                }
            }

            var comp = name1.CompareTo(name2);
            if (comp != 0)
            {
                return comp;
            }
        }

        return names1.Count - names2.Count;
    }

    private static void GetNameParts(INamespaceOrTypeSymbol? namespaceOrTypeSymbol, List<string> result)
    {
        if (namespaceOrTypeSymbol == null || (namespaceOrTypeSymbol.IsNamespace && ((INamespaceSymbol)namespaceOrTypeSymbol).IsGlobalNamespace))
        {
            return;
        }

        GetNameParts(namespaceOrTypeSymbol.ContainingNamespace, result);
        result.Add(namespaceOrTypeSymbol.Name);
    }

    /// <summary>
    /// Lazily returns all nested types contained (recursively) within this namespace or type.
    /// In case of a type, it is included itself as the first result.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> GetAllTypes(
        this INamespaceOrTypeSymbol namespaceOrTypeSymbol,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<INamespaceOrTypeSymbol>.GetInstance(out var stack);
        stack.Push(namespaceOrTypeSymbol);

        while (stack.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (current is INamespaceSymbol currentNs)
            {
                stack.AddRange(currentNs.GetMembers());
            }
            else
            {
                var namedType = (INamedTypeSymbol)current;
                stack.AddRange(namedType.GetTypeMembers());
                yield return namedType;
            }
        }
    }
}
