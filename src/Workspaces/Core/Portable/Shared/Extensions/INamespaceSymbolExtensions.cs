// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class INamespaceSymbolExtensions
    {
        private static readonly ConditionalWeakTable<INamespaceSymbol, List<string>> s_namespaceToNameMap =
            new ConditionalWeakTable<INamespaceSymbol, List<string>>();
        private static readonly ConditionalWeakTable<INamespaceSymbol, List<string>>.CreateValueCallback s_getNameParts = GetNameParts;

        public static readonly Comparison<INamespaceSymbol> CompareNamespaces = CompareTo;
        public static readonly IEqualityComparer<INamespaceSymbol> EqualityComparer = new Comparer();

        private static List<string> GetNameParts(INamespaceSymbol? namespaceSymbol)
        {
            var result = new List<string>();
            GetNameParts(namespaceSymbol, result);
            return result;
        }

        private static void GetNameParts(INamespaceSymbol? namespaceSymbol, List<string> result)
        {
            if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
            {
                return;
            }

            GetNameParts(namespaceSymbol.ContainingNamespace, result);
            result.Add(namespaceSymbol.Name);
        }

        public static int CompareTo(this INamespaceSymbol n1, INamespaceSymbol n2)
        {
            var names1 = s_namespaceToNameMap.GetValue(n1, GetNameParts);
            var names2 = s_namespaceToNameMap.GetValue(n2, GetNameParts);

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

        public static IEnumerable<INamespaceOrTypeSymbol> GetAllNamespacesAndTypes(
            this INamespaceSymbol namespaceSymbol,
            CancellationToken cancellationToken)
        {
            var stack = new Stack<INamespaceOrTypeSymbol>();
            stack.Push(namespaceSymbol);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stack.Pop();
                if (current is INamespaceSymbol childNamespace)
                {
                    stack.Push(childNamespace.GetMembers().AsEnumerable());
                    yield return childNamespace;
                }
                else
                {
                    var child = (INamedTypeSymbol)current;
                    stack.Push(child.GetTypeMembers());
                    yield return child;
                }
            }
        }

        public static IEnumerable<INamespaceSymbol> GetAllNamespaces(
            this INamespaceSymbol namespaceSymbol,
            CancellationToken cancellationToken)
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(namespaceSymbol);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stack.Pop();
                if (current is INamespaceSymbol childNamespace)
                {
                    stack.Push(childNamespace.GetNamespaceMembers());
                    yield return childNamespace;
                }
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetAllTypes(
            this IEnumerable<INamespaceSymbol> namespaceSymbols,
            CancellationToken cancellationToken)
        {
            return namespaceSymbols.SelectMany(n => n.GetAllTypes(cancellationToken));
        }

        /// <summary>
        /// Searches the namespace for namespaces with the provided name.
        /// </summary>
        public static IEnumerable<INamespaceSymbol> FindNamespaces(
            this INamespaceSymbol namespaceSymbol,
            string namespaceName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stack = new Stack<INamespaceSymbol>();
            stack.Push(namespaceSymbol);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stack.Pop();

                var matchingChildren = current.GetMembers(namespaceName).OfType<INamespaceSymbol>();
                foreach (var child in matchingChildren)
                {
                    yield return child;
                }

                stack.Push(current.GetNamespaceMembers());
            }
        }

        public static bool ContainsAccessibleTypesOrNamespaces(
            this INamespaceSymbol namespaceSymbol,
            IAssemblySymbol assembly)
        {
            using (var namespaceQueue = SharedPools.Default<Queue<INamespaceOrTypeSymbol>>().GetPooledObject())
            {
                return ContainsAccessibleTypesOrNamespacesWorker(namespaceSymbol, assembly, namespaceQueue.Object);
            }
        }

        public static INamespaceSymbol? GetQualifiedNamespace(
            this INamespaceSymbol globalNamespace,
            string namespaceName)
        {
            INamespaceSymbol? namespaceSymbol = globalNamespace;
            foreach (var name in namespaceName.Split('.'))
            {
                var members = namespaceSymbol.GetMembers(name);
                namespaceSymbol = members.Count() == 1
                        ? members.First() as INamespaceSymbol
                        : null;

                if (namespaceSymbol is null)
                {
                    break;
                }
            }
            return namespaceSymbol;
        }

        private static bool ContainsAccessibleTypesOrNamespacesWorker(
            this INamespaceSymbol namespaceSymbol,
            IAssemblySymbol assembly,
            Queue<INamespaceOrTypeSymbol> namespaceQueue)
        {
            // Note: we only store INamespaceSymbols in here, even though we type it as 
            // INamespaceOrTypeSymbol.  This is because when we call GetMembers below we
            // want it to return an ImmutableArray so we don't incur any costs to iterate
            // over it.

            foreach (var constituent in namespaceSymbol.ConstituentNamespaces)
            {
                // Assume that any namespace in our own assembly is accessible to us.  This saves a
                // lot of cpu time checking namespaces.
                if (Equals(constituent.ContainingAssembly, assembly))
                {
                    return true;
                }

                namespaceQueue.Enqueue(constituent);
            }

            while (namespaceQueue.Count > 0)
            {
                var ns = namespaceQueue.Dequeue();

                // Upcast so we call the 'GetMembers' method that returns an ImmutableArray.
                var members = ns.GetMembers();

                foreach (var namespaceOrType in members)
                {
                    if (namespaceOrType.Kind == SymbolKind.NamedType)
                    {
                        if (namespaceOrType.IsAccessibleWithin(assembly))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        namespaceQueue.Enqueue((INamespaceSymbol)namespaceOrType);
                    }
                }
            }

            return false;
        }
    }
}
