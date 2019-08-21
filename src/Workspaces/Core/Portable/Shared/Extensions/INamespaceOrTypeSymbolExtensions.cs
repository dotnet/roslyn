// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class INamespaceOrTypeSymbolExtensions
    {
        private static readonly ConditionalWeakTable<INamespaceOrTypeSymbol, List<string>> s_namespaceOrTypeToNameMap =
            new ConditionalWeakTable<INamespaceOrTypeSymbol, List<string>>();
        public static readonly ConditionalWeakTable<INamespaceOrTypeSymbol, List<string>>.CreateValueCallback s_getNamePartsCallBack =
            namespaceSymbol =>
            {
                var result = new List<string>();
                GetNameParts(namespaceSymbol, result);
                return result;
            };

        private static readonly SymbolDisplayFormat s_shortNameFormat = new SymbolDisplayFormat(
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable);

        public static string GetShortName(this INamespaceOrTypeSymbol symbol)
        {
            return symbol.ToDisplayString(s_shortNameFormat);
        }

        public static IEnumerable<IPropertySymbol> GetIndexers(this INamespaceOrTypeSymbol? symbol)
        {
            return symbol == null
                ? SpecializedCollections.EmptyEnumerable<IPropertySymbol>()
                : symbol.GetMembers(WellKnownMemberNames.Indexer).OfType<IPropertySymbol>().Where(p => p.IsIndexer);
        }

        public static IReadOnlyList<string> GetNameParts(this INamespaceOrTypeSymbol symbol)
            => s_namespaceOrTypeToNameMap.GetValue(symbol, s_getNamePartsCallBack);

        public static int CompareNameParts(
            IReadOnlyList<string> names1, IReadOnlyList<string> names2,
            bool placeSystemNamespaceFirst)
            => IComparableHelper.CompareTo(names1, names2, names => GetComparisonComponents(names, placeSystemNamespaceFirst));

        public static IEnumerable<IComparable> GetComparisonComponents(IReadOnlyList<string> names, bool placeSystemNamespaceFirst)
        {
            bool isFirstItem = true;
            foreach (var name in names)
            {
                // For each item iteration, compare if one of list is over. The shorter wins in the case (-1 in Compare).
                yield return true;

                if (isFirstItem && placeSystemNamespaceFirst)
                {
                    isFirstItem = false;

                    // If one has System in the beginning and another does not,
                    // the one with System should win (-1 in Compare).
                    yield return name != nameof(System);
                }

                yield return name;
            }

            // Items are over in the current list. This list should win (Compare == -1) if another one still produces items.
            yield return false;
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
            var stack = new Stack<INamespaceOrTypeSymbol>();
            stack.Push(namespaceOrTypeSymbol);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stack.Pop();
                if (current is INamespaceSymbol currentNs)
                {
                    stack.Push(currentNs.GetMembers());
                }
                else
                {
                    var namedType = (INamedTypeSymbol)current;
                    stack.Push(namedType.GetTypeMembers());
                    yield return namedType;
                }
            }
        }
    }
}
