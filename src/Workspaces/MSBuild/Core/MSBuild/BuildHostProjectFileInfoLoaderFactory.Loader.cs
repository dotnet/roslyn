// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
///  Creates <see cref="ProjectFileInfoLoader"/> instances that use build host processes to load projects.
/// </summary>
internal sealed partial class BuildHostProjectFileInfoLoaderFactory
{
    private sealed class Loader(
        ImmutableDictionary<string, string> properties,
        ProjectFileExtensionRegistry projectFileExtensionRegistry,
        ProjectLoadOperationRunner operationRunner,
        DiagnosticReporter diagnosticReporter,
        ILoggerFactory loggerFactory)
        : ProjectFileInfoLoader(diagnosticReporter, projectFileExtensionRegistry, operationRunner)
    {
        private readonly BuildHostProcessManager _buildHostProcessManager = new(properties, loggerFactory: loggerFactory);

        public override ValueTask DisposeAsync()
            => _buildHostProcessManager.DisposeAsync();

        public override async Task<ImmutableArray<ProjectFileInfo>> LoadProjectFileInfosAsync(
            string projectFilePath,
            DiagnosticReportingOptions reportingOptions,
            CancellationToken cancellationToken)
        {
            if (!TryGetLanguageNameFromProjectPath(projectFilePath, reportingOptions, out var languageName))
            {
                return []; // Failure should already be reported.
            }

            var preferredBuildHostKind = BuildHostProcessManager.GetKindForProject(projectFilePath);
            var (buildHost, actualBuildHostKind) = await _buildHostProcessManager
                .GetBuildHostWithFallbackAsync(preferredBuildHostKind, projectFilePath, cancellationToken)
                .ConfigureAwait(false);

            var projectFile = await DoOperationAndReportProgressAsync(
                    ProjectLoadOperation.Evaluate,
                    projectFilePath,
                    targetFramework: null,
                    () => buildHost.LoadProjectFileAsync(projectFilePath, languageName, cancellationToken))
                .ConfigureAwait(false);

            // If there were any failures during load, we won't be able to build the project. So, bail early with an empty project.
            var diagnosticItems = await projectFile.GetDiagnosticLogItemsAsync(cancellationToken).ConfigureAwait(false);
            if (diagnosticItems.Any(static d => d.Kind == DiagnosticLogItemKind.Error))
            {
                ReportDiagnostics(diagnosticItems);

                return [ProjectFileInfo.CreateEmpty(languageName, projectFilePath)];
            }

            var projectFileInfos = await DoOperationAndReportProgressAsync(
                    ProjectLoadOperation.Build,
                    projectFilePath,
                    targetFramework: null,
                    () => projectFile.GetProjectFileInfosAsync(cancellationToken))
                .ConfigureAwait(false);

            var results = ImmutableArray.CreateBuilder<ProjectFileInfo>(projectFileInfos.Length);

            foreach (var projectFileInfo in projectFileInfos)
            {
                // Note: any diagnostics would have been logged to the original project file's log.

                results.Add(projectFileInfo);
            }

            // We'll go check for any further diagnostics and report them
            diagnosticItems = await projectFile.GetDiagnosticLogItemsAsync(cancellationToken).ConfigureAwait(false);
            ReportDiagnostics(diagnosticItems);

            return results.MoveToImmutable();
        }

        public override async Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken)
        {
            var buildHost = await _buildHostProcessManager.GetBuildHostWithFallbackAsync(projectFilePath, cancellationToken).ConfigureAwait(false);
            return await buildHost.TryGetProjectOutputPathAsync(projectFilePath, cancellationToken).ConfigureAwait(false);
        }
    }
}
