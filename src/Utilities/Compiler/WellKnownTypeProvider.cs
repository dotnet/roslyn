// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
        /// Attempts to get the type by the full type name.
        /// </summary>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <param name="namedTypeSymbol">Named type symbol, if any.</param>
        /// <returns>True if found in the compilation, false otherwise.</returns>
        public bool TryGetOrCreateTypeByMetadataName(string fullTypeName, [NotNullWhen(returnValue: true)] out INamedTypeSymbol? namedTypeSymbol)
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

                    type ??= Compilation.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName);
                    if (type is null)
                    {
                        foreach (var module in Compilation.Assembly.Modules)
                        {
                            foreach (var referencedAssembly in module.ReferencedAssemblySymbols)
                            {
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
    }
}
