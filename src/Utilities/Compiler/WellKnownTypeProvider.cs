// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Provides and caches well known types in a compilation.
    /// </summary>
    public class WellKnownTypeProvider
    {
        private static readonly BoundedCacheWithFactory<Compilation, WellKnownTypeProvider> s_providerCache =
            new BoundedCacheWithFactory<Compilation, WellKnownTypeProvider>();

        private WellKnownTypeProvider(Compilation compilation)
        {
            Compilation = compilation;
            _fullNameToTypeMap = new ConcurrentDictionary<string, INamedTypeSymbol?>(StringComparer.Ordinal);
            _referencedAssemblies = new Lazy<ImmutableHashSet<IAssemblySymbol>>(
                () =>
                {
                    return ImmutableHashSet.Create<IAssemblySymbol>(
                        Compilation.Assembly.Modules
                            .SelectMany(m => m.ReferencedAssemblySymbols)
                            .ToArray());
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public static WellKnownTypeProvider GetOrCreate(Compilation compilation)
        {
            return s_providerCache.GetOrCreateValue(compilation, CreateWellKnownTypeProvider);

            // Local functions
            static WellKnownTypeProvider CreateWellKnownTypeProvider(Compilation compilation)
                => new WellKnownTypeProvider(compilation);
        }

        public Compilation Compilation { get; }

        /// <summary>
        /// All the referenced assembly symbols.
        /// </summary>
        /// <remarks>
        /// Seems to be less memory intensive than:
        /// foreach (Compilation.Assembly.Modules)
        ///     foreach (Module.ReferencedAssemblySymbols)
        /// </remarks>
        private readonly Lazy<ImmutableHashSet<IAssemblySymbol>> _referencedAssemblies;

        /// <summary>
        /// Mapping of full name to <see cref="INamedTypeSymbol"/>.
        /// </summary>
        private readonly ConcurrentDictionary<string, INamedTypeSymbol?> _fullNameToTypeMap;

#if !NETSTANDARD1_3 // Assuming we're on .NET Standard 2.0 or later, cache the type names that are probably compile time constants.
        /// <summary>
        /// Static cache of full type names (with namespaces) to namespace name parts,
        /// so we can query <see cref="IAssemblySymbol.NamespaceNames"/>.
        /// </summary>
        /// <remarks>
        /// Example: "System.Collections.Generic.List`1" => [ "System", "Collections", "Generic" ]
        /// 
        /// https://github.com/dotnet/roslyn/blob/9e786147b8cb884af454db081bb747a5bd36a086/src/Compilers/CSharp/Portable/Symbols/AssemblySymbol.cs#L455
        /// suggests the TypeNames collection can be checked to avoid expensive operations. But realizing TypeNames seems to be
        /// as memory intensive as unnecessary calls GetTypeByMetadataName() in some cases. So we'll go with namespace names.
        /// </remarks>
        private static readonly ConcurrentDictionary<string, string[]> _fullTypeNameToNamespaceNames =
            new ConcurrentDictionary<string, string[]>(StringComparer.Ordinal);
#endif

        /// <summary>
        /// Attempts to get the type by the full type name.
        /// </summary>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <param name="namedTypeSymbol">Named type symbol, if any.</param>
        /// <returns>True if found in the compilation, false otherwise.</returns>
        public bool TryGetOrCreateTypeByMetadataName(
            string fullTypeName,
            [NotNullWhen(returnValue: true)] out INamedTypeSymbol? namedTypeSymbol)
        {
            namedTypeSymbol = _fullNameToTypeMap.GetOrAdd(
                fullTypeName,
                fullyQualifiedMetadataName =>
                {
                    // Caching null results is intended.

                    // sharwell says: Suppose you reference assembly A with public API X.Y, and you reference assembly B with
                    // internal API X.Y. Even though you can use X.Y from assembly A, compilation.GetTypeByMetadataName will 
                    // fail outright because it finds two types with the same name.

                    INamedTypeSymbol? type = null;

                    string[]? namespaceNames = null;
#if NETSTANDARD1_3 // Probably in 2.9.x branch; just don't cache.
                    namespaceNames = GetNamespaceNamesFromFullTypeName(fullTypeName);
#else // Assuming we're on .NET Standard 2.0 or later, cache the type names that are probably compile time constants.
                    if (string.IsInterned(fullTypeName) != null)
                    {
                        namespaceNames = _fullTypeNameToNamespaceNames.GetOrAdd(
                            fullTypeName,
                            GetNamespaceNamesFromFullTypeName);
                    }
                    else
                    {
                        namespaceNames = GetNamespaceNamesFromFullTypeName(fullTypeName);
                    }
#endif

                    if (IsSubsetOfCollection(namespaceNames, Compilation.Assembly.NamespaceNames))
                    {
                        type = Compilation.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName);
                    }

                    if (type is null)
                    {
                        RoslynDebug.Assert(namespaceNames != null);

                        foreach (IAssemblySymbol? referencedAssembly in _referencedAssemblies.Value)
                        {
                            if (!IsSubsetOfCollection(namespaceNames, referencedAssembly.NamespaceNames!))
                            {
                                continue;
                            }

                            var currentType = referencedAssembly.GetTypeByMetadataName(fullyQualifiedMetadataName);
                            if (currentType is null)
                            {
                                continue;
                            }

                            switch (currentType.GetResultantVisibility())
                            {
                                case SymbolVisibility.Public:
                                case SymbolVisibility.Internal when referencedAssembly.GivesAccessTo(Compilation.Assembly):
                                    break;

                                default:
                                    continue;
                            }

                            if (type is object)
                            {
                                // Multiple visible types with the same metadata name are present.
                                return null;
                            }

                            type = currentType;
                        }
                    }

                    return type;
                });

            return namedTypeSymbol != null;
        }

        /// <summary>
        /// Gets a type by its full type name.
        /// </summary>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <returns>The <see cref="INamedTypeSymbol"/> if found, null otherwise.</returns>
        public INamedTypeSymbol? GetOrCreateTypeByMetadataName(string fullTypeName)
        {
            TryGetOrCreateTypeByMetadataName(fullTypeName, out INamedTypeSymbol? namedTypeSymbol);
            return namedTypeSymbol;
        }

        /// <summary>
        /// Determines if <paramref name="typeSymbol"/> is a <see cref="System.Threading.Tasks.Task{TResult}"/> with its type
        /// argument satisfying <paramref name="typeArgumentPredicate"/>.
        /// </summary>
        /// <param name="typeSymbol">Type potentially representing a <see cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="typeArgumentPredicate">Predicate to check the <paramref name="typeSymbol"/>'s type argument.</param>
        /// <returns>True if <paramref name="typeSymbol"/> is a <see cref="System.Threading.Tasks.Task{TResult}"/> with its
        /// type argument satisfying <paramref name="typeArgumentPredicate"/>, false otherwise.</returns>
        internal bool IsTaskOfType([NotNullWhen(returnValue: true)] ITypeSymbol? typeSymbol, Func<ITypeSymbol, bool> typeArgumentPredicate)
        {
            return typeSymbol != null
                && typeSymbol.OriginalDefinition != null
                && typeSymbol.OriginalDefinition.Equals(GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksGenericTask))
                && typeSymbol is INamedTypeSymbol namedTypeSymbol
                && namedTypeSymbol.TypeArguments.Length == 1
                && typeArgumentPredicate(namedTypeSymbol.TypeArguments[0]);
        }

        private static string[] GetNamespaceNamesFromFullTypeName(string fullTypeName)
        {
            int dotCount = 0;
            int lastDotIndex = -1;
            for (int i = 0; i < fullTypeName.Length; i++)
            {
                switch (fullTypeName[i])
                {
                    case '.':
                        lastDotIndex = i;
                        dotCount++;
                        break;

                    // Dunno if Compilation.GetTypeByMetadataName() or IAssemblySymbol.GetTypeByMetadataName() will ever 
                    // support something like "System.Collections.Generic.List<System.String>", but if they do, we'll be ready.
                    case '`':
                    case '+':
                    case '<':
                    case '>':
                    case '[':
                    case ']':
                    case '*':
                    case ',':
                        goto ExitLoop;
                }
            }

            ExitLoop:
            if (dotCount == 0)
            {
                return Array.Empty<string>();
            }

            string[] namespaceNames = new string[dotCount];
            int namespaceIndex = 0;
            int prevStartIndex = 0;
            for (int i = 0; i <= lastDotIndex; i++)
            {
                if (fullTypeName[i] == '.')
                {
                    namespaceNames[namespaceIndex++] = fullTypeName[prevStartIndex..i];
                    prevStartIndex = i + 1;
                }
            }

            return namespaceNames;
        }

        private static bool IsSubsetOfCollection<T>(T[] set1, ICollection<T> set2)
        {
            if (set1.Length > set2.Count)
            {
                return false;
            }

            for (int i = 0; i < set1.Length; i++)
            {
                if (!set2.Contains(set1[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
