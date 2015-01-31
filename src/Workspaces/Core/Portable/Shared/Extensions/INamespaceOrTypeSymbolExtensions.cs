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

        private static readonly SymbolDisplayFormat s_shortNameFormat = new SymbolDisplayFormat(
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable);

        public static readonly Comparison<INamespaceOrTypeSymbol> CompareNamespaceOrTypeSymbols = CompareTo;

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

        public static int CompareTo(this INamespaceOrTypeSymbol n1, INamespaceOrTypeSymbol n2)
        {
            var names1 = s_namespaceOrTypeToNameMap.GetValue(n1, GetNameParts);
            var names2 = s_namespaceOrTypeToNameMap.GetValue(n2, GetNameParts);

            for (var i = 0; i < Math.Min(names1.Count, names2.Count); i++)
            {
                var comp = names1[i].CompareTo(names2[i]);
                if (comp != 0)
                {
                    return comp;
                }
            }

            return names1.Count - names2.Count;
        }

        private static List<string> GetNameParts(INamespaceOrTypeSymbol namespaceSymbol)
        {
            var result = new List<string>();
            GetNameParts(namespaceSymbol, result);
            return result;
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
