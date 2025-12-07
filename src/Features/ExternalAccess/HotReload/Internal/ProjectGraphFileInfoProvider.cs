// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias BuildHost;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Graph;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Internal;

internal sealed class ProjectGraphFileInfoProvider(ProjectGraph graph, ProjectFileExtensionRegistry projectFileExtensionRegistry) : IProjectFileInfoProvider
{
    private readonly ImmutableDictionary<string, ImmutableArray<ProjectGraphNode>> _projectFilePathToNodeMap = graph.ProjectNodes
        .GroupBy(static node => node.ProjectInstance.FullPath)
            .ToImmutableDictionary(
                keySelector: static group => group.Key,
                elementSelector: static group => group.ToImmutableArray());

    public Task<ImmutableArray<ProjectFileInfo>> LoadProjectFileInfosAsync(string projectPath, DiagnosticReportingOptions reportingOptions, CancellationToken cancellationToken)
    {
        if (!_projectFilePathToNodeMap.TryGetValue(projectPath, out var nodes) ||
            !projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out var languageName))
        {
            return Task.FromResult(ImmutableArray<ProjectFileInfo>.Empty);
        }

        return Task.FromResult(nodes.SelectAsArray(node =>
        {
            var projectFile = BuildHost::Microsoft.CodeAnalysis.MSBuild.ProjectFile.Create(project: null, languageName);
            return projectFile.CreateProjectFileInfo(node.ProjectInstance).Convert();
        }));
    }

    public Task<string?> TryGetProjectOutputPathAsync(string projectPath, CancellationToken cancellationToken)
        => Task.FromResult(
            _projectFilePathToNodeMap.TryGetValue(projectPath, out var nodes)
                ? nodes.First().ProjectInstance.GetPropertyValue(BuildHost::Microsoft.CodeAnalysis.MSBuild.PropertyNames.TargetPath)
                : null);
}
