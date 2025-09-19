// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal abstract class ProjectFileInfoLoader(
    DiagnosticReporter diagnosticReporter,
    ProjectFileExtensionRegistry projectFileExtensionRegistry,
    ProjectLoadOperationRunner operationRunner) : IAsyncDisposable
{
    private readonly DiagnosticReporter _diagnosticReporter = diagnosticReporter;
    private readonly ProjectFileExtensionRegistry _projectFileExtensionRegistry = projectFileExtensionRegistry;
    private readonly ProjectLoadOperationRunner _operationRunner = operationRunner;

    public abstract ValueTask DisposeAsync();

    public abstract Task<ImmutableArray<ProjectFileInfo>> LoadProjectFileInfosAsync(
        string projectFilePath,
        DiagnosticReportingOptions reportingOptions,
        CancellationToken cancellationToken);

    public abstract Task<string?> TryGetProjectOutputPathAsync(string projectFilePath, CancellationToken cancellationToken);

    protected bool TryGetLanguageNameFromProjectPath(
        string projectFilePath,
        DiagnosticReportingOptions reportingOptions,
        [NotNullWhen(true)] out string? languageName)
        => _projectFileExtensionRegistry.TryGetLanguageNameFromProjectPath(projectFilePath, reportingOptions.OnLoaderFailure, out languageName);

    protected Task<TResult> DoOperationAndReportProgressAsync<TResult>(
        ProjectLoadOperation operation,
        string? projectFilePath,
        string? targetFramework,
        Func<Task<TResult>> doFunc)
        => _operationRunner.DoOperationAndReportProgressAsync(operation, projectFilePath, targetFramework, doFunc);

    protected void ReportDiagnostic(WorkspaceDiagnostic diagnostic)
        => _diagnosticReporter.Report(diagnostic);

    protected void ReportDiagnostics(IEnumerable<DiagnosticLogItem> diagnostics)
        => _diagnosticReporter.Report(diagnostics);
}
