// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Information about analyzers supplied by the host (IDE), which can be completely skipped or its diagnostics partially filtered for the corresponding project
    /// as project analyzer reference (from NuGet) has equivalent analyzer(s) reporting all or subset of diagnostic IDs reported by these analyzers.
    /// </summary>
    internal sealed class SkippedHostAnalyzersInfo : ISkippedAnalyzersInfo
    {
        public static readonly SkippedHostAnalyzersInfo Default = new SkippedHostAnalyzersInfo(
            ImmutableHashSet<DiagnosticAnalyzer>.Empty,
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
            Project project,
            HostDiagnosticAnalyzers hostAnalyzers,
            DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            var projectAnalyzers = hostAnalyzers.CreateProjectDiagnosticAnalyzersPerReference(project).SelectMany(v => v.Value);
            if (projectAnalyzers.IsEmpty())
            {
                return Default;
            }

            var hostAnalyzersPerReference = hostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(project.Language).SelectMany(v => v.Value);
            return Create(projectAnalyzers, hostAnalyzersPerReference, analyzerInfoCache);
        }

        public static SkippedHostAnalyzersInfo Create(
            Project project,
            IEnumerable<AnalyzerReference> hostAnalyzerReferences,
            DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            var projectAnalyzers = project.AnalyzerReferences.SelectMany(p => p.GetAnalyzers(project.Language));
            if (projectAnalyzers.IsEmpty())
            {
                return Default;
            }

            var hostAnalyzers = hostAnalyzerReferences.SelectMany(p => p.GetAnalyzers(project.Language));
            return Create(projectAnalyzers, hostAnalyzers, analyzerInfoCache);
        }

        private static SkippedHostAnalyzersInfo Create(
            IEnumerable<DiagnosticAnalyzer> projectAnalyzers,
            IEnumerable<DiagnosticAnalyzer> hostAnalyzers,
            DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            ComputeSkippedHostAnalyzers(projectAnalyzers, hostAnalyzers, analyzerInfoCache,
                out var fullySkippedHostAnalyzers, out var filteredDiagnosticIdsForAnalyzers);
            if (fullySkippedHostAnalyzers.IsEmpty && filteredDiagnosticIdsForAnalyzers.IsEmpty)
            {
                return Default;
            }

            return new SkippedHostAnalyzersInfo(fullySkippedHostAnalyzers, filteredDiagnosticIdsForAnalyzers);

            static void ComputeSkippedHostAnalyzers(
                IEnumerable<DiagnosticAnalyzer> projectAnalyzers,
                IEnumerable<DiagnosticAnalyzer> hostAnalyzers,
                DiagnosticAnalyzerInfoCache analyzerInfoCache,
                out ImmutableHashSet<DiagnosticAnalyzer> fullySkippedHostAnalyzers,
                out ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<string>> filteredDiagnosticIdsForAnalyzers)
            {
                var idsReportedByProjectAnalyzers = GetIdsReportedByProjectAnalyzers(projectAnalyzers, analyzerInfoCache);
                var fullySkippedHostAnalyzersBuilder = ImmutableHashSet.CreateBuilder<DiagnosticAnalyzer>();
                var partiallySkippedHostAnalyzersBuilder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<string>>();
                using var _ = PooledHashSet<DiagnosticAnalyzer>.GetInstance(out var projectAnalyzersSet);
                projectAnalyzersSet.AddRange(projectAnalyzers);

                foreach (var hostAnalyzer in hostAnalyzers)
                {
                    if (projectAnalyzersSet.Contains(hostAnalyzer))
                    {
                        // Duplicate project and host analyzer.
                        // Do not mark this as a skipped host analyzer as that will also cause the project analyzer to be skipped.
                        // We already perform the required host analyzer reference de-duping in the executor.
                        continue;
                    }

                    if (!ShouldIncludeHostAnalyzer(hostAnalyzer, idsReportedByProjectAnalyzers, analyzerInfoCache, out var skippedIdsForAnalyzer))
                    {
                        fullySkippedHostAnalyzersBuilder.Add(hostAnalyzer);
                    }
                    else if (skippedIdsForAnalyzer.Length > 0)
                    {
                        partiallySkippedHostAnalyzersBuilder.Add(hostAnalyzer, skippedIdsForAnalyzer);
                    }
                }

                fullySkippedHostAnalyzers = fullySkippedHostAnalyzersBuilder.ToImmutable();
                filteredDiagnosticIdsForAnalyzers = partiallySkippedHostAnalyzersBuilder.ToImmutable();
            }

            static ImmutableHashSet<string> GetIdsReportedByProjectAnalyzers(
                IEnumerable<DiagnosticAnalyzer> projectAnalyzers,
                DiagnosticAnalyzerInfoCache analyzerInfoCache)
            {
                var builder = ImmutableHashSet.CreateBuilder<string>();

                foreach (var analyzer in projectAnalyzers)
                {
                    var descriptors = analyzerInfoCache.GetDiagnosticDescriptors(analyzer);
                    builder.AddRange(descriptors.Select(d => d.Id));
                }

                return builder.ToImmutable();
            }

            static bool ShouldIncludeHostAnalyzer(
                DiagnosticAnalyzer hostAnalyzer,
                ImmutableHashSet<string> idsReportedByProjectAnalyzers,
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
                    if (idsReportedByProjectAnalyzers.Contains(descriptor.Id))
                    {
                        skippedDiagnosticIdsBuilder.Add(descriptor.Id);
                    }
                    else
                    {
                        shouldInclude = true;
                    }
                }

                skippedDiagnosticIdsForAnalyzer = skippedDiagnosticIdsBuilder.ToImmutableAndFree();
                return shouldInclude;
            }
        }
    }
}
