// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        /// Mapping of full name to <see cref="INamedTypeSymbol"/>.
        /// </summary>
        private readonly ConcurrentDictionary<string, INamedTypeSymbol?> _fullNameToTypeMap;

        /// <summary>
        /// Global cache of full type names (with namespaces) to namespace name parts and simple type name (without namespace),
        /// so we can query <see cref="IAssemblySymbol.NamespaceNames"/> and <see cref="IAssemblySymbol.TypeNames"/>.
        /// </summary>
        /// <remarks>
        /// Example: "System.Collections.Generic.List`1" => ( [ "System", "Collections", "Generic" ], "List" )
        /// 
        /// https://github.com/dotnet/roslyn/blob/9e786147b8cb884af454db081bb747a5bd36a086/src/Compilers/CSharp/Portable/Symbols/AssemblySymbol.cs#L455
        /// suggests the TypeNames collection can be checked to avoid expensive operations.
        /// </remarks>
        private static readonly ConcurrentDictionary<string, (ImmutableHashSet<string> NamespaceNames, string SimpleTypeName)> _fullTypeNameToSimpleInfo =
            new ConcurrentDictionary<string, (ImmutableHashSet<string> NamespaceNames, string SimpleTypeName)>(StringComparer.Ordinal);

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
                    // Caching null results in our cache is intended.

#pragma warning disable RS0030 // Do not used banned APIs
                    // Use of Compilation.GetTypeByMetadataName is allowed here (this is our wrapper for it which
                    // includes fallback handling for cases where GetTypeByMetadataName returns null).
                    var type = Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
#pragma warning restore RS0030 // Do not used banned APIs

                    // sharwell says: Suppose you reference assembly A with public API X.Y, and you reference assembly B with
                    // internal API X.Y. Even though you can use X.Y from assembly A, compilation.GetTypeByMetadataName will 
                    // fail outright because it finds two types with the same name.

                    ImmutableHashSet<string>? namespaceNames = null;
                    string? typeName = null;
                    if (type is null)
                    {
#if NETSTANDARD1_3 // Probably in 2.9.x branch; just cache everything.
                        (namespaceNames, typeName) = _fullTypeNameToSimpleInfo.GetOrAdd(
                            fullTypeName,
                            GetSimpleNameInfoFromFullTypeName);
#else // Assuming we're on .NET Standard 2.0 or later, cache type names which are probably compile time constants.
                        if (string.IsInterned(fullTypeName) != null)
                        {
                            (namespaceNames, typeName) = _fullTypeNameToSimpleInfo.GetOrAdd(
                                fullTypeName,
                                GetSimpleNameInfoFromFullTypeName);
                        }
                        else
                        {
                            (namespaceNames, typeName) = GetSimpleNameInfoFromFullTypeName(fullTypeName);
                        }
#endif

                        if (IsSubsetOfCollection(namespaceNames, Compilation.Assembly.NamespaceNames)
                            && Compilation.Assembly.TypeNames.Contains(typeName))
                        {
                            type = Compilation.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName);
                        }
                    }

                    if (type is null)
                    {
                        RoslynDebug.Assert(namespaceNames != null);
                        RoslynDebug.Assert(typeName != null);

                        foreach (var module in Compilation.Assembly.Modules)
                        {
                            foreach (var referencedAssembly in module.ReferencedAssemblySymbols)
                            {
                                if (!IsSubsetOfCollection(namespaceNames, referencedAssembly.NamespaceNames!)
                                    || !referencedAssembly.TypeNames.Contains(typeName))
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

        private static (ImmutableHashSet<string> NamespaceNames, string SimpleTypeName) GetSimpleNameInfoFromFullTypeName(string fullTypeName)
        {
            int plusIndex = fullTypeName.LastIndexOf('+');   // For nested types.
            int dotIndex = fullTypeName.LastIndexOf('.');
            int backTickIndex = fullTypeName.LastIndexOf('`');

            int typeStartIndex = Math.Max(dotIndex, plusIndex) + 1;   // Exclude the '+' or '.'; LastIndexOf() returns -1 if not found.
            int typeEndIndex = backTickIndex >= 0 && backTickIndex > typeStartIndex ? backTickIndex : fullTypeName.Length;

            ImmutableHashSet<string> namespaceNames;
            if (dotIndex >= 0)
            {
                namespaceNames = ImmutableHashSet.Create(
                    StringComparer.Ordinal,
                    fullTypeName
                        .Substring(0, dotIndex >= 0 ? dotIndex : fullTypeName.Length)
                        .Split('.'));
            }
            else
            {
                namespaceNames = ImmutableHashSet<string>.Empty;
            }

            return (namespaceNames, fullTypeName[typeStartIndex..typeEndIndex]);
        }

        // This would live in ImmutableHashSetsExtensions.cs, but Microsoft.CodeAnalysis.PublicApiAnalyzers.csproj doesn't get
        // it.
        private static bool IsSubsetOfCollection<T>(ImmutableHashSet<T> set1, ICollection<T> set2)
        {
            if (set1.Count > set2.Count)
            {
                return false;
            }

            foreach (T item in set1)
            {
                if (!set2.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
