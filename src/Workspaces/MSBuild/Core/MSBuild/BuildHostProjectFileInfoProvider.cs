// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildHostProjectFileInfoProvider(
    BuildHostProcessManager buildHostProcessManager,
    ProjectFileExtensionRegistry projectFileExtensionRegistry,
    DiagnosticReporter diagnosticReporter,
    IProgress<ProjectLoadProgress>? progress) : IProjectFileInfoProvider
{
    public async Task<ProjectFileInfo[]> LoadProjectFileInfosAsync(string projectPath, DiagnosticReportingOptions reportingOptions, CancellationToken cancellationToken)
    {
        if (!projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectPath, reportingOptions.OnLoaderFailure, out var languageName))
        {
            return []; // Failure should already be reported.
        }

        var preferredBuildHostKind = BuildHostProcessManager.GetKindForProject(projectPath);
        var (buildHost, _) = await buildHostProcessManager.GetBuildHostWithFallbackAsync(preferredBuildHostKind, projectPath, cancellationToken).ConfigureAwait(false);
        var projectFile = await progress.DoOperationAndReportProgressAsync(
            ProjectLoadOperation.Evaluate,
            projectPath,
            targetFramework: null,
            () => buildHost.LoadProjectFileAsync(projectPath, languageName, cancellationToken)
        ).ConfigureAwait(false);

        // If there were any failures during load, we won't be able to build the project. So, bail early with an empty project.
        var diagnosticItems = await projectFile.GetDiagnosticLogItemsAsync(cancellationToken).ConfigureAwait(false);
        if (diagnosticItems.Any(d => d.Kind == DiagnosticLogItemKind.Error))
        {
            diagnosticReporter.Report(diagnosticItems);

            return [ProjectFileInfo.CreateEmpty(languageName, projectPath)];
        }

        var projectFileInfos = await progress.DoOperationAndReportProgressAsync(
            ProjectLoadOperation.Build,
            projectPath,
            targetFramework: null,
            () => projectFile.GetProjectFileInfosAsync(cancellationToken)
        ).ConfigureAwait(false);

        // We'll go check for any further diagnostics and report them
        diagnosticItems = await projectFile.GetDiagnosticLogItemsAsync(cancellationToken).ConfigureAwait(false);
        diagnosticReporter.Report(diagnosticItems);

        return projectFileInfos;
    }

    public async Task<string[]> GetProjectOutputPathsAsync(string projectPath, CancellationToken cancellationToken)
    {
        // TODO: Should return multiple output paths for multi-targeted projects.
        // https://github.com/dotnet/roslyn/issues/81589

        var buildHost = await buildHostProcessManager.GetBuildHostWithFallbackAsync(projectPath, cancellationToken).ConfigureAwait(false);
        var path = await buildHost.TryGetProjectOutputPathAsync(projectPath, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(path) ? [] : [path];
    }
}
