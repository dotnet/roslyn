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
            FullNameToType = new Dictionary<string, INamedTypeSymbol>();

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
            Boolean = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemBoolean);
            Byte = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemByte);
            Char = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemChar);
            DateTime = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDateTime);
            Decimal = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDecimal);
            Double = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemDouble);
            TimeSpanParse = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemGlobalizationTimeSpanParse);
            Guid = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemGuid);
            Int16 = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemInt16);
            Int32 = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemInt32);
            Int64 = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemInt64);
            Number = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemNumber);
            Single = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemSingle);
            TimeSpan = GetTypeByMetadataName(compilation, Analyzer.Utilities.WellKnownTypes.SystemTimeSpan);
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
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Boolean"/>
        /// </summary>
        public INamedTypeSymbol Boolean { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Byte"/>
        /// </summary>
        public INamedTypeSymbol Byte { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Char"/>
        /// </summary>
        public INamedTypeSymbol Char { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.DateTime"/>
        /// </summary>
        public INamedTypeSymbol DateTime { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Decimal"/>
        /// </summary>
        public INamedTypeSymbol Decimal { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Double"/>
        /// </summary>
        public INamedTypeSymbol Double { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Globalization.TimeSpanParse"/>
        /// </summary>
        public INamedTypeSymbol TimeSpanParse { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Guid"/>
        /// </summary>
        public INamedTypeSymbol Guid { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Int16"/>
        /// </summary>
        public INamedTypeSymbol Int16 { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Int32"/>
        /// </summary>
        public INamedTypeSymbol Int32 { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Int64"/>
        /// </summary>
        public INamedTypeSymbol Int64 { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Number"/>
        /// </summary>
        public INamedTypeSymbol Number { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.Single"/>
        /// </summary>
        public INamedTypeSymbol Single { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"> for <see cref="System.TimeSpan"/>
        /// </summary>
        public INamedTypeSymbol TimeSpan { get; }
        
        /// <summary>
        /// Set containing following named types, if not null:
        /// 1. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.ICollection"/>
        /// 2. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.ICollection{T}"/>
        /// 3. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>
        /// </summary>
        public ImmutableHashSet<INamedTypeSymbol> CollectionTypes { get; }

        /// <summary>
        /// Mapping of <see cref="ISymbol"/> to full name (e.g. "System.Exception").
        /// </summary>
        private Dictionary<ISymbol, string> TypeToFullName { get; }

        /// <summary>
        /// Mapping of full name to <see cref="INamedTypeSymbol"/>.
        /// </summary>
        private Dictionary<string, INamedTypeSymbol> FullNameToType { get; }

        /// <summary>
        /// Attempts to get the full type name (namespace + type) of the specifed symbol.
        /// </summary>
        /// <param name="symbol">Symbol, if any.</param>
        /// <param name="fullTypeName">Namespace + type name.</param>
        /// <returns>True if found, false otherwise.</returns>
        /// <remarks>This only works for types that this <see cref="WellKnownTypeProvider"/> knows about.</remarks>
        public bool TryGetFullTypeName(ISymbol symbol, out string fullTypeName)
        {
            return TypeToFullName.TryGetValue(symbol, out fullTypeName);
        }

        /// <summary>
        /// Attempts to get the type by the full type name.
        /// </summary>
        /// <param name="fullTypeName">>Namespace + type name.</param>
        /// <param name="namedTypeSymbol">Named type symbol, if any.</param>
        /// <returns>True if found, false otherwise.</returns>
        /// <remarks>This only works for types that this <see cref="WellKnownTypeProvider"/> knows about.</remarks>
        public bool TryGetType(string fullTypeName, out INamedTypeSymbol namedTypeSymbol)
        {
            return FullNameToType.TryGetValue(fullTypeName, out namedTypeSymbol);
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
                this.FullNameToType.Add(metadataName, namedTypeSymbol);
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
