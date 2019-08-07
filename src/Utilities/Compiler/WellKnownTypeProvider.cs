// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Analyzer.Utilities;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Provides and caches well known types in a compilation for <see cref="DataFlowAnalysis"/>.
    /// </summary>
    public class WellKnownTypeProvider
    {
        private static readonly ConditionalWeakTable<Compilation, WellKnownTypeProvider> s_providerCache =
            new ConditionalWeakTable<Compilation, WellKnownTypeProvider>();
        private static readonly ConditionalWeakTable<Compilation, WellKnownTypeProvider>.CreateValueCallback s_ProviderCacheCallback =
            new ConditionalWeakTable<Compilation, WellKnownTypeProvider>.CreateValueCallback(compilation => new WellKnownTypeProvider(compilation));

        private WellKnownTypeProvider(Compilation compilation)
        {
            Compilation = compilation;
            _fullNameToTypeMap = new ConcurrentDictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);

            Exception = GetTypeByMetadataName(WellKnownTypeNames.SystemExceptionFullName);
            Contract = GetTypeByMetadataName(WellKnownTypeNames.SystemDiagnosticContractsContract);
            IDisposable = GetTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
            Monitor = GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingMonitor);
            Task = GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask);
            GenericTask = GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksGenericTask);
            CollectionTypes = GetWellKnownCollectionTypes();
            SerializationInfo = GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationSerializationInfo);
            GenericIEquatable = GetTypeByMetadataName(WellKnownTypeNames.SystemIEquatable1);
        }

        public static WellKnownTypeProvider GetOrCreate(Compilation compilation) => s_providerCache.GetValue(compilation, s_ProviderCacheCallback);

        public Compilation Compilation { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Exception"/>
        /// </summary>
        public INamedTypeSymbol Exception { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for 'System.Diagnostics.Contracts.Contract' type. />
        /// </summary>
        public INamedTypeSymbol Contract { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IDisposable"/>
        /// </summary>
        public INamedTypeSymbol IDisposable { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Tasks.Task"/>
        /// </summary>
        public INamedTypeSymbol Task { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Tasks.Task{TResult}"/>
        /// </summary>
        public INamedTypeSymbol GenericTask { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Monitor"/>
        /// </summary>
        public INamedTypeSymbol Monitor { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for 'System.Runtime.Serialization.SerializationInfo' type />
        /// </summary>
        public INamedTypeSymbol SerializationInfo { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IEquatable{T}"/>
        /// </summary>
        public INamedTypeSymbol GenericIEquatable { get; }

        /// <summary>
        /// Mapping of full name to <see cref="INamedTypeSymbol"/>.
        /// </summary>
        private readonly ConcurrentDictionary<string, INamedTypeSymbol> _fullNameToTypeMap;

        /// <summary>
        /// Attempts to get the type by the full type name.
        /// </summary>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <param name="namedTypeSymbol">Named type symbol, if any.</param>
        /// <returns>True if found in the compilation, false otherwise.</returns>
        public bool TryGetTypeByMetadataName(string fullTypeName, out INamedTypeSymbol namedTypeSymbol)
        {
            namedTypeSymbol = _fullNameToTypeMap.GetOrAdd(
                fullTypeName,
                (string s) => Compilation.GetTypeByMetadataName(s));    // Caching null results in our cache is intended.
            return namedTypeSymbol != null;
        }

        private INamedTypeSymbol GetTypeByMetadataName(string fullTypeName)
        {
            TryGetTypeByMetadataName(fullTypeName, out INamedTypeSymbol namedTypeSymbol);
            return namedTypeSymbol;
        }

        /// <summary>
        /// Set containing following named types, if not null:
        /// 1. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.ICollection"/>
        /// 2. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.ICollection{T}"/>
        /// 3. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>
        /// </summary>
        public ImmutableHashSet<INamedTypeSymbol> CollectionTypes { get; }

        private ImmutableHashSet<INamedTypeSymbol> GetWellKnownCollectionTypes()
        {
            var builder = PooledHashSet<INamedTypeSymbol>.GetInstance();
            var iCollection = GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsICollection);
            if (iCollection != null)
            {
                builder.Add(iCollection);
            }

            var genericICollection = GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericICollection1);
            if (genericICollection != null)
            {
                builder.Add(genericICollection);
            }

            var genericIReadOnlyCollection = GetTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIReadOnlyCollection1);
            if (genericIReadOnlyCollection != null)
            {
                builder.Add(genericIReadOnlyCollection);
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Determines if <paramref name="typeSymbol"/> is a <see cref="System.Threading.Tasks.Task{TResult}"/> with its type
        /// argument satisfying <paramref name="typeArgumentPredicate"/>.
        /// </summary>
        /// <param name="typeSymbol">Type potentially representing a <see cref="System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="typeArgumentPredicate">Predicate to check the <paramref name="typeSymbol"/>'s type argument.</param>
        /// <returns>True if <paramref name="typeSymbol"/> is a <see cref="System.Threading.Tasks.Task{TResult}"/> with its
        /// type argument satisfying <paramref name="typeArgumentPredicate"/>, false otherwise.</returns>
        internal bool IsTaskOfType(ITypeSymbol typeSymbol, Func<ITypeSymbol, bool> typeArgumentPredicate)
        {
            return typeSymbol != null
                && typeSymbol.OriginalDefinition != null
                && typeSymbol.OriginalDefinition.Equals(GenericTask)
                && typeSymbol is INamedTypeSymbol namedTypeSymbol
                && namedTypeSymbol.TypeArguments.Length == 1
                && typeArgumentPredicate(namedTypeSymbol.TypeArguments[0]);
        }
    }
}
