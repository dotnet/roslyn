// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Represents a MEF composition used for testing.
    /// </summary>
    public sealed class TestComposition
    {
        public static readonly TestComposition Empty = new TestComposition([], [], []);

        private static readonly Dictionary<CacheKey, IExportProviderFactory> s_factoryCache = [];

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            private readonly ImmutableArray<Assembly> _assemblies;
            private readonly ImmutableArray<Type> _parts;
            private readonly ImmutableArray<Type> _excludedPartTypes;

            public CacheKey(ImmutableHashSet<Assembly> assemblies, ImmutableHashSet<Type> parts, ImmutableHashSet<Type> excludedPartTypes)
            {
                _assemblies = [.. assemblies.OrderBy((a, b) => string.CompareOrdinal(a.FullName, b.FullName))];
                _parts = [.. parts.OrderBy((a, b) => string.CompareOrdinal(a.FullName, b.FullName))];
                _excludedPartTypes = [.. excludedPartTypes.OrderBy((a, b) => string.CompareOrdinal(a.FullName, b.FullName))];
            }

            public override bool Equals(object? obj)
                => obj is CacheKey key && Equals(key);

            public bool Equals(CacheKey other)
                => _parts.SequenceEqual(other._parts) &&
                   _excludedPartTypes.SequenceEqual(other._excludedPartTypes) &&
                   _assemblies.SequenceEqual(other._assemblies);

            public override int GetHashCode()
                => Hash.Combine(Hash.Combine(Hash.CombineValues(_assemblies), Hash.CombineValues(_parts)), Hash.CombineValues(_excludedPartTypes));

            public static bool operator ==(CacheKey left, CacheKey right)
                => left.Equals(right);

            public static bool operator !=(CacheKey left, CacheKey right)
                => !(left == right);
        }

        /// <summary>
        /// Assemblies to include in the composition.
        /// </summary>
        public readonly ImmutableHashSet<Assembly> Assemblies;

        /// <summary>
        /// Types to exclude from the composition.
        /// All subtypes of types specified in <see cref="ExcludedPartTypes"/> and defined in <see cref="Assemblies"/> are excluded before <see cref="Parts"/> are added.
        /// </summary>
        public readonly ImmutableHashSet<Type> ExcludedPartTypes;

        /// <summary>
        /// Additional part types to add to the composition.
        /// </summary>
        public readonly ImmutableHashSet<Type> Parts;

        private readonly Lazy<IExportProviderFactory> _exportProviderFactory;

        private TestComposition(ImmutableHashSet<Assembly> assemblies, ImmutableHashSet<Type> parts, ImmutableHashSet<Type> excludedPartTypes)
        {
            Assemblies = assemblies;
            Parts = parts;
            ExcludedPartTypes = excludedPartTypes;

            _exportProviderFactory = new Lazy<IExportProviderFactory>(GetOrCreateFactory);
        }

        /// <summary>
        /// Returns a new instance of <see cref="HostServices"/> for the composition. This will either be a MEF composition or VS MEF composition host, 
        /// depending on what layer the composition is for. Editor Features and VS layers use VS MEF composition while anything else uses System.Composition.
        /// </summary>
        public HostServices GetHostServices()
            => VisualStudioMefHostServices.Create(ExportProviderFactory.CreateExportProvider());

        /// <summary>
        /// VS MEF <see cref="ExportProvider"/>.
        /// </summary>
        public IExportProviderFactory ExportProviderFactory => _exportProviderFactory.Value;

        private IExportProviderFactory GetOrCreateFactory()
        {
            var key = new CacheKey(Assemblies, Parts, ExcludedPartTypes);

            lock (s_factoryCache)
            {
                if (s_factoryCache.TryGetValue(key, out var existing))
                {
                    return existing;
                }
            }

            var newFactory = ExportProviderCache.CreateExportProviderFactory(GetCatalog(), IsRemote);

            lock (s_factoryCache)
            {
                if (s_factoryCache.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                s_factoryCache.Add(key, newFactory);
            }

            return newFactory;
        }

        public bool IsRemote
            => Assemblies.Contains(typeof(Remote.BrokeredServiceBase).Assembly);

        private ComposableCatalog GetCatalog()
        {
            // Compositions should not be realized if they contain the same part in both the explicit include list and
            // the explicit exclude list.
            var configurationOverlap = Parts.Intersect(ExcludedPartTypes);
            Assert.Empty(configurationOverlap);

            return ExportProviderCache.CreateAssemblyCatalog(Assemblies, ExportProviderCache.CreateResolver()).WithoutPartsOfTypes(ExcludedPartTypes).WithParts(Parts);
        }

        public CompositionConfiguration GetCompositionConfiguration()
            => CompositionConfiguration.Create(GetCatalog());

        public TestComposition Add(TestComposition composition)
            => AddAssemblies(composition.Assemblies).AddParts(composition.Parts).AddExcludedPartTypes(composition.ExcludedPartTypes);

        public TestComposition AddAssemblies(params Assembly[]? assemblies)
            => AddAssemblies((IEnumerable<Assembly>?)assemblies);

        public TestComposition AddAssemblies(IEnumerable<Assembly>? assemblies)
            => WithAssemblies(Assemblies.Union(assemblies ?? []));

        public TestComposition AddParts(IEnumerable<Type>? types)
            => WithParts(Parts.Union(types ?? []));

        public TestComposition AddParts(params Type[]? types)
            => AddParts((IEnumerable<Type>?)types);

        public TestComposition AddExcludedPartTypes(IEnumerable<Type>? types)
            => WithExcludedPartTypes(ExcludedPartTypes.Union(types ?? []));

        public TestComposition AddExcludedPartTypes(params Type[]? types)
            => AddExcludedPartTypes((IEnumerable<Type>?)types);

        public TestComposition Remove(TestComposition composition)
            => RemoveAssemblies(composition.Assemblies).RemoveParts(composition.Parts).RemoveExcludedPartTypes(composition.ExcludedPartTypes);

        public TestComposition RemoveAssemblies(params Assembly[]? assemblies)
            => RemoveAssemblies((IEnumerable<Assembly>?)assemblies);

        public TestComposition RemoveAssemblies(IEnumerable<Assembly>? assemblies)
            => WithAssemblies(Assemblies.Except(assemblies ?? []));

        public TestComposition RemoveParts(IEnumerable<Type>? types)
            => WithParts(Parts.Except(types ?? []));

        public TestComposition RemoveParts(params Type[]? types)
            => RemoveParts((IEnumerable<Type>?)types);

        public TestComposition RemoveExcludedPartTypes(IEnumerable<Type>? types)
            => WithExcludedPartTypes(ExcludedPartTypes.Except(types ?? []));

        public TestComposition RemoveExcludedPartTypes(params Type[]? types)
            => RemoveExcludedPartTypes((IEnumerable<Type>?)types);

        public TestComposition WithAssemblies(ImmutableHashSet<Assembly> assemblies)
        {
            if (assemblies == Assemblies)
            {
                return this;
            }

            var testAssembly = assemblies.FirstOrDefault(IsTestAssembly);
            Contract.ThrowIfFalse(testAssembly == null, $"Test assemblies are not allowed in test composition: {testAssembly}. Specify explicit test parts instead.");

            return new TestComposition(assemblies, Parts, ExcludedPartTypes);

            static bool IsTestAssembly(Assembly assembly)
            {
                var name = assembly.GetName().Name!;
                return
                    name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".UnitTests", StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf("Test.Utilities", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        public TestComposition WithParts(ImmutableHashSet<Type> parts)
            => (parts == Parts) ? this : new TestComposition(Assemblies, parts, ExcludedPartTypes);

        public TestComposition WithExcludedPartTypes(ImmutableHashSet<Type> excludedPartTypes)
            => (excludedPartTypes == ExcludedPartTypes) ? this : new TestComposition(Assemblies, Parts, excludedPartTypes);

        /// <summary>
        /// Use for VS MEF composition troubleshooting.
        /// </summary>
        /// <returns>All composition error messages.</returns>
        internal string GetCompositionErrorLog()
        {
            var configuration = CompositionConfiguration.Create(GetCatalog());

            var sb = new StringBuilder();
            foreach (var errorGroup in configuration.CompositionErrors)
            {
                foreach (var error in errorGroup)
                {
                    sb.Append(error.Message);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
