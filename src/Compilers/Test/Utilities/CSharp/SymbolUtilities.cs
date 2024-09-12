// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class NameAndArityComparer
        : IComparer<NamedTypeSymbol>
    {
        public int Compare(NamedTypeSymbol x, NamedTypeSymbol y) // Implements IComparer<NamedTypeSymbol).Compare
        {
            int result = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);

            if (result != 0)
            {
                return result;
            }

            return x.Arity - y.Arity;
        }
    }

    internal static class SymbolUtilities
    {
        public static NamespaceSymbol ChildNamespace(this NamespaceSymbol ns, string name)
        {
            return ns.GetMembers()
                   .Where(n => n.Name.Equals(name))
                   .Cast<NamespaceSymbol>()
                   .Single();
        }

        public static NamedTypeSymbol ChildType(this NamespaceSymbol ns, string name)
        {
            return ns.GetMembers()
                   .OfType<NamedTypeSymbol>()
                   .Single(n => n.Name.Equals(name));
        }

        public static NamedTypeSymbol ChildType(this NamespaceSymbol ns, string name, int arity)
        {
            return ns.GetMembers()
                   .OfType<NamedTypeSymbol>()
                   .Single(n => n.Name.Equals(name) && n.Arity == arity);
        }

        public static Symbol ChildSymbol(this NamespaceOrTypeSymbol parent, string name)
        {
            return parent.GetMembers(name).First();
        }

        public static T GetIndexer<T>(this NamespaceOrTypeSymbol type, string name) where T : PropertySymbol
        {
            T member = type.GetMembers(WellKnownMemberNames.Indexer).Where(i => i.MetadataName == name).Single() as T;
            Assert.NotNull(member);
            return member;
        }

        public static string ListToSortedString(this List<string> list)
        {
            string text = "";
            list.Sort();

            foreach (var element in list)
            {
                text = text + '\n' + element.ToString();
            }
            return text;
        }

        public static string ListToSortedString<TSymbol>(this List<TSymbol> listOfSymbols) where TSymbol : ISymbol
        {
            string text = "";
            List<string> listOfSymbolString = listOfSymbols.Select(e => e.ToTestDisplayString()).ToList();
            listOfSymbolString.Sort();

            foreach (var symbolString in listOfSymbolString)
            {
                text = text + '\n' + symbolString;
            }
            return text;
        }

        private static SymbolDisplayFormat GetDisplayFormat(bool includeNonNullable)
        {
            var format = SymbolDisplayFormat.TestFormat;
            if (includeNonNullable)
            {
                format = format.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier)
                    .WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.None);
            }

            return format;
        }

        public static string ToTestDisplayString(this TypeWithAnnotations type, bool includeNonNullable = false)
        {
            SymbolDisplayFormat format = GetDisplayFormat(includeNonNullable);
            return type.ToDisplayString(format);
        }

        public static string[] ToTestDisplayStrings(this IEnumerable<TypeWithAnnotations> types)
        {
            return types.Select(t => t.ToTestDisplayString()).ToArray();
        }

        public static string[] ToTestDisplayStrings(this IEnumerable<ISymbol> symbols)
        {
            return symbols.Select(s => s.ToTestDisplayString()).ToArray();
        }

        public static string[] ToTestDisplayStrings(this IEnumerable<Symbol> symbols, SymbolDisplayFormat format = null)
        {
            format ??= SymbolDisplayFormat.TestFormat;
            return symbols.Select(s => s.ToDisplayString(format)).ToArray();
        }

        public static string ToTestDisplayString(this ISymbol symbol, bool includeNonNullable)
        {
            SymbolDisplayFormat format = GetDisplayFormat(includeNonNullable);
            return symbol.ToDisplayString(format);
        }

        public static string ToTestDisplayString(this Symbol symbol, bool includeNonNullable)
        {
            SymbolDisplayFormat format = GetDisplayFormat(includeNonNullable);
            return symbol.ToDisplayString(format);
        }
    }
}
