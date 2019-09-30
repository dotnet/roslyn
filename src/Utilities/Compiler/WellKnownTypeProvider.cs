// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;
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
            _fullNameToTypeMap = new ConcurrentDictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);

            Exception = GetTypeByMetadataName(WellKnownTypeNames.SystemExceptionFullName);
            Contract = GetTypeByMetadataName(WellKnownTypeNames.SystemDiagnosticContractsContract);
            IDisposable = GetTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
            Monitor = GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingMonitor);
            Interlocked = GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingInterlocked);
            Task = GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask);
            GenericTask = GetTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksGenericTask);
            CollectionTypes = GetWellKnownCollectionTypes();
            SerializationInfo = GetTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationSerializationInfo);
            Array = GetTypeBySpecialType(SpecialType.System_Array);
            String = GetTypeBySpecialType(SpecialType.System_String);
            Object = GetTypeBySpecialType(SpecialType.System_Object);
            IntPtr = GetTypeBySpecialType(SpecialType.System_IntPtr);
            UIntPtr = GetTypeBySpecialType(SpecialType.System_UIntPtr);
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
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Interlocked"/>
        /// </summary>
        public INamedTypeSymbol Interlocked { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for 'System.Runtime.Serialization.SerializationInfo' type />
        /// </summary>
        public INamedTypeSymbol SerializationInfo { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IEquatable{T}"/>
        /// </summary>
        public INamedTypeSymbol GenericIEquatable { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="SpecialType.System_Array"/>
        /// </summary>
        public INamedTypeSymbol Array { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="SpecialType.System_String"/>
        /// </summary>
        public INamedTypeSymbol String { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="SpecialType.System_Object"/>
        /// </summary>
        public INamedTypeSymbol Object { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="SpecialType.System_IntPtr"/>
        /// </summary>
        public INamedTypeSymbol IntPtr { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="SpecialType.System_UIntPtr"/>
        /// </summary>
        public INamedTypeSymbol UIntPtr { get; }

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

        /// <summary>
        /// Gets a type by its full type name.
        /// </summary>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <returns>The <see cref="INamedTypeSymbol"/> if found, null otherwise.</returns>
        public INamedTypeSymbol GetTypeByMetadataName(string fullTypeName)
        {
            TryGetTypeByMetadataName(fullTypeName, out INamedTypeSymbol namedTypeSymbol);
            return namedTypeSymbol;
        }

        /// <summary>
        /// Attempts to get the type by its special type.
        /// </summary>
        /// <param name="specialType">ID of the special runtime type.</param>
        /// <param name="namedTypeSymbol">Named type symbol, if any.</param>
        /// <returns>True if found in the compilation, false otherwise.</returns>
        private bool TryGetTypeBySpecialType(SpecialType specialType, out INamedTypeSymbol namedTypeSymbol)
        {
            namedTypeSymbol = _fullNameToTypeMap.GetOrAdd(
                specialType.ToString(),
                (string s) => Compilation.GetSpecialType((SpecialType)Enum.Parse(typeof(SpecialType), s)));
            return namedTypeSymbol != null;
        }

        /// <summary>
        /// Gets a type by its special type.
        /// </summary>
        /// <param name="specialType">ID of the special runtime type.</param>
        /// <returns>The <see cref="INamedTypeSymbol"/> if found, null otherwise.</returns>
        private INamedTypeSymbol GetTypeBySpecialType(SpecialType specialType)
        {
            TryGetTypeBySpecialType(specialType, out INamedTypeSymbol namedTypeSymbol);
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
