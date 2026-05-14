// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.AspNetCore.Razor.Test.Common.Mef;

/// <summary>
/// Represents a MEF composition used for testing.
/// </summary>
public sealed partial class TestComposition
{
    public static readonly TestComposition Empty = new(
        ImmutableHashSet<Assembly>.Empty,
        ImmutableHashSet<Type>.Empty,
        ImmutableHashSet<Type>.Empty);

    public static readonly TestComposition RoslynFeatures = Empty
        .AddAssemblies(MefHostServices.DefaultAssemblies)
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.CSharp.Features.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.Features.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.ExternalAccess.Razor.Features.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.LanguageServer.Protocol.dll"))
        .AddParts(typeof(RazorTestWorkspaceRegistrationService));

#if NETFRAMEWORK
    public static readonly TestComposition Roslyn = Empty
        .AddAssemblies(MefHostServices.DefaultAssemblies)
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.EditorFeatures.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.ExternalAccess.Razor.Features.dll"))
        .AddAssemblies(Assembly.LoadFrom("Microsoft.CodeAnalysis.LanguageServer.Protocol.dll"))
        .AddParts(typeof(RazorTestWorkspaceRegistrationService));

    public static readonly TestComposition Editor = Empty
        .AddAssemblies(Assembly.LoadFrom("Microsoft.VisualStudio.Text.Implementation.dll"))
        .AddParts(typeof(TestExportJoinableTaskContext));
#endif

    private static readonly Dictionary<CacheKey, IExportProviderFactory> s_factoryCache = [];

    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        private readonly ImmutableArray<Assembly> _assemblies;
        private readonly ImmutableArray<Type> _parts;
        private readonly ImmutableArray<Type> _excludedPartTypes;

        public CacheKey(ImmutableHashSet<Assembly> assemblies, ImmutableHashSet<Type> parts, ImmutableHashSet<Type> excludedPartTypes)
        {
            _assemblies = assemblies.OrderBy(a => a.FullName, StringComparer.Ordinal).ToImmutableArray();
            _parts = parts.OrderBy(a => a.FullName, StringComparer.Ordinal).ToImmutableArray();
            _excludedPartTypes = excludedPartTypes.OrderBy(a => a.FullName, StringComparer.Ordinal).ToImmutableArray();
        }

        public override bool Equals(object? obj)
            => obj is CacheKey key && Equals(key);

        public bool Equals(CacheKey other)
            => _parts.SequenceEqual(other._parts) &&
               _excludedPartTypes.SequenceEqual(other._excludedPartTypes) &&
               _assemblies.SequenceEqual(other._assemblies);

        public override int GetHashCode()
        {
            var hashCode = -744873704;

            foreach (var assembly in _assemblies)
                hashCode = hashCode * -1521134295 + assembly.GetHashCode();

            foreach (var part in _parts)
                hashCode = hashCode * -1521134295 + part.GetHashCode();

            foreach (var excludedPartType in _excludedPartTypes)
                hashCode = hashCode * -1521134295 + excludedPartType.GetHashCode();

            return hashCode;
        }

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

        var newFactory = ExportProviderCache.CreateExportProviderFactory(GetCatalog());

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

    private ComposableCatalog GetCatalog()
        => ExportProviderCache.CreateAssemblyCatalog(Assemblies, ExportProviderCache.CreateResolver()).WithoutPartsOfTypes(ExcludedPartTypes).WithParts(Parts);

    public TestComposition Add(TestComposition composition)
        => AddAssemblies(composition.Assemblies).AddParts(composition.Parts).AddExcludedPartTypes(composition.ExcludedPartTypes);

    public TestComposition AddAssemblies(params Assembly[]? assemblies)
        => AddAssemblies((IEnumerable<Assembly>?)assemblies);

    public TestComposition AddAssemblies(IEnumerable<Assembly>? assemblies)
        => WithAssemblies(Assemblies.Union(assemblies ?? Array.Empty<Assembly>()));

    public TestComposition AddParts(IEnumerable<Type>? types)
        => WithParts(Parts.Union(types ?? Array.Empty<Type>()));

    public TestComposition AddParts(params Type[]? types)
        => AddParts((IEnumerable<Type>?)types);

    public TestComposition AddExcludedPartTypes(IEnumerable<Type>? types)
        => WithExcludedPartTypes(ExcludedPartTypes.Union(types ?? Array.Empty<Type>()));

    public TestComposition AddExcludedPartTypes(params Type[]? types)
        => AddExcludedPartTypes((IEnumerable<Type>?)types);

    public TestComposition Remove(TestComposition composition)
        => RemoveAssemblies(composition.Assemblies).RemoveParts(composition.Parts).RemoveExcludedPartTypes(composition.ExcludedPartTypes);

    public TestComposition RemoveAssemblies(params Assembly[]? assemblies)
        => RemoveAssemblies((IEnumerable<Assembly>?)assemblies);

    public TestComposition RemoveAssemblies(IEnumerable<Assembly>? assemblies)
        => WithAssemblies(Assemblies.Except(assemblies ?? Array.Empty<Assembly>()));

    public TestComposition RemoveParts(IEnumerable<Type>? types)
        => WithParts(Parts.Except(types ?? Array.Empty<Type>()));

    public TestComposition RemoveParts(params Type[]? types)
        => RemoveParts((IEnumerable<Type>?)types);

    public TestComposition RemoveExcludedPartTypes(IEnumerable<Type>? types)
        => WithExcludedPartTypes(ExcludedPartTypes.Except(types ?? Array.Empty<Type>()));

    public TestComposition RemoveExcludedPartTypes(params Type[]? types)
        => RemoveExcludedPartTypes((IEnumerable<Type>?)types);

    public TestComposition WithAssemblies(ImmutableHashSet<Assembly> assemblies)
    {
        if (assemblies == Assemblies)
        {
            return this;
        }

        var testAssembly = assemblies.FirstOrDefault(IsTestAssembly);
        Verify.Operation(testAssembly is null, $"Test assemblies are not allowed in test composition: {testAssembly}. Specify explicit test parts instead.");

        return new TestComposition(assemblies, Parts, ExcludedPartTypes);

        static bool IsTestAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name!;
            return
                name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".UnitTests", StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("Test.Utilities", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Test.Common", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public TestComposition WithParts(ImmutableHashSet<Type> parts)
        => parts == Parts ? this : new TestComposition(Assemblies, parts, ExcludedPartTypes);

    public TestComposition WithExcludedPartTypes(ImmutableHashSet<Type> excludedPartTypes)
        => excludedPartTypes == ExcludedPartTypes ? this : new TestComposition(Assemblies, Parts, excludedPartTypes);

    /// <summary>
    /// Use for VS MEF composition troubleshooting.
    /// </summary>
    /// <returns>All composition error messages.</returns>
    public IEnumerable<string> GetCompositionErrors()
    {
        var configuration = CompositionConfiguration.Create(GetCatalog());

        foreach (var errorGroup in configuration.CompositionErrors)
        {
            foreach (var error in errorGroup)
            {
                yield return error.Message;
            }
        }
    }
}
