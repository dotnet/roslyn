// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class IAssemblySymbolExtensions
    {
        private const string AttributeSuffix = "Attribute";

        public static bool ContainsNamespaceName(
            this List<IAssemblySymbol> assemblies,
            string namespaceName)
        {
            // PERF: Expansion of "assemblies.Any(a => a.NamespaceNames.Contains(namespaceName))"
            // to avoid allocating a lambda.
            foreach (var a in assemblies)
            {
                if (a.NamespaceNames.Contains(namespaceName))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsTypeName(this List<IAssemblySymbol> assemblies, string typeName, bool tryWithAttributeSuffix = false)
        {
            if (!tryWithAttributeSuffix)
            {
                // PERF: Expansion of "assemblies.Any(a => a.TypeNames.Contains(typeName))"
                // to avoid allocating a lambda.
                foreach (var a in assemblies)
                {
                    if (a.TypeNames.Contains(typeName))
                    {
                        return true;
                    }
                }
            }
            else
            {
                var attributeName = typeName + AttributeSuffix;
                foreach (var a in assemblies)
                {
                    var typeNames = a.TypeNames;
                    if (typeNames.Contains(typeName) || typeNames.Contains(attributeName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsSameAssemblyOrHasFriendAccessTo(this IAssemblySymbol assembly, IAssemblySymbol toAssembly)
        {
            return
                Equals(assembly, toAssembly) ||
                (assembly.IsInteractive && toAssembly.IsInteractive) ||
                toAssembly.GivesAccessTo(assembly);
        }
    }
}
