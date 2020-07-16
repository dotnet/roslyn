// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Represents a MEF composition used for testing.
    /// </summary>
    public sealed class TestComposition
    {
        public static readonly TestComposition Empty = new TestComposition(ImmutableHashSet<Assembly>.Empty, ImmutableHashSet<Type>.Empty, ImmutableHashSet<Type>.Empty);

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

            _exportProviderFactory = new Lazy<IExportProviderFactory>(() => ExportProviderCache.GetOrCreateExportProviderFactory(GetCatalog()));
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

        private ComposableCatalog GetCatalog()
            => ExportProviderCache.GetOrCreateAssemblyCatalog(Assemblies, ExportProviderCache.CreateResolver()).WithoutPartsOfTypes(ExcludedPartTypes).WithParts(Parts);

        public TestComposition Add(TestComposition composition)
            => WithAssemblies(composition.Assemblies).WithParts(composition.Parts).WithExcludedPartTypes(composition.ExcludedPartTypes);

        public TestComposition AddAssemblies(params Assembly[]? assemblies)
            => AddAssemblies((IEnumerable<Assembly>?)assemblies);

        public TestComposition AddAssemblies(IEnumerable<Assembly>? assemblies)
            => WithAssemblies(Assemblies.Union(assemblies ?? Array.Empty<Assembly>());

        public TestComposition AddParts(IEnumerable<Type>? types)
            => WithParts(Parts.Union(types ?? Array.Empty<Type>()));

        public TestComposition AddParts(params Type[]? types)
            => AddParts((IEnumerable<Type>?)types);

        public TestComposition AddExcludedParts(IEnumerable<Type>? types)
            => WithExcludedPartTypes(Parts.Union(types ?? Array.Empty<Type>()));

        public TestComposition AddExcludedParts(params Type[]? types)
            => AddExcludedParts((IEnumerable<Type>?)types);

        public TestComposition Remove(TestComposition composition)
            => WithAssemblies(composition.Assemblies).WithParts(composition.Parts).WithExcludedPartTypes(composition.ExcludedPartTypes);

        public TestComposition RemoveAssemblies(params Assembly[]? assemblies)
            => RemoveAssemblies((IEnumerable<Assembly>?)assemblies);

        public TestComposition RemoveAssemblies(IEnumerable<Assembly>? assemblies)
            => WithAssemblies(Assemblies.Except(assemblies ?? Array.Empty<Assembly>()));

        public TestComposition RemoveParts(IEnumerable<Type>? types)
            => WithParts(Parts.Except(types ?? Array.Empty<Type>()));

        public TestComposition RemoveParts(params Type[]? types)
            => RemoveParts((IEnumerable<Type>?)types);

        public TestComposition RemoveExcludedParts(IEnumerable<Type>? types)
            => WithExcludedPartTypes(Parts.Except(types ?? Array.Empty<Type>()));

        public TestComposition RemoveExcludedParts(params Type[]? types)
            => RemoveExcludedParts((IEnumerable<Type>?)types);

        public TestComposition WithAssemblies(ImmutableHashSet<Assembly> assemblies)
            => (assemblies == Assemblies) ? this : new TestComposition(assemblies, Parts, ExcludedPartTypes);

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
