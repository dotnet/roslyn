// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// This is in charge of anything related to <see cref="DiagnosticAnalyzer"/>
    /// </summary>
    private sealed partial class StateManager(DiagnosticAnalyzerInfoCache analyzerInfoCache)
    {
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache = analyzerInfoCache;

        /// <summary>
        /// Analyzers supplied by the host (IDE). These are built-in to the IDE, the compiler, or from an installed IDE extension (VSIX). 
        /// Maps language name to the analyzers and their state.
        /// </summary>
        private ImmutableDictionary<HostAnalyzerInfoKey, HostAnalyzerInfo> _hostAnalyzerStateMap = ImmutableDictionary<HostAnalyzerInfoKey, HostAnalyzerInfo>.Empty;

        /// <summary>
        /// Analyzers referenced by the project via a PackageReference. Updates are protected by _projectAnalyzerStateMapGuard.
        /// ImmutableDictionary used to present a safe, non-immutable view to users.
        /// </summary>
        private ImmutableDictionary<ProjectId, ProjectAnalyzerInfo> _projectAnalyzerStateMap = ImmutableDictionary<ProjectId, ProjectAnalyzerInfo>.Empty;

        /// <summary>
        /// Guard around updating _projectAnalyzerStateMap. This is used in UpdateProjectStateSets to avoid
        /// duplicated calculations for a project during contentious calls.
        /// </summary>
        private readonly SemaphoreSlim _projectAnalyzerStateMapGuard = new(initialCount: 1);

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer"/>s for the given <see cref="Project"/>. 
        /// </summary>
        public async Task<ImmutableArray<DiagnosticAnalyzer>> GetOrCreateAnalyzersAsync(
            SolutionState solution, ProjectState project, CancellationToken cancellationToken)
        {
            var hostAnalyzerInfo = await GetOrCreateHostAnalyzerInfoAsync(solution, project, cancellationToken).ConfigureAwait(false);
            var projectAnalyzerInfo = await GetOrCreateProjectAnalyzerInfoAsync(solution, project, cancellationToken).ConfigureAwait(false);
            return hostAnalyzerInfo.OrderedAllAnalyzers.AddRange(projectAnalyzerInfo.Analyzers);
        }

        public async Task<HostAnalyzerInfo> GetOrCreateHostAnalyzerInfoAsync(
            SolutionState solution, ProjectState project, CancellationToken cancellationToken)
        {
            var projectAnalyzerInfo = await GetOrCreateProjectAnalyzerInfoAsync(solution, project, cancellationToken).ConfigureAwait(false);
            return GetOrCreateHostAnalyzerInfo(solution, project, projectAnalyzerInfo);
        }

        private HostAnalyzerInfo GetOrCreateHostAnalyzerInfo(
            SolutionState solution, ProjectState project, ProjectAnalyzerInfo projectAnalyzerInfo)
        {
            var key = new HostAnalyzerInfoKey(project.Language, project.HasSdkCodeStyleAnalyzers, solution.Analyzers.HostAnalyzerReferences);
            // Some Host Analyzers may need to be treated as Project Analyzers so that they do not have access to the
            // Host fallback options. These ids will be used when building up the Host and Project analyzer collections.
            var referenceIdsToRedirect = GetReferenceIdsToRedirectAsProjectAnalyzers(solution, project);
            var hostAnalyzerInfo = ImmutableInterlocked.GetOrAdd(ref _hostAnalyzerStateMap, key, CreateLanguageSpecificAnalyzerMap, (solution.Analyzers, referenceIdsToRedirect));
            return hostAnalyzerInfo.WithExcludedAnalyzers(projectAnalyzerInfo.SkippedAnalyzersInfo.SkippedAnalyzers);

            static HostAnalyzerInfo CreateLanguageSpecificAnalyzerMap(HostAnalyzerInfoKey arg, (HostDiagnosticAnalyzers HostAnalyzers, ImmutableHashSet<object> ReferenceIdsToRedirect) state)
            {
                var language = arg.Language;
                var analyzersPerReference = state.HostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(language);

                var (hostAnalyzerCollection, projectAnalyzerCollection) = GetAnalyzerCollections(analyzersPerReference, state.ReferenceIdsToRedirect);
                var (hostAnalyzers, allAnalyzers) = PartitionAnalyzers(projectAnalyzerCollection, hostAnalyzerCollection, includeWorkspacePlaceholderAnalyzers: true);

                return new HostAnalyzerInfo(hostAnalyzers, allAnalyzers);
            }

            static (ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> HostAnalyzerCollection, ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> ProjectAnalyzerCollection) GetAnalyzerCollections(
                ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> analyzersPerReference,
                ImmutableHashSet<object> referenceIdsToRedirectAsProjectAnalyzers)
            {
                if (referenceIdsToRedirectAsProjectAnalyzers.IsEmpty)
                {
                    return ([.. analyzersPerReference.Values], []);
                }

                var hostAnalyzerCollection = ArrayBuilder<ImmutableArray<DiagnosticAnalyzer>>.GetInstance();
                var projectAnalyzerCollection = ArrayBuilder<ImmutableArray<DiagnosticAnalyzer>>.GetInstance();

                foreach (var (referenceId, analyzers) in analyzersPerReference)
                {
                    if (referenceIdsToRedirectAsProjectAnalyzers.Contains(referenceId))
                    {
                        projectAnalyzerCollection.Add(analyzers);
                    }
                    else
                    {
                        hostAnalyzerCollection.Add(analyzers);
                    }
                }

                return (hostAnalyzerCollection.ToImmutableAndFree(), projectAnalyzerCollection.ToImmutableAndFree());
            }
        }

        private static (ImmutableHashSet<DiagnosticAnalyzer> hostAnalyzers, ImmutableHashSet<DiagnosticAnalyzer> allAnalyzers) PartitionAnalyzers(
            ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> projectAnalyzerCollection,
            ImmutableArray<ImmutableArray<DiagnosticAnalyzer>> hostAnalyzerCollection,
            bool includeWorkspacePlaceholderAnalyzers)
        {
            using var _1 = PooledHashSet<DiagnosticAnalyzer>.GetInstance(out var hostAnalyzers);
            using var _2 = PooledHashSet<DiagnosticAnalyzer>.GetInstance(out var allAnalyzers);

            if (includeWorkspacePlaceholderAnalyzers)
            {
                hostAnalyzers.Add(FileContentLoadAnalyzer.Instance);
                hostAnalyzers.Add(GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
                allAnalyzers.Add(FileContentLoadAnalyzer.Instance);
                allAnalyzers.Add(GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
            }

            foreach (var analyzers in projectAnalyzerCollection)
            {
                foreach (var analyzer in analyzers)
                {
                    Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance && analyzer != GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
                    allAnalyzers.Add(analyzer);
                }
            }

            foreach (var analyzers in hostAnalyzerCollection)
            {
                foreach (var analyzer in analyzers)
                {
                    Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance && analyzer != GeneratorDiagnosticsPlaceholderAnalyzer.Instance);
                    allAnalyzers.Add(analyzer);
                    hostAnalyzers.Add(analyzer);
                }
            }

            return (hostAnalyzers.ToImmutableHashSet(), allAnalyzers.ToImmutableHashSet());
        }

        private static ImmutableHashSet<object> GetReferenceIdsToRedirectAsProjectAnalyzers(
            SolutionState solution, ProjectState project)
        {
            if (project.HasSdkCodeStyleAnalyzers)
            {
                // When a project uses CodeStyle analyzers added by the SDK, we remove them in favor of the
                // Features analyzers. We need to then treat the Features analyzers as Project analyzers so
                // they do not get access to the Host fallback options.
                return GetFeaturesAnalyzerReferenceIds(solution.Analyzers);
            }

            return [];

            static ImmutableHashSet<object> GetFeaturesAnalyzerReferenceIds(HostDiagnosticAnalyzers hostAnalyzers)
            {
                var builder = ImmutableHashSet.CreateBuilder<object>();

                foreach (var analyzerReference in hostAnalyzers.HostAnalyzerReferences)
                {
                    if (analyzerReference.IsFeaturesAnalyzer())
                        builder.Add(analyzerReference.Id);
                }

                return builder.ToImmutable();
            }
        }

        private readonly record struct HostAnalyzerInfoKey(
            string Language, bool HasSdkCodeStyleAnalyzers, IReadOnlyList<AnalyzerReference> AnalyzerReferences);
    }
}
