// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Manages a canonical miscellaneous files project that is shared across all genuine miscellaneous files.
/// This avoids running a design-time build for each individual misc file.
/// </summary>
internal sealed class CanonicalMiscFilesProject
{
    private readonly LanguageServerWorkspaceFactory _workspaceFactory;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private readonly string _canonicalProjectPath;
    private readonly string _emptyFilePath;

    // State protected by _gate
    private LoadedProject? _loadedProject;
    private bool _hasBeenInitialized;

    public ProjectId? Id => _loadedProject?.ProjectSystemProject.Id;

    public CanonicalMiscFilesProject(
        LanguageServerWorkspaceFactory workspaceFactory,
        ILogger logger)
    {
        _workspaceFactory = workspaceFactory;
        _logger = logger;

        // Create a temp location for the canonical project
        var tempDirectory = GetCanonicalProjectDirectory();
        Directory.CreateDirectory(tempDirectory);

        _emptyFilePath = Path.Combine(tempDirectory, "EmptyFile.cs");
        _canonicalProjectPath = Path.Combine(tempDirectory, "CanonicalMiscFiles.csproj");

        // Create an empty file for the initial build
        File.WriteAllText(_emptyFilePath, string.Empty);
    }

    private static string GetCanonicalProjectDirectory()
    {
        string baseDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetTempPath()
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(baseDirectory, "roslyn-lsp", "canonical-misc-files");
    }

    /// <summary>
    /// Ensures the canonical project is initialized with a design-time build.
    /// </summary>
    public async Task<LoadedProject?> EnsureInitializedAsync(
        BuildHostProcessManager buildHostProcessManager,
        IFileChangeWatcher fileChangeWatcher,
        ImmutableDictionary<string, string> additionalProperties,
        CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            if (_hasBeenInitialized)
            {
                return _loadedProject;
            }

            _hasBeenInitialized = true;

            try
            {
                // Create the project XML content
                var projectContent = CreateCanonicalProjectContent();

                // Write the project file to disk
                File.WriteAllText(_canonicalProjectPath, projectContent);

                // Perform the design-time build
                const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
                var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, _canonicalProjectPath, dotnetPath: null, cancellationToken);
                var loadedFile = await buildHost.LoadProjectAsync(_canonicalProjectPath, projectContent, languageName: LanguageNames.CSharp, cancellationToken);

                var diagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken);
                if (diagnosticLogItems.Any(item => item.Kind is DiagnosticLogItemKind.Error))
                {
                    foreach (var diagnostic in diagnosticLogItems)
                    {
                        _logger.LogError($"Error loading canonical misc files project: {diagnostic.Message}");
                    }
                    return null;
                }

                var loadedProjectInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken);
                if (loadedProjectInfos.Length == 0)
                {
                    _logger.LogError("No project info loaded for canonical misc files project");
                    return null;
                }

                var projectInfo = loadedProjectInfos[0];
                var projectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;

                // Create the project in the workspace
                var projectSystemName = "Canonical Miscellaneous Files";
                var projectCreationInfo = new ProjectSystemProjectCreationInfo
                {
                    AssemblyName = projectSystemName,
                    FilePath = _canonicalProjectPath,
                    CompilationOutputAssemblyFilePath = projectInfo.IntermediateOutputFilePath,
                };

                var projectSystemProject = await projectFactory.CreateAndAddToWorkspaceAsync(
                    projectSystemName,
                    LanguageNames.CSharp,
                    projectCreationInfo,
                    _workspaceFactory.ProjectSystemHostInfo);

                _loadedProject = new LoadedProject(
                    projectSystemProject,
                    projectFactory,
                    fileChangeWatcher,
                    _workspaceFactory.TargetFrameworkManager);

                // Update the project with the build information
                await _loadedProject.UpdateWithNewProjectInfoAsync(projectInfo, isMiscellaneousFile: true, _logger);

                // Remove the empty file that was used for the initial build
                var workspace = projectFactory.Workspace;
                var project = workspace.CurrentSolution.GetProject(_loadedProject.ProjectSystemProject.Id);
                if (project != null)
                {
                    var emptyDocument = project.Documents.FirstOrDefault(d => d.FilePath == _emptyFilePath);
                    if (emptyDocument != null)
                    {
                        await projectFactory.ApplyChangeToWorkspaceAsync(
                            w => w.OnDocumentRemoved(emptyDocument.Id),
                            cancellationToken);
                    }
                }

                _logger.LogInformation("Canonical miscellaneous files project initialized successfully");
                return _loadedProject;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize canonical miscellaneous files project");
                return null;
            }
        }
    }

    /// <summary>
    /// Adds a document to the canonical project.
    /// </summary>
    public async Task<Document?> AddDocumentAsync(
        string filePath,
        SourceText sourceText,
        CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            if (_loadedProject == null)
            {
                return null;
            }

            var projectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;
            var workspace = projectFactory.Workspace;
            var project = workspace.CurrentSolution.GetProject(_loadedProject.ProjectSystemProject.Id);
            if (project == null)
            {
                _logger.LogError("Canonical project not found in workspace");
                return null;
            }

            // Add the source file to the project
            _loadedProject.ProjectSystemProject.AddSourceFile(filePath);

            // Get the updated project and document
            var updatedProject = workspace.CurrentSolution.GetProject(_loadedProject.ProjectSystemProject.Id);
            var document = updatedProject?.Documents.FirstOrDefault(d => d.FilePath == filePath);

            return document;
        }
    }

    /// <summary>
    /// Removes a document from the canonical project.
    /// </summary>
    public async Task<bool> RemoveDocumentAsync(string filePath, CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            if (_loadedProject == null)
            {
                return false;
            }

            var projectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;
            var workspace = projectFactory.Workspace;
            var project = workspace.CurrentSolution.GetProject(_loadedProject.ProjectSystemProject.Id);
            if (project == null)
            {
                return false;
            }

            var document = project.Documents.FirstOrDefault(d => d.FilePath == filePath);
            if (document == null)
            {
                return false;
            }

            // Remove the source file from the project
            _loadedProject.ProjectSystemProject.RemoveSourceFile(filePath);

            return true;
        }
    }

    private string CreateCanonicalProjectContent()
    {
        var artifactsPath = GetCanonicalProjectDirectory();
        var targetFramework = Environment.GetEnvironmentVariable("DOTNET_RUN_FILE_TFM") ?? "net$(BundledNETCoreAppTargetFrameworkVersion)";

        var virtualProjectXml = $"""
            <Project>
              <PropertyGroup>
                <BaseIntermediateOutputPath>{SecurityElement.Escape(artifactsPath)}\obj\</BaseIntermediateOutputPath>
                <BaseOutputPath>{SecurityElement.Escape(artifactsPath)}\bin\</BaseOutputPath>
              </PropertyGroup>
              <!-- We need to explicitly import Sdk props/targets so we can override the targets below. -->
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
              <PropertyGroup>
                <OutputType>Library</OutputType>
                <TargetFramework>{SecurityElement.Escape(targetFramework)}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <PropertyGroup>
                <EnableDefaultItems>false</EnableDefaultItems>
              </PropertyGroup>
              <PropertyGroup>
                <LangVersion>preview</LangVersion>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="{SecurityElement.Escape(_emptyFilePath)}" />
              </ItemGroup>
              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
              <!--
                Override targets which don't work with project files that are not present on disk.
                See https://github.com/NuGet/Home/issues/14148.
              -->
              <Target Name="_FilterRestoreGraphProjectInputItems"
                      DependsOnTargets="_LoadRestoreGraphEntryPoints"
                      Returns="@(FilteredRestoreGraphProjectInputItems)">
                <ItemGroup>
                  <FilteredRestoreGraphProjectInputItems Include="@(RestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>
              <Target Name="_GetAllRestoreProjectPathItems"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems"
                      Returns="@(_RestoreProjectPathItems)">
                <ItemGroup>
                  <_RestoreProjectPathItems Include="@(FilteredRestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>
              <Target Name="_GenerateRestoreGraph"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems;_GetAllRestoreProjectPathItems;_GenerateRestoreGraphProjectEntry;_GenerateProjectRestoreGraph"
                      Returns="@(_RestoreGraphEntry)">
                <!-- Output from dependency _GenerateRestoreGraphProjectEntry and _GenerateProjectRestoreGraph -->
              </Target>
            </Project>
            """;

        return virtualProjectXml;
    }

    public void Dispose()
    {
        _loadedProject?.Dispose();
    }
}
