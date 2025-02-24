// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        private partial class StateManager
        {
            private readonly struct ProjectAnalyzerInfo
            {
                public static readonly ProjectAnalyzerInfo Default = new(
                    analyzerReferences: [],
                    analyzers: [],
                    SkippedHostAnalyzersInfo.Empty);

                public readonly IReadOnlyList<AnalyzerReference> AnalyzerReferences;

                public readonly ImmutableHashSet<DiagnosticAnalyzer> Analyzers;

                public readonly SkippedHostAnalyzersInfo SkippedAnalyzersInfo;

                internal ProjectAnalyzerInfo(
                    IReadOnlyList<AnalyzerReference> analyzerReferences,
                    ImmutableHashSet<DiagnosticAnalyzer> analyzers,
                    SkippedHostAnalyzersInfo skippedAnalyzersInfo)
                {
                    AnalyzerReferences = analyzerReferences;
                    Analyzers = analyzers;
                    SkippedAnalyzersInfo = skippedAnalyzersInfo;
                }
            }

            private ProjectAnalyzerInfo? TryGetProjectAnalyzerInfo(ProjectState project)
            {
                // check if the analyzer references have changed since the last time we updated the map:
                // No need to use _projectAnalyzerStateMapGuard during reads of _projectAnalyzerStateMap
                if (_projectAnalyzerStateMap.TryGetValue(project.Id, out var entry) &&
                    entry.AnalyzerReferences.SequenceEqual(project.AnalyzerReferences))
                {
                    return entry;
                }

                return null;
            }

            private async Task<ProjectAnalyzerInfo> GetOrCreateProjectAnalyzerInfoAsync(SolutionState solution, ProjectState project, CancellationToken cancellationToken)
                => TryGetProjectAnalyzerInfo(project) ?? await UpdateProjectAnalyzerInfoAsync(solution, project, cancellationToken).ConfigureAwait(false);

            private ProjectAnalyzerInfo CreateProjectAnalyzerInfo(SolutionState solution, ProjectState project)
            {
                if (project.AnalyzerReferences.Count == 0)
                {
                    return ProjectAnalyzerInfo.Default;
                }

                var solutionAnalyzers = solution.Analyzers;
                var analyzersPerReference = solutionAnalyzers.CreateProjectDiagnosticAnalyzersPerReference(project);
                if (analyzersPerReference.Count == 0)
                {
                    return ProjectAnalyzerInfo.Default;
                }

                var (newHostAnalyzers, newAllAnalyzers) = PartitionAnalyzers(
                    analyzersPerReference.Values, hostAnalyzerCollection: [], includeWorkspacePlaceholderAnalyzers: false);

                // We passed an empty array for 'hostAnalyzeCollection' above, and we specifically asked to not include
                // workspace placeholder analyzers.  So we should never get host analyzers back here.
                Contract.ThrowIfTrue(newHostAnalyzers.Count > 0);

                var skippedAnalyzersInfo = solutionAnalyzers.GetSkippedAnalyzersInfo(project, _analyzerInfoCache);
                return new ProjectAnalyzerInfo(project.AnalyzerReferences, newAllAnalyzers, skippedAnalyzersInfo);
            }

            /// <summary>
            /// Updates the map to the given project snapshot.
            /// </summary>
            private async Task<ProjectAnalyzerInfo> UpdateProjectAnalyzerInfoAsync(
                SolutionState solution, ProjectState project, CancellationToken cancellationToken)
            {
                // This code is called concurrently for a project, so the guard prevents duplicated effort calculating StateSets.
                using (await _projectAnalyzerStateMapGuard.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    var projectAnalyzerInfo = TryGetProjectAnalyzerInfo(project);

                    if (projectAnalyzerInfo == null)
                    {
                        projectAnalyzerInfo = CreateProjectAnalyzerInfo(solution, project);

                        // update cache. 
                        _projectAnalyzerStateMap = _projectAnalyzerStateMap.SetItem(project.Id, projectAnalyzerInfo.Value);
                    }

                    return projectAnalyzerInfo.Value;
                }
            }
        }
    }
}
