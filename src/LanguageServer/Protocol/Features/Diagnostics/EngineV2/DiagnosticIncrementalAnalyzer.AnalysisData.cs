// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2;

internal partial class DiagnosticIncrementalAnalyzer
{
    /// <summary>
    /// Data holder for all diagnostics for a project for an analyzer
    /// </summary>
    private readonly struct ProjectAnalysisData(
        ProjectId projectId,
        VersionStamp version,
        ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
    {
        /// <summary>
        /// ProjectId of this data
        /// </summary>
        public readonly ProjectId ProjectId = projectId;

        /// <summary>
        /// Version of the Items
        /// </summary>
        public readonly VersionStamp Version = version;

        /// <summary>
        /// Current data that matches the version
        /// </summary>
        public readonly ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> Result = result;

        public DiagnosticAnalysisResult GetResult(DiagnosticAnalyzer analyzer)
            => GetResultOrEmpty(Result, analyzer, ProjectId, Version);

        public bool TryGetResult(DiagnosticAnalyzer analyzer, out DiagnosticAnalysisResult result)
            => Result.TryGetValue(analyzer, out result);

        public static async Task<ProjectAnalysisData> CreateAsync(Project project, ImmutableArray<StateSet> stateSets, CancellationToken cancellationToken)
        {
            VersionStamp? version = null;

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();
            foreach (var stateSet in stateSets)
            {
                var state = stateSet.GetOrCreateProjectState(project.Id);
                var result = await state.GetAnalysisDataAsync(project, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(project.Id == result.ProjectId);

                if (!version.HasValue)
                {
                    version = result.Version;
                }
                else if (version.Value != VersionStamp.Default && version.Value != result.Version)
                {
                    // if not all version is same, set version as default.
                    // this can happen at the initial data loading or
                    // when document is closed and we put active file state to project state
                    version = VersionStamp.Default;
                }

                builder.Add(stateSet.Analyzer, result);
            }

            if (!version.HasValue)
            {
                // there is no saved data to return.
                return new ProjectAnalysisData(project.Id, VersionStamp.Default, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty);
            }

            return new ProjectAnalysisData(project.Id, version.Value, builder.ToImmutable());
        }
    }
}
