// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

        public static string ToTestDisplayString(this ISymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.TestFormat);
        }
    }
}
