// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Provides and caches well known types in a compilation for <see cref="DataFlowAnalysis"/>.
    /// </summary>
    internal class WellKnownTypeProvider
    {
        private static readonly ConditionalWeakTable<Compilation, WellKnownTypeProvider> s_providerCache =
            new ConditionalWeakTable<Compilation, WellKnownTypeProvider>();
        private static readonly ConditionalWeakTable<Compilation, WellKnownTypeProvider>.CreateValueCallback s_ProviderCacheCallback =
            new ConditionalWeakTable<Compilation, WellKnownTypeProvider>.CreateValueCallback(compilation => new WellKnownTypeProvider(compilation));

        private WellKnownTypeProvider(Compilation compilation)
        {
            Compilation = compilation;
            FullNameToType = new ConcurrentDictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);


            Exception = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemException);
            Contract = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemDiagnosticContractsContract);
            IDisposable = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemIDisposable);
            Monitor = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemThreadingMonitor);
            Task = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemThreadingTasksTask);
            CollectionTypes = GetWellKnownCollectionTypes();
            SerializationInfo = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemRuntimeSerializationSerializationInfo);
            GenericIEquatable = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemIEquatable1);
        }

        public static WellKnownTypeProvider GetOrCreate(Compilation compilation) => s_providerCache.GetValue(compilation, s_ProviderCacheCallback);

        public Compilation Compilation { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Exception"/>
        /// </summary>
        public INamedTypeSymbol Exception { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Diagnostics.Contracts.Contract"/>
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
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Monitor"/>
        /// </summary>
        public INamedTypeSymbol Monitor { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Runtime.Serialization.SerializationInfo"/>
        /// </summary>
        public INamedTypeSymbol SerializationInfo { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IEquatable{T}"/>
        /// </summary>
        public INamedTypeSymbol GenericIEquatable { get; }

        /// <summary>
        /// Mapping of full name to <see cref="INamedTypeSymbol"/>.
        /// </summary>
        private ConcurrentDictionary<string, INamedTypeSymbol> FullNameToType { get; }

        /// <summary>
        /// Attempts to get the type by the full type name.
        /// </summary>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <param name="namedTypeSymbol">Named type symbol, if any.</param>
        /// <returns>True if found in the compilation, false otherwise.</returns>
        public bool TryGetTypeByMetadataName(string fullTypeName, out INamedTypeSymbol namedTypeSymbol)
        {
            if (!FullNameToType.TryGetValue(fullTypeName, out namedTypeSymbol))
            {
                namedTypeSymbol = Compilation.GetTypeByMetadataName(fullTypeName);

                // Even if the compilation gives back null, still cache the null to avoid future lookups.
                FullNameToType.TryAdd(fullTypeName, namedTypeSymbol);
            }

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
            var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
            var iCollection = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemCollectionsICollection);
            if (iCollection != null)
            {
                builder.Add(iCollection);
            }

            var genericICollection = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemCollectionsGenericICollection);
            if (genericICollection != null)
            {
                builder.Add(genericICollection);
            }

            var genericIReadOnlyCollection = GetTypeByMetadataName(Analyzer.Utilities.WellKnownTypes.SystemCollectionsGenericIReadOnlyCollection);
            if (genericIReadOnlyCollection != null)
            {
                builder.Add(genericIReadOnlyCollection);
            }

            return builder.ToImmutable();
        }
    }
}
