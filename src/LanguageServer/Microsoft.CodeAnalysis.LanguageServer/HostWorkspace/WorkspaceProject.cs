// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class WorkspaceProject : IWorkspaceProject
{
    private readonly ProjectSystemProject _project;
    private readonly ProjectSystemProjectOptionsProcessor _optionsProcessor;
    private readonly ProjectTargetFrameworkManager _targetFrameworkManager;
    private readonly ILogger _logger;

    public WorkspaceProject(ProjectSystemProject project, SolutionServices solutionServices, ProjectTargetFrameworkManager targetFrameworkManager, ILoggerFactory logger)
    {
        _project = project;
        _optionsProcessor = new ProjectSystemProjectOptionsProcessor(_project, solutionServices);
        _targetFrameworkManager = targetFrameworkManager;
        _logger = logger.CreateLogger<WorkspaceProject>();
    }

    [Obsolete($"Call the {nameof(AddAdditionalFilesAsync)} overload that takes {nameof(SourceFileInfo)}.")]
    public Task AddAdditionalFilesAsync(IReadOnlyList<string> additionalFilePaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var additionalFilePath in additionalFilePaths)
                _project.AddAdditionalFile(additionalFilePath);
        }, cancellationToken);
    }

    public Task AddAdditionalFilesAsync(IReadOnlyList<SourceFileInfo> additionalFiles, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var additionalFile in additionalFiles)
                _project.AddAdditionalFile(additionalFile.FilePath, folders: [.. additionalFile.FolderNames]);
        }, cancellationToken);
    }

    public Task AddAnalyzerConfigFilesAsync(IReadOnlyList<string> analyzerConfigPaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var analyzerConfigPath in analyzerConfigPaths)
                _project.AddAnalyzerConfigFile(analyzerConfigPath);
        }, cancellationToken);
    }

    public Task AddAnalyzerReferencesAsync(IReadOnlyList<string> analyzerPaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var analyzerPath in analyzerPaths)
                _project.AddAnalyzerReference(analyzerPath);
        }, cancellationToken);
    }

    public Task AddDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var dynamicFilePath in dynamicFilePaths)
                _project.AddDynamicSourceFile(dynamicFilePath, folders: []);
        }, cancellationToken);
    }

    public Task AddMetadataReferencesAsync(IReadOnlyList<MetadataReferenceInfo> metadataReferences, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var metadataReference in metadataReferences)
                _project.AddMetadataReference(metadataReference.FilePath, metadataReference.CreateProperties());
        }, cancellationToken);
    }

    public Task AddSourceFilesAsync(IReadOnlyList<SourceFileInfo> sourceFiles, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var sourceFile in sourceFiles)
                _project.AddSourceFile(sourceFile.FilePath, folders: [.. sourceFile.FolderNames]);
        }, cancellationToken);
    }

    public void Dispose()
    {
        _project.RemoveFromWorkspace();
    }

    public Task RemoveAdditionalFilesAsync(IReadOnlyList<string> additionalFilePaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var additionalFilePath in additionalFilePaths)
                _project.RemoveAdditionalFile(additionalFilePath);
        }, cancellationToken);
    }

    public Task RemoveAnalyzerConfigFilesAsync(IReadOnlyList<string> analyzerConfigPaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var analyzerConfigPath in analyzerConfigPaths)
                _project.RemoveAnalyzerConfigFile(analyzerConfigPath);
        }, cancellationToken);
    }

    public Task RemoveAnalyzerReferencesAsync(IReadOnlyList<string> analyzerPaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var analyzerPath in analyzerPaths)
                _project.RemoveAnalyzerReference(analyzerPath);
        }, cancellationToken);
    }

    public Task RemoveDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var dynamicFilePath in dynamicFilePaths)
                _project.RemoveDynamicSourceFile(dynamicFilePath);
        }, cancellationToken);
    }

    public Task RemoveMetadataReferencesAsync(IReadOnlyList<MetadataReferenceInfo> metadataReferences, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var metadataReference in metadataReferences)
                _project.RemoveMetadataReference(metadataReference.FilePath, metadataReference.CreateProperties());
        }, cancellationToken);
    }

    public Task RemoveSourceFilesAsync(IReadOnlyList<string> sourceFiles, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            foreach (var sourceFile in sourceFiles)
                _project.RemoveSourceFile(sourceFile);
        }, cancellationToken);
    }

    public Task SetBuildSystemPropertiesAsync(IReadOnlyDictionary<string, string> properties, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() =>
        {
            string? fileDirectory = null;

            foreach (var (name, value) in properties)
            {
                var valueOrNull = string.IsNullOrEmpty(value) ? null : value;

                switch (name)
                {
                    case "AssemblyName": _project.AssemblyName = value; break;
                    case "IntermediateAssembly": _project.CompilationOutputAssemblyFilePath = GetFullyQualifiedPath(valueOrNull); break;
                    case "MaxSupportedLangVersion": _project.MaxLangVersion = value; break;
                    case "RootNamespace": _project.DefaultNamespace = valueOrNull; break;
                    case "RunAnalyzers": _project.RunAnalyzers = bool.Parse(valueOrNull ?? bool.TrueString); break;
                    case "RunAnalyzersDuringLiveAnalysis": _project.RunAnalyzersDuringLiveAnalysis = bool.Parse(valueOrNull ?? bool.TrueString); break;
                    case "TargetPath": _project.OutputFilePath = GetFullyQualifiedPath(valueOrNull); break;
                    case "TargetRefPath": _project.OutputRefFilePath = GetFullyQualifiedPath(valueOrNull); break;
                    case "CompilerGeneratedFilesOutputPath": _project.GeneratedFilesOutputDirectory = GetFullyQualifiedPath(valueOrNull); break;
                    case "TargetFrameworkIdentifier": _targetFrameworkManager.UpdateIdentifierForProject(_project.Id, valueOrNull); break;
                }
            }

            string? GetFullyQualifiedPath(string? propertyValue)
            {
                Contract.ThrowIfNull(_project.FilePath, "We don't have a project path at this point.");

                // Path.Combine doesn't check if the first parameter is an absolute path to a file instead of a directory,
                // so make sure to use the directory from the _project.FilePath. If the propertyValue is an absolute
                // path that will still be used, but if it's a relative path it will correctly construct the full path.
                fileDirectory ??= Path.GetDirectoryName(_project.FilePath);
                Contract.ThrowIfNull(fileDirectory);

                if (propertyValue is not null)
                    return Path.Combine(fileDirectory, propertyValue);
                else
                    return null;
            }
        }, cancellationToken);
    }

    public Task SetCommandLineArgumentsAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() => _optionsProcessor.SetCommandLine([.. arguments]), cancellationToken);
    }

    public Task SetDisplayNameAsync(string displayName, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() => _project.DisplayName = displayName, cancellationToken);
    }

    public Task SetProjectHasAllInformationAsync(bool hasAllInformation, CancellationToken cancellationToken)
    {
        return RunAndReportNFWAsync(() => _project.HasAllInformation = hasAllInformation, cancellationToken);
    }

    public async Task<IWorkspaceProjectBatch> StartBatchAsync(CancellationToken cancellationToken)
    {
        // Create a batch scope, just so we have asynchronous closing and application of the batch.
        var disposableBatchScope = await _project.CreateBatchScopeAsync(cancellationToken).ConfigureAwait(false);
        await using var _ = disposableBatchScope.ConfigureAwait(false);

        return new WorkspaceProjectBatch(_project.CreateBatchScope(), _logger);
    }

    private async Task RunAndReportNFWAsync(Action action, CancellationToken cancellationToken)
    {
        try
        {
            var disposableBatchScope = await _project.CreateBatchScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var _ = disposableBatchScope.ConfigureAwait(false);
            action();
        }
        catch (Exception e) when (LanguageServerFatalError.ReportAndLogAndPropagate(e, _logger, "Error applying project system update."))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private sealed class WorkspaceProjectBatch : IWorkspaceProjectBatch
    {
        private IAsyncDisposable? _batch;
        private readonly ILogger _logger;

        public WorkspaceProjectBatch(IAsyncDisposable batch, ILogger logger)
        {
            _batch = batch;
            _logger = logger;
        }

        public async Task ApplyAsync(CancellationToken cancellationToken)
        {
            if (_batch == null)
                throw new InvalidOperationException("The batch has already been applied.");

            try
            {
                await _batch.DisposeAsync().ConfigureAwait(false);
                _batch = null;
            }
            catch (Exception e) when (LanguageServerFatalError.ReportAndLogAndPropagate(e, _logger, "Error applying project system batch."))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public void Dispose()
        {
        }
    }
}
