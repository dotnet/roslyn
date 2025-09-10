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

internal sealed partial class DiagnosticAnalyzerService
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

    private ProjectAnalyzerInfo GetOrCreateProjectAnalyzerInfo_OnlyCallInProcess(Project project)
    {
        return ImmutableInterlocked.GetOrAdd(
            ref _projectAnalyzerStateMap,
            (project.Id, project.AnalyzerReferences),
            static (_, tuple) =>
            {
                var (@this, project) = tuple;
                return @this.CreateProjectAnalyzerInfo(project);
            },
            (this, project));
    }

    private ProjectAnalyzerInfo CreateProjectAnalyzerInfo(Project project)
    {
        if (project.AnalyzerReferences.Count == 0)
            return ProjectAnalyzerInfo.Default;

        var solutionAnalyzers = project.Solution.SolutionState.Analyzers;
        var analyzersPerReference = solutionAnalyzers.CreateProjectDiagnosticAnalyzersPerReference(project.State);
        if (analyzersPerReference.Count == 0)
            return ProjectAnalyzerInfo.Default;

        var (newHostAnalyzers, newAllAnalyzers) = PartitionAnalyzers(
            [.. analyzersPerReference.Values], hostAnalyzerCollection: [], includeWorkspacePlaceholderAnalyzers: false);

        // We passed an empty array for 'hostAnalyzeCollection' above, and we specifically asked to not include
        // workspace placeholder analyzers.  So we should never get host analyzers back here.
        Contract.ThrowIfTrue(newHostAnalyzers.Count > 0);

        var skippedAnalyzersInfo = solutionAnalyzers.GetSkippedAnalyzersInfo(project.State, _analyzerInfoCache);
        return new ProjectAnalyzerInfo(project.AnalyzerReferences, newAllAnalyzers, skippedAnalyzersInfo);
    }
}
