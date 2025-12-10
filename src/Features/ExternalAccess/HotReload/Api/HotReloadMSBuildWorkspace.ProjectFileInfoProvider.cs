// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias BuildHost;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis.ExternalAccess.HotReload.Internal;
using Microsoft.CodeAnalysis.MSBuild;

using MSBuildHost = BuildHost::Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;

internal sealed partial class HotReloadMSBuildWorkspace
{
    private sealed class ProjectFileInfoProvider(Func<string, ImmutableArray<ProjectInstance>> getProjectInstances, ProjectFileExtensionRegistry projectFileExtensionRegistry)
        : IProjectFileInfoProvider
    {
        public Task<ImmutableArray<ProjectFileInfo>> LoadProjectFileInfosAsync(string projectPath, DiagnosticReportingOptions reportingOptions, CancellationToken cancellationToken)
        {
            var instances = getProjectInstances(projectPath);

            if (instances.IsEmpty ||
                !projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out var languageName))
            {
                return Task.FromResult(ImmutableArray<ProjectFileInfo>.Empty);
            }

            return Task.FromResult(instances.SelectAsArray(instance =>
            {
                var reader = new MSBuildHost.ProjectInstanceReader(
                    MSBuildHost.ProjectCommandLineProvider.Create(languageName),
                    instance,
                    project: null);

                return reader.CreateProjectFileInfo().Convert();
            }));
        }

        public Task<ImmutableArray<string>> GetProjectOutputPathsAsync(string projectPath, CancellationToken cancellationToken)
            => Task.FromResult(
                getProjectInstances(projectPath).SelectAsArray(static instance => instance.GetPropertyValue(MSBuildHost.PropertyNames.TargetPath)));
    }
}
