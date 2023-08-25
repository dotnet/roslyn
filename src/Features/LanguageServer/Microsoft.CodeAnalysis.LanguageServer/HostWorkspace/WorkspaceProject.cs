﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal class WorkspaceProject : IWorkspaceProject
{
    private readonly ProjectSystemProject _project;
    private readonly ProjectSystemProjectOptionsProcessor _optionsProcessor;
    private readonly ProjectTargetFrameworkManager _targetFrameworkManager;

    public WorkspaceProject(ProjectSystemProject project, SolutionServices solutionServices, ProjectTargetFrameworkManager targetFrameworkManager)
    {
        _project = project;
        _optionsProcessor = new ProjectSystemProjectOptionsProcessor(_project, solutionServices);
        _targetFrameworkManager = targetFrameworkManager;
    }

    public async Task AddAdditionalFilesAsync(IReadOnlyList<string> additionalFilePaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var additionalFilePath in additionalFilePaths)
            _project.AddAdditionalFile(additionalFilePath);
    }

    public async Task AddAnalyzerConfigFilesAsync(IReadOnlyList<string> analyzerConfigPaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var analyzerConfigPath in analyzerConfigPaths)
            _project.AddAnalyzerConfigFile(analyzerConfigPath);
    }

    public async Task AddAnalyzerReferencesAsync(IReadOnlyList<string> analyzerPaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var analyzerPath in analyzerPaths)
            _project.AddAnalyzerReference(analyzerPath);
    }

    public async Task AddDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var dynamicFilePath in dynamicFilePaths)
            _project.AddDynamicSourceFile(dynamicFilePath, folders: ImmutableArray<string>.Empty);
    }

    public async Task AddMetadataReferencesAsync(IReadOnlyList<MetadataReferenceInfo> metadataReferences, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var metadataReference in metadataReferences)
            _project.AddMetadataReference(metadataReference.FilePath, metadataReference.CreateProperties());
    }

    public async Task AddSourceFilesAsync(IReadOnlyList<SourceFileInfo> sourceFiles, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var sourceFile in sourceFiles)
            _project.AddSourceFile(sourceFile.FilePath, folders: sourceFile.FolderNames.ToImmutableArray());
    }

    public void Dispose()
    {
        _project.RemoveFromWorkspace();
    }

    public async Task RemoveAdditionalFilesAsync(IReadOnlyList<string> additionalFilePaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var additionalFilePath in additionalFilePaths)
            _project.RemoveAdditionalFile(additionalFilePath);
    }

    public async Task RemoveAnalyzerConfigFilesAsync(IReadOnlyList<string> analyzerConfigPaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var analyzerConfigPath in analyzerConfigPaths)
            _project.RemoveAnalyzerConfigFile(analyzerConfigPath);
    }

    public async Task RemoveAnalyzerReferencesAsync(IReadOnlyList<string> analyzerPaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var analyzerPath in analyzerPaths)
            _project.RemoveAnalyzerReference(analyzerPath);
    }

    public async Task RemoveDynamicFilesAsync(IReadOnlyList<string> dynamicFilePaths, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var dynamicFilePath in dynamicFilePaths)
            _project.RemoveDynamicSourceFile(dynamicFilePath);
    }

    public async Task RemoveMetadataReferencesAsync(IReadOnlyList<MetadataReferenceInfo> metadataReferences, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var metadataReference in metadataReferences)
            _project.RemoveMetadataReference(metadataReference.FilePath, metadataReference.CreateProperties());
    }

    public async Task RemoveSourceFilesAsync(IReadOnlyList<string> sourceFiles, CancellationToken _)
    {
        await using var batchScope = _project.CreateBatchScope();

        foreach (var sourceFile in sourceFiles)
            _project.RemoveSourceFile(sourceFile);
    }

    public async Task SetBuildSystemPropertiesAsync(IReadOnlyDictionary<string, string> properties, CancellationToken _)
    {
        // Create a batch scope, just so we have asynchronous closing and application of the batch.
        await using var batchScope = _project.CreateBatchScope();

        foreach (var (name, value) in properties)
        {
            var valueOrNull = string.IsNullOrEmpty(value) ? null : value;

            switch (name)
            {
                case "AssemblyName": _project.AssemblyName = value; break;
                case "MaxSupportedLangVersion": _project.MaxLangVersion = value; break;
                case "RootNamespace": _project.DefaultNamespace = valueOrNull; break;
                case "RunAnalyzers": _project.RunAnalyzers = bool.Parse(valueOrNull ?? bool.TrueString); break;
                case "RunAnalyzersDuringLiveAnalysis": _project.RunAnalyzersDuringLiveAnalysis = bool.Parse(valueOrNull ?? bool.TrueString); break;
                case "TargetPath": _project.OutputFilePath = GetFullyQualifiedPath(valueOrNull); break;
                case "TargetRefPath": _project.OutputRefFilePath = GetFullyQualifiedPath(valueOrNull); break;
                case "TargetFrameworkIdentifier": _targetFrameworkManager.UpdateIdentifierForProject(_project.Id, valueOrNull); break;
            }
        }

        // Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1830960
        _project.CompilationOutputAssemblyFilePath = _project.OutputFilePath;

        string? GetFullyQualifiedPath(string? propertyValue)
        {
            Contract.ThrowIfNull(_project.FilePath, "We don't have a project path at this point.");

            if (propertyValue is not null)
                return Path.Combine(_project.FilePath, propertyValue);
            else
                return null;
        }
    }

    public async Task SetCommandLineArgumentsAsync(IReadOnlyList<string> arguments, CancellationToken _)
    {
        // Create a batch scope, just so we have asynchronous closing and application of the batch.
        await using var batchScope = _project.CreateBatchScope();
        _optionsProcessor.SetCommandLine(arguments.ToImmutableArray());
    }

    public async Task SetDisplayNameAsync(string displayName, CancellationToken _)
    {
        // Create a batch scope, just so we have asynchronous closing and application of the batch.
        await using var batchScope = _project.CreateBatchScope();
        _project.DisplayName = displayName;
    }

    public async Task SetProjectHasAllInformationAsync(bool hasAllInformation, CancellationToken _)
    {
        // Create a batch scope, just so we have asynchronous closing and application of the batch.
        await using var batchScope = _project.CreateBatchScope();
        _project.HasAllInformation = hasAllInformation;
    }

    public Task<IWorkspaceProjectBatch> StartBatchAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IWorkspaceProjectBatch>(new WorkspaceProjectBatch(_project.CreateBatchScope()));
    }

    private class WorkspaceProjectBatch : IWorkspaceProjectBatch
    {
        private IAsyncDisposable? _batch;

        public WorkspaceProjectBatch(IAsyncDisposable batch)
        {
            _batch = batch;
        }

        public async Task ApplyAsync(CancellationToken cancellationToken)
        {
            if (_batch == null)
                throw new InvalidOperationException("The batch has already been applied.");

            await _batch.DisposeAsync().ConfigureAwait(false);
            _batch = null;
        }

        public void Dispose()
        {
        }
    }
}
