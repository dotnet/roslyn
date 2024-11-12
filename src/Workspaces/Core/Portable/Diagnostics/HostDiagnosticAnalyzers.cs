// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed class HostDiagnosticAnalyzers
{
    /// <summary>
    /// Key is <see cref="AnalyzerReference.Id"/>.
    /// 
    /// We use the key to de-duplicate analyzer references if they are referenced from multiple places.
    /// </summary>
    private readonly ImmutableDictionary<object, AnalyzerReference> _hostAnalyzerReferencesMap;

    /// <summary>
    /// Key is the language the <see cref="DiagnosticAnalyzer"/> supports and key for the second map is analyzer reference identity and
    /// <see cref="DiagnosticAnalyzer"/> for that assembly reference.
    /// 
    /// Entry will be lazily filled in.
    /// </summary>
    private readonly ConcurrentDictionary<string, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>> _hostDiagnosticAnalyzersPerLanguageMap;

    /// <summary>
    /// Key is <see cref="AnalyzerReference.Id"/>.
    /// 
    /// Value is set of <see cref="DiagnosticAnalyzer"/> that belong to the <see cref="AnalyzerReference"/>.
    /// 
    /// We populate it lazily. otherwise, we will bring in all analyzers preemptively
    /// </summary>
    private readonly Lazy<ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>> _lazyHostDiagnosticAnalyzersPerReferenceMap;

    /// <summary>
    /// Maps <see cref="LanguageNames"/> to compiler diagnostic analyzers.
    /// </summary>
    private ImmutableDictionary<string, DiagnosticAnalyzer> _compilerDiagnosticAnalyzerMap;

    /// <summary>
    /// Maps list of analyzer references and <see cref="LanguageNames"/> to <see cref="SkippedHostAnalyzersInfo"/>.
    /// </summary>
    /// <remarks>
    /// TODO: https://github.com/dotnet/roslyn/issues/42848
    /// It is quite common for multiple projects to have the same set of analyzer references, yet we will create
    /// multiple instances of the analyzer list and thus not share the info.
    /// </remarks>
    private readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, StrongBox<ImmutableDictionary<string, SkippedHostAnalyzersInfo>>> _skippedHostAnalyzers = new();

    internal HostDiagnosticAnalyzers(IReadOnlyList<AnalyzerReference> hostAnalyzerReferences)
    {
        HostAnalyzerReferences = hostAnalyzerReferences;
        _hostAnalyzerReferencesMap = CreateAnalyzerReferencesMap(hostAnalyzerReferences);
        _hostDiagnosticAnalyzersPerLanguageMap = new ConcurrentDictionary<string, ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(concurrencyLevel: 2, capacity: 2);
        _lazyHostDiagnosticAnalyzersPerReferenceMap = new Lazy<ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>>(() => CreateDiagnosticAnalyzersPerReferenceMap(_hostAnalyzerReferencesMap), isThreadSafe: true);

        _compilerDiagnosticAnalyzerMap = ImmutableDictionary<string, DiagnosticAnalyzer>.Empty;
    }

    /// <summary>
    /// List of host <see cref="AnalyzerReference"/>s
    /// </summary>
    public IReadOnlyList<AnalyzerReference> HostAnalyzerReferences { get; }

    /// <summary>
    /// Get <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="language"/>
    /// </summary> 
    public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> GetOrCreateHostDiagnosticAnalyzersPerReference(string language)
        => _hostDiagnosticAnalyzersPerLanguageMap.GetOrAdd(language, CreateHostDiagnosticAnalyzersAndBuildMap);

    public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsPerReference(DiagnosticAnalyzerInfoCache infoCache)
    {
        return ConvertReferenceIdentityToName(
            CreateDiagnosticDescriptorsPerReference(infoCache, _lazyHostDiagnosticAnalyzersPerReferenceMap.Value),
            _hostAnalyzerReferencesMap);
    }

    public ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsPerReference(DiagnosticAnalyzerInfoCache infoCache, Project project)
    {
        var descriptorPerReference = CreateDiagnosticDescriptorsPerReference(infoCache, CreateDiagnosticAnalyzersPerReference(project));
        var map = _hostAnalyzerReferencesMap.AddRange(CreateProjectAnalyzerReferencesMap(project.AnalyzerReferences));
        return ConvertReferenceIdentityToName(descriptorPerReference, map);
    }

    private static ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> ConvertReferenceIdentityToName(
        ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> descriptorsPerReference,
        ImmutableDictionary<object, AnalyzerReference> map)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticDescriptor>>();

        foreach (var (id, descriptors) in descriptorsPerReference)
        {
            if (!map.TryGetValue(id, out var reference) || reference == null)
            {
                continue;
            }

            var displayName = reference.Display ?? WorkspacesResources.Unknown;

            // if there are duplicates, merge descriptors
            if (builder.TryGetValue(displayName, out var existing))
            {
                builder[displayName] = existing.AddRange(descriptors);
                continue;
            }

            builder.Add(displayName, descriptors);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="project"/> that
    /// includes both host and project analyzers
    /// </summary>
    public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReference(Project project)
    {
        var hostAnalyzerReferences = GetOrCreateHostDiagnosticAnalyzersPerReference(project.Language);
        var projectAnalyzerReferences = CreateProjectDiagnosticAnalyzersPerReference(project.AnalyzerReferences, project.Language);

        return MergeDiagnosticAnalyzerMap(hostAnalyzerReferences, projectAnalyzerReferences);
    }

    /// <summary>
    /// Create <see cref="AnalyzerReference"/> identity and <see cref="DiagnosticAnalyzer"/>s map for given <paramref name="project"/> that
    /// has only project analyzers
    /// </summary>
    public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateProjectDiagnosticAnalyzersPerReference(Project project)
        => CreateProjectDiagnosticAnalyzersPerReference(project.AnalyzerReferences, project.Language);

    public ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateProjectDiagnosticAnalyzersPerReference(IReadOnlyList<AnalyzerReference> projectAnalyzerReferences, string language)
        => CreateDiagnosticAnalyzersPerReferenceMap(CreateProjectAnalyzerReferencesMap(projectAnalyzerReferences), language);

    /// <summary>
    /// Return compiler <see cref="DiagnosticAnalyzer"/> for the given language.
    /// </summary>
    public DiagnosticAnalyzer? GetCompilerDiagnosticAnalyzer(string language)
    {
        _ = GetOrCreateHostDiagnosticAnalyzersPerReference(language);
        if (_compilerDiagnosticAnalyzerMap.TryGetValue(language, out var compilerAnalyzer))
        {
            return compilerAnalyzer;
        }

        return null;
    }

    private ImmutableDictionary<object, AnalyzerReference> CreateProjectAnalyzerReferencesMap(IReadOnlyList<AnalyzerReference> projectAnalyzerReferences)
        => CreateAnalyzerReferencesMap(projectAnalyzerReferences.Where(reference => !_hostAnalyzerReferencesMap.ContainsKey(reference.Id)));

    private static ImmutableDictionary<object, ImmutableArray<DiagnosticDescriptor>> CreateDiagnosticDescriptorsPerReference(
        DiagnosticAnalyzerInfoCache infoCache,
        ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> analyzersMap)
    {
        var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticDescriptor>>();
        foreach (var (referenceId, analyzers) in analyzersMap)
        {
            var descriptors = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();
            foreach (var analyzer in analyzers)
            {
                // given map should be in good shape. no duplication. no null and etc
                descriptors.AddRange(infoCache.GetDiagnosticDescriptors(analyzer));
            }

            // there can't be duplication since _hostAnalyzerReferenceMap is already de-duplicated.
            builder.Add(referenceId, descriptors.ToImmutable());
        }

        return builder.ToImmutable();
    }

    private ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateHostDiagnosticAnalyzersAndBuildMap(string language)
    {
        Contract.ThrowIfNull(language);

        var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticAnalyzer>>();
        foreach (var (referenceIdentity, reference) in _hostAnalyzerReferencesMap)
        {
            var analyzers = reference.GetAnalyzers(language);
            if (analyzers.Length == 0)
            {
                continue;
            }

            UpdateCompilerAnalyzerMapIfNeeded(language, analyzers);

            // there can't be duplication since _hostAnalyzerReferenceMap is already de-duplicated.
            builder.Add(referenceIdentity, analyzers);
        }

        return builder.ToImmutable();
    }

    private void UpdateCompilerAnalyzerMapIfNeeded(string language, ImmutableArray<DiagnosticAnalyzer> analyzers)
    {
        if (_compilerDiagnosticAnalyzerMap.ContainsKey(language))
        {
            return;
        }

        foreach (var analyzer in analyzers)
        {
            if (analyzer.IsCompilerAnalyzer())
            {
                ImmutableInterlocked.GetOrAdd(ref _compilerDiagnosticAnalyzerMap, language, analyzer);
                return;
            }
        }
    }

    private static ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> CreateDiagnosticAnalyzersPerReferenceMap(
        IDictionary<object, AnalyzerReference> analyzerReferencesMap, string? language = null)
    {
        var builder = ImmutableDictionary.CreateBuilder<object, ImmutableArray<DiagnosticAnalyzer>>();

        // Randomize the order we process analyzer references to minimize static constructor/JIT contention during analyzer instantiation.
        foreach (var reference in Shuffle(analyzerReferencesMap))
        {
            var analyzers = language == null ? reference.Value.GetAnalyzersForAllLanguages() : reference.Value.GetAnalyzers(language);
            if (analyzers.Length == 0)
            {
                continue;
            }

            // input "analyzerReferencesMap" is a dictionary, so there will be no duplication here.
            builder.Add(reference.Key, analyzers.WhereNotNull().ToImmutableArray());
        }

        return builder.ToImmutable();

        static IEnumerable<KeyValuePair<object, AnalyzerReference>> Shuffle(IDictionary<object, AnalyzerReference> source)
        {
            var random =
#if NET6_0_OR_GREATER
                    Random.Shared;
#else
                    new Random();
#endif

            using var _ = ArrayBuilder<KeyValuePair<object, AnalyzerReference>>.GetInstance(source.Count, out var builder);
            builder.AddRange(source);

            for (var i = builder.Count - 1; i >= 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                yield return builder[swapIndex];
                builder[swapIndex] = builder[i];
            }
        }
    }

    private static ImmutableDictionary<object, AnalyzerReference> CreateAnalyzerReferencesMap(IEnumerable<AnalyzerReference> analyzerReferences)
    {
        var builder = ImmutableDictionary.CreateBuilder<object, AnalyzerReference>();
        foreach (var reference in analyzerReferences)
        {
            var key = reference.Id;

            // filter out duplicated analyzer reference
            if (builder.ContainsKey(key))
            {
                continue;
            }

            builder.Add(key, reference);
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> MergeDiagnosticAnalyzerMap(
        ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> map1,
        ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> map2)
    {
        var current = map1;
        var seen = new HashSet<DiagnosticAnalyzer>(map1.Values.SelectMany(v => v));

        foreach (var (referenceIdentity, analyzers) in map2)
        {
            if (map1.ContainsKey(referenceIdentity))
            {
                continue;
            }

            current = current.Add(referenceIdentity, analyzers.Where(seen.Add).ToImmutableArray());
        }

        return current;
    }

    public SkippedHostAnalyzersInfo GetSkippedAnalyzersInfo(Project project, DiagnosticAnalyzerInfoCache infoCache)
    {
        var box = _skippedHostAnalyzers.GetOrCreateValue(project.AnalyzerReferences);

        if (box.Value != null && box.Value.TryGetValue(project.Language, out var info))
        {
            return info;
        }

        lock (box)
        {
            box.Value ??= ImmutableDictionary<string, SkippedHostAnalyzersInfo>.Empty;

            if (!box.Value.TryGetValue(project.Language, out info))
            {
                info = SkippedHostAnalyzersInfo.Create(this, project.AnalyzerReferences, project.Language, infoCache);
                box.Value = box.Value.Add(project.Language, info);
            }

            return info;
        }
    }
}
