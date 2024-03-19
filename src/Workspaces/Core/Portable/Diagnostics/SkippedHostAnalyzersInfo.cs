// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

#if !NETCOREAPP
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Information about analyzers supplied by the host (IDE), which can be completely skipped or its diagnostics partially filtered for the corresponding project
/// as project analyzer reference (from NuGet) has equivalent analyzer(s) reporting all or subset of diagnostic IDs reported by these analyzers.
/// </summary>
internal readonly struct SkippedHostAnalyzersInfo
{
    public static readonly SkippedHostAnalyzersInfo Empty = new(
        [],
        ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<string>>.Empty);

    /// <summary>
    /// Analyzers supplied by the host (IDE), which can be completely skipped for the corresponding project
    /// as project analyzer reference has equivalent analyzer(s) reporting all diagnostic IDs reported by these analyzers.
    /// </summary>
    public ImmutableHashSet<DiagnosticAnalyzer> SkippedAnalyzers { get; }

    /// <summary>
    /// Analyzer to diagnostic ID map, such that the diagnostics of those IDs reported by the analyzer should be filtered
    /// for a correspndiong project.
    /// This includes the analyzers supplied by the host (IDE), such that project's analyzer references (from NuGet)
    /// has equivalent analyzer(s) reporting subset of diagnostic IDs reported by these analyzers.
    /// </summary>
    public ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<string>> FilteredDiagnosticIdsForAnalyzers { get; }

    private SkippedHostAnalyzersInfo(
        ImmutableHashSet<DiagnosticAnalyzer> skippedHostAnalyzers,
        ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<string>> filteredDiagnosticIdsForAnalyzers)
    {
        SkippedAnalyzers = skippedHostAnalyzers;
        FilteredDiagnosticIdsForAnalyzers = filteredDiagnosticIdsForAnalyzers;
    }

    public static SkippedHostAnalyzersInfo Create(
        HostDiagnosticAnalyzers hostAnalyzers,
        IReadOnlyList<AnalyzerReference> projectAnalyzerReferences,
        string language,
        DiagnosticAnalyzerInfoCache analyzerInfoCache)
    {
        using var _1 = PooledHashSet<object>.GetInstance(out var projectAnalyzerIds);
        using var _2 = PooledHashSet<string>.GetInstance(out var projectAnalyzerDiagnosticIds);
        using var _3 = PooledHashSet<string>.GetInstance(out var projectSuppressedDiagnosticIds);

        foreach (var (analyzerId, analyzers) in hostAnalyzers.CreateProjectDiagnosticAnalyzersPerReference(projectAnalyzerReferences, language))
        {
            projectAnalyzerIds.Add(analyzerId);

            foreach (var analyzer in analyzers)
            {
                foreach (var descriptor in analyzerInfoCache.GetDiagnosticDescriptors(analyzer))
                {
                    projectAnalyzerDiagnosticIds.Add(descriptor.Id);
                }

                if (analyzer is DiagnosticSuppressor suppressor)
                {
                    foreach (var descriptor in analyzerInfoCache.GetDiagnosticSuppressions(suppressor))
                    {
                        projectSuppressedDiagnosticIds.Add(descriptor.SuppressedDiagnosticId);
                    }
                }
            }
        }

        if (projectAnalyzerIds.Count == 0)
        {
            return Empty;
        }

        var fullySkippedHostAnalyzersBuilder = ImmutableHashSet.CreateBuilder<DiagnosticAnalyzer>();
        var partiallySkippedHostAnalyzersBuilder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<string>>();

        foreach (var (hostAnalyzerId, analyzers) in hostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(language))
        {
            foreach (var hostAnalyzer in analyzers)
            {
                if (projectAnalyzerIds.Contains(hostAnalyzerId))
                {
                    // Duplicate project and host analyzer.
                    // Do not mark this as a skipped host analyzer as that will also cause the project analyzer to be skipped.
                    // We already perform the required host analyzer reference de-duping in the executor.
                    continue;
                }

                if (!ShouldIncludeHostAnalyzer(hostAnalyzer, projectAnalyzerDiagnosticIds, projectSuppressedDiagnosticIds, analyzerInfoCache, out var skippedIdsForAnalyzer))
                {
                    fullySkippedHostAnalyzersBuilder.Add(hostAnalyzer);
                }
                else if (skippedIdsForAnalyzer.Length > 0)
                {
                    partiallySkippedHostAnalyzersBuilder.Add(hostAnalyzer, skippedIdsForAnalyzer);
                }
            }
        }

        var fullySkippedHostAnalyzers = fullySkippedHostAnalyzersBuilder.ToImmutable();
        var filteredDiagnosticIdsForAnalyzers = partiallySkippedHostAnalyzersBuilder.ToImmutable();

        if (fullySkippedHostAnalyzers.IsEmpty && filteredDiagnosticIdsForAnalyzers.IsEmpty)
        {
            return Empty;
        }

        return new SkippedHostAnalyzersInfo(fullySkippedHostAnalyzers, filteredDiagnosticIdsForAnalyzers);

        static bool ShouldIncludeHostAnalyzer(
            DiagnosticAnalyzer hostAnalyzer,
            HashSet<string> projectAnalyzerDiagnosticIds,
            HashSet<string> projectSuppressedDiagnosticIds,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            out ImmutableArray<string> skippedDiagnosticIdsForAnalyzer)
        {
            // Include only those host (VSIX) analyzers that report at least one unique diagnostic ID
            // which is not reported by any project (NuGet) analyzer.
            // See https://github.com/dotnet/roslyn/issues/18818.

            var shouldInclude = false;

            var descriptors = analyzerInfoCache.GetDiagnosticDescriptors(hostAnalyzer);
            var skippedDiagnosticIdsBuilder = ArrayBuilder<string>.GetInstance();
            foreach (var descriptor in descriptors)
            {
                if (projectAnalyzerDiagnosticIds.Contains(descriptor.Id))
                {
                    skippedDiagnosticIdsBuilder.Add(descriptor.Id);
                }
                else
                {
                    shouldInclude = true;
                }
            }

            if (hostAnalyzer is DiagnosticSuppressor suppressor)
            {
                skippedDiagnosticIdsForAnalyzer = [];

                // Only execute host suppressor if it does not suppress any diagnostic ID reported by project analyzer
                // and does not share any suppression ID with a project suppressor.
                foreach (var descriptor in analyzerInfoCache.GetDiagnosticSuppressions(suppressor))
                {
                    if (projectAnalyzerDiagnosticIds.Contains(descriptor.SuppressedDiagnosticId) ||
                        projectSuppressedDiagnosticIds.Contains(descriptor.SuppressedDiagnosticId))
                    {
                        return false;
                    }
                }

                return true;
            }

            skippedDiagnosticIdsForAnalyzer = skippedDiagnosticIdsBuilder.ToImmutableAndFree();
            return shouldInclude;
        }
    }
}
