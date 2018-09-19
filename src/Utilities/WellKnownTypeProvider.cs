// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            TypeToFullName = new Dictionary<ISymbol, string>();

            Exception = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemException);
            Contract = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDiagnosticContractsContract);
            IDisposable = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemIDisposable);
            Monitor = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemThreadingMonitor);
            Task = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemThreadingTasksTask);
            CollectionTypes = GetWellKnownCollectionTypes(compilation);
            SerializationInfo = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemRuntimeSerializationSerializationInfo);
            GenericIEquatable = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemIEquatable1);
            HttpRequest = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebHttpRequest);
            IDbCommand = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDataIDbCommand);
            WebControlsSqlDataSource = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemWebUIWebControlsSqlDataSource);
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
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Web.HttpRequest"/>
        /// </summary>
        public INamedTypeSymbol HttpRequest { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Data.IDbCommand"/>
        /// </summary>
        public INamedTypeSymbol IDbCommand { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Web.UI.WebControls.SqlDataSource"/>
        /// </summary>
        public INamedTypeSymbol WebControlsSqlDataSource { get; }

        /// <summary>
        /// Set containing following named types, if not null:
        /// 1. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.ICollection"/>
        /// 2. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.ICollection{T}"/>
        /// 3. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>
        /// </summary>
        public ImmutableHashSet<INamedTypeSymbol> CollectionTypes { get; }

        /// <summary>
        /// Mapping of ISymbol to full name (e.g. "System.Exception").
        /// </summary>
        private Dictionary<ISymbol, string> TypeToFullName { get; }

        /// <summary>
        /// Attempts to get the full type name (namespace + type) of the specifed symbol.
        /// </summary>
        /// <param name="symbol">Symbol.</param>
        /// <param name="fullTypeName">Namespace + type name.</param>
        /// <returns>True if found, false otherwise.</returns>
        /// <remarks>This only works for types that this <see cref="WellKnownTypeProvider"/> knows about.</remarks>
        public bool TryGetFullTypeName(ISymbol symbol, out string fullTypeName)
        {
            return TypeToFullName.TryGetValue(symbol, out fullTypeName);
        }

        /// <summary>
        /// Gets the INamedTypeSymbol from the compilation and caches a mapping from the INamedTypeSymbol to its canonical name.
        /// </summary>
        /// <param name="compilation">Compilation from which to retrieve the INamedTypeSymbol from.</param>
        /// <param name="metadataName">Metadata name.</param>
        /// <returns>INamedTypeSymbol, if any.</returns>
        private INamedTypeSymbol GetTypeByMetadataName(Compilation compilation, string metadataName)
        {
            INamedTypeSymbol namedTypeSymbol = compilation.GetTypeByMetadataName(metadataName);
            if (namedTypeSymbol != null)
            {
                this.TypeToFullName.Add(namedTypeSymbol, metadataName);
            }

            return namedTypeSymbol;
        }

        private ImmutableHashSet<INamedTypeSymbol> GetWellKnownCollectionTypes(Compilation compilation)
        {
            var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
            var iCollection = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemCollectionsICollection);
            if (iCollection != null)
            {
                builder.Add(iCollection);
            }

            var genericICollection = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemCollectionsGenericICollection);
            if (genericICollection != null)
            {
                builder.Add(genericICollection);
            }

            var genericIReadOnlyCollection = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemCollectionsGenericIReadOnlyCollection);
            if (genericIReadOnlyCollection != null)
            {
                builder.Add(genericIReadOnlyCollection);
            }

            return builder.ToImmutable();
        }
    }
}
