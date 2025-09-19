// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

internal static class WatchMSBuildProjectLoader
{
    public static MSBuildProjectLoader Create(
        Workspace workspace,
        IWatchProjectFileInfoLoaderFactory loaderFactory)
        => Create(workspace, properties: null, loaderFactory);

    public static MSBuildProjectLoader Create(
        Workspace workspace,
        ImmutableDictionary<string, string>? properties,
        IWatchProjectFileInfoLoaderFactory loaderFactory)
        => new(workspace, properties, new WatchProjectFileInfoLoaderFactory(loaderFactory));

    private sealed class WatchProjectFileInfoLoaderFactory(IWatchProjectFileInfoLoaderFactory loaderFactory) : IProjectFileInfoLoaderFactory
    {
        public ProjectFileInfoLoader Create(
            ImmutableDictionary<string, string> properties,
            ProjectFileExtensionRegistry projectFileExtensionRegistry,
            ProjectLoadOperationRunner operationRunner,
            DiagnosticReporter diagnosticReporter,
            ILoggerFactory loggerFactory)
            => new WatchProjectFileInfoLoader(diagnosticReporter, projectFileExtensionRegistry, operationRunner, loaderFactory.Create(properties));
    }

    private sealed class WatchProjectFileInfoLoader(
        DiagnosticReporter diagnosticReporter,
        ProjectFileExtensionRegistry projectFileExtensionRegistry,
        ProjectLoadOperationRunner operationRunner,
        IWatchProjectFileInfoLoader loader)
        : ProjectFileInfoLoader(diagnosticReporter, projectFileExtensionRegistry, operationRunner)
    {
        private readonly IWatchProjectFileInfoLoader _loader = loader;

        public override ValueTask DisposeAsync()
            => (_loader as IAsyncDisposable)?.DisposeAsync() ?? default;

        public override async Task<ImmutableArray<ProjectFileInfo>> LoadProjectFileInfosAsync(
            string projectFilePath,
            DiagnosticReportingOptions reportingOptions,
            CancellationToken cancellationToken)
        {
            if (!TryGetLanguageNameFromProjectPath(projectFilePath, reportingOptions, out var languageName))
            {
                return [];
            }

            var result = await _loader
                .LoadProjectFileInfosAsync(projectFilePath, languageName, cancellationToken)
                .ConfigureAwait(false);

            ReportDiagnostics(result.DiagnosticItems.SelectAsArray(static x => x.UnderlyingObject));

            return result.ProjectFileInfos.SelectAsArray(static x => x.UnderlyingObject);
        }

        public override Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken)
            => _loader.TryGetProjectOutputPathAsync(projectFilePath, cancellationToken);
    }
}
