// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public static IEnumerable<IPropertySymbol> GetIndexers(this INamespaceOrTypeSymbol symbol)
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

        private static void GetNameParts(INamespaceOrTypeSymbol namespaceOrTypeSymbol, List<string> result)
        {
            if (namespaceOrTypeSymbol == null || (namespaceOrTypeSymbol.IsNamespace && ((INamespaceSymbol)namespaceOrTypeSymbol).IsGlobalNamespace))
            {
                return;
            }

            GetNameParts(namespaceOrTypeSymbol.ContainingNamespace, result);
            result.Add(namespaceOrTypeSymbol.Name);
        }
    }
}
