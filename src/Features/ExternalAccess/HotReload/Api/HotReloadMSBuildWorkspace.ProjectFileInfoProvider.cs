// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;

using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;

internal sealed partial class HotReloadMSBuildWorkspace
{
    private sealed class ProjectFileInfoProvider(
        Func<string, (ImmutableArray<MSB.Execution.ProjectInstance> instances, MSB.Evaluation.Project? project)> getBuildProjects,
        ProjectFileExtensionRegistry projectFileExtensionRegistry)
        : IProjectFileInfoProvider
    {
        public Task<ProjectFileInfo[]> LoadProjectFileInfosAsync(string projectPath, DiagnosticReportingOptions reportingOptions, CancellationToken cancellationToken)
        {
            var (instances, project) = getBuildProjects(projectPath);

            if (instances.IsEmpty ||
                !projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out var languageName))
            {
                return Task.FromResult(Array.Empty<ProjectFileInfo>());
            }

            return Task.FromResult(instances.SelectAsArray(instance =>
            {
                var reader = new ProjectInstanceReader(
                    ProjectCommandLineProvider.Create(languageName),
                    instance,
                    project);

                return reader.CreateProjectFileInfo();
            }).ToArray());
        }

        public Task<string[]> GetProjectOutputPathsAsync(string projectPath, CancellationToken cancellationToken)
            => Task.FromResult(
                getBuildProjects(projectPath).instances.SelectAsArray(static instance => instance.GetPropertyValue(PropertyNames.TargetPath)).ToArray());
    }
}
