// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Exception = Analyzer.Utilities.WellKnownTypes.Exception(compilation);
            Contract = Analyzer.Utilities.WellKnownTypes.SystemDiagnosticContractsContract(compilation);
            IDisposable = Analyzer.Utilities.WellKnownTypes.IDisposable(compilation);
            Monitor = Analyzer.Utilities.WellKnownTypes.Monitor(compilation);
            Task = Analyzer.Utilities.WellKnownTypes.Task(compilation);
            CollectionTypes = GetWellKnownCollectionTypes(compilation);
            SerializationInfo = Analyzer.Utilities.WellKnownTypes.SerializationInfo(compilation);
            GenericIEquatable = Analyzer.Utilities.WellKnownTypes.GenericIEquatable(compilation);
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
        /// Set containing following named types, if not null:
        /// 1. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.ICollection"/>
        /// 2. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.ICollection{T}"/>
        /// 3. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>
        /// </summary>
        public ImmutableHashSet<INamedTypeSymbol> CollectionTypes { get; }

        private static ImmutableHashSet<INamedTypeSymbol> GetWellKnownCollectionTypes(Compilation compilation)
        {
            var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
            var iCollection = Analyzer.Utilities.WellKnownTypes.ICollection(compilation);
            if (iCollection != null)
            {
                builder.Add(iCollection);
            }

            var genericICollection = Analyzer.Utilities.WellKnownTypes.GenericICollection(compilation);
            if (genericICollection != null)
            {
                builder.Add(genericICollection);
            }

            var genericIReadOnlyCollection = Analyzer.Utilities.WellKnownTypes.GenericIReadOnlyCollection(compilation);
            if (genericIReadOnlyCollection != null)
            {
                builder.Add(genericIReadOnlyCollection);
            }

            return builder.ToImmutable();
        }
    }
}
