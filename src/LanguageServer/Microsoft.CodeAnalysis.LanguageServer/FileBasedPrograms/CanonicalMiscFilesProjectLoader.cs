// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Handles loading miscellaneous files that are not file-based programs.
/// These files are loaded into a canonical project backed by an empty .cs file in temp.
/// </summary>
internal sealed class CanonicalMiscFilesProjectLoader : LanguageServerProjectLoader
{
    private readonly ILogger<CanonicalMiscFilesProjectLoader> _logger;
    private string? _canonicalProjectPath;
    private string? _canonicalDocumentPath;

    public CanonicalMiscFilesProjectLoader(
        LanguageServerWorkspaceFactory workspaceFactory,
        IFileChangeWatcher fileChangeWatcher,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ProjectLoadTelemetryReporter projectLoadTelemetry,
        ServerConfigurationFactory serverConfigurationFactory,
        IBinLogPathProvider binLogPathProvider)
            : base(
                workspaceFactory,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binLogPathProvider)
    {
        _logger = loggerFactory.CreateLogger<CanonicalMiscFilesProjectLoader>();
    }

    /// <summary>
    /// Adds a miscellaneous document to the canonical project.
    /// If the canonical project doesn't exist, creates a primordial project and starts loading the canonical project.
    /// </summary>
    public async ValueTask<TextDocument?> AddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, CancellationToken cancellationToken)
    {
        var documentPath = uri.ParsedUri is not null 
            ? ProtocolConversions.GetDocumentFilePathFromUri(uri.ParsedUri) 
            : uri.UriString;
        
        // Execute under the gate to ensure thread-safety
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            // Ensure the canonical project path is created
            if (_canonicalProjectPath is null)
            {
                InitializeCanonicalProjectPath();
            }

            Contract.ThrowIfNull(_canonicalProjectPath);
            Contract.ThrowIfNull(_canonicalDocumentPath);

            // Check the current state of the canonical project
            if (loadedProjects.TryGetValue(_canonicalProjectPath, out var loadState))
            {
                if (loadState is ProjectLoadState.LoadedTargets loadedTargets && loadedTargets.LoadedProjectTargets.Length > 0)
                {
                    // Case 1: Project is fully loaded with targets
                    return await AddDocumentToLoadedProjectAsync(documentPath, documentText, loadedTargets.LoadedProjectTargets[0], cancellationToken);
                }
                else
                {
                    // Case 2: Project exists but is in primordial state
                    return await AddDocumentToPrimordialProjectAsync(documentPath, documentText, cancellationToken);
                }
            }
            else
            {
                // Case 3: Project doesn't exist at all
                return await CreatePrimordialProjectAndAddDocumentAsync(documentPath, documentText, cancellationToken);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Removes a miscellaneous document from the canonical project.
    /// The canonical project itself is never removed.
    /// </summary>
    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri, CancellationToken cancellationToken)
    {
        var documentPath = uri.ParsedUri is not null 
            ? ProtocolConversions.GetDocumentFilePathFromUri(uri.ParsedUri) 
            : uri.UriString;

        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            if (_canonicalProjectPath is null)
            {
                // No canonical project exists, nothing to remove
                return false;
            }

            // Try to find and remove the document from either workspace
            var hostWorkspace = _workspaceFactory.HostProjectFactory.Workspace;
            var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;

            var document = hostWorkspace.CurrentSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => PathUtilities.Comparer.Equals(d.FilePath, documentPath));

            if (document is null)
            {
                document = miscWorkspace.CurrentSolution.Projects
                    .SelectMany(p => p.Documents)
                    .FirstOrDefault(d => PathUtilities.Comparer.Equals(d.FilePath, documentPath));
            }

            if (document is not null)
            {
                var projectFactory = document.Project.Solution.Workspace == hostWorkspace
                    ? _workspaceFactory.HostProjectFactory
                    : _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;

                await projectFactory.ApplyChangeToWorkspaceAsync(workspace =>
                {
                    workspace.OnDocumentRemoved(document.Id);
                }, cancellationToken);

                return true;
            }

            return false;
        }, cancellationToken);
    }

    /// <summary>
    /// Checks if the given document is part of the canonical miscellaneous files project.
    /// </summary>
    public async ValueTask<bool> IsMiscellaneousDocumentAsync(string documentPath, CancellationToken cancellationToken)
    {
        return await ExecuteUnderGateAsync(loadedProjects =>
        {
            if (_canonicalProjectPath is null)
            {
                return ValueTask.FromResult(false);
            }

            // Check both workspaces
            var hostWorkspace = _workspaceFactory.HostProjectFactory.Workspace;
            var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;

            var inHost = hostWorkspace.CurrentSolution.Projects
                .Where(p => PathUtilities.Comparer.Equals(p.FilePath, _canonicalProjectPath))
                .SelectMany(p => p.Documents)
                .Any(d => PathUtilities.Comparer.Equals(d.FilePath, documentPath));

            if (inHost)
                return ValueTask.FromResult(true);

            var inMisc = miscWorkspace.CurrentSolution.Projects
                .Where(p => PathUtilities.Comparer.Equals(p.FilePath, _canonicalProjectPath))
                .SelectMany(p => p.Documents)
                .Any(d => PathUtilities.Comparer.Equals(d.FilePath, documentPath));

            return ValueTask.FromResult(inMisc);
        }, cancellationToken);
    }

    private void InitializeCanonicalProjectPath()
    {
        // Create a temp directory for the canonical project
        var tempDirectory = Path.Combine(Path.GetTempPath(), "roslyn-canonical-misc", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        _canonicalDocumentPath = Path.Combine(tempDirectory, "Canonical.cs");
        _canonicalProjectPath = Path.ChangeExtension(_canonicalDocumentPath, ".csproj");

        // Create the empty canonical document
        File.WriteAllText(_canonicalDocumentPath, string.Empty);
    }

    private async ValueTask<TextDocument?> AddDocumentToLoadedProjectAsync(string documentPath, SourceText documentText, LoadedProject loadedProject, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_canonicalProjectPath);

        var hostWorkspace = _workspaceFactory.HostProjectFactory.Workspace;
        var project = hostWorkspace.CurrentSolution.GetProject(loadedProject.GetProjectSystemProject().Id);

        if (project is null)
        {
            _logger.LogWarning($"Canonical project at '{_canonicalProjectPath}' not found in workspace.");
            return null;
        }

        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        Document? addedDocument = null;
        await _workspaceFactory.HostProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
        {
            workspace.OnDocumentAdded(documentInfo);
            addedDocument = workspace.CurrentSolution.GetDocument(documentInfo.Id);
        }, cancellationToken);

        return addedDocument;
    }

    private async ValueTask<TextDocument?> AddDocumentToPrimordialProjectAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_canonicalProjectPath);

        // Add to the primordial project in the miscellaneous files workspace
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var project = miscWorkspace.CurrentSolution.Projects
            .FirstOrDefault(p => PathUtilities.Comparer.Equals(p.FilePath, _canonicalProjectPath));

        if (project is null)
        {
            _logger.LogWarning($"Primordial project at '{_canonicalProjectPath}' not found in miscellaneous workspace.");
            return null;
        }

        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        Document? addedDocument = null;
        await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
        {
            workspace.OnDocumentAdded(documentInfo);
            addedDocument = workspace.CurrentSolution.GetDocument(documentInfo.Id);
        }, cancellationToken);

        return addedDocument;
    }

    private async ValueTask<TextDocument?> CreatePrimordialProjectAndAddDocumentAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_canonicalProjectPath);
        Contract.ThrowIfNull(_canonicalDocumentPath);

        // Create primordial project with the canonical document
        var canonicalText = SourceText.From(string.Empty);
        var canonicalLoader = new SourceTextLoader(canonicalText, _canonicalDocumentPath);
        
        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace,
            _canonicalProjectPath,
            canonicalLoader,
            new LanguageInformation(LanguageNames.CSharp, scriptExtension: null),
            canonicalText.ChecksumAlgorithm,
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace.Services.SolutionServices,
            metadataReferences: []);

        // Add the project first, then add the requested document
        _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace =>
        {
            workspace.OnProjectAdded(projectInfo);
        });

        // Now add the requested document
        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(projectInfo.Id),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace =>
        {
            workspace.OnDocumentAdded(documentInfo);
        });

        // Begin loading the canonical project with a design-time build
        await BeginLoadingProjectWithPrimordialAsync(
            _canonicalProjectPath,
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
            projectInfo.Id,
            doDesignTimeBuild: true);

        // Return the requested document (not the canonical one)
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        return miscWorkspace.CurrentSolution.GetDocument(documentInfo.Id);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_canonicalDocumentPath);

        // Generate a static csproj XML for a class library
        var virtualProjectXml = GenerateCanonicalProjectXml(_canonicalDocumentPath);

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, projectPath, dotnetPath: null, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(projectPath, virtualProjectXml, languageName: LanguageNames.CSharp, cancellationToken);

        return new RemoteProjectLoadResult
        {
            ProjectFile = loadedFile,
            ProjectFactory = _workspaceFactory.HostProjectFactory,
            IsFileBasedProgram = false,
            IsMiscellaneousFile = true,
            PreferredBuildHostKind = buildHostKind,
            ActualBuildHostKind = buildHostKind,
        };
    }

    protected override ValueTask OnProjectUnloadedAsync(string projectFilePath)
    {
        // Nothing special to do on unload for canonical project
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask TransitionPrimordialProjectToLoadedAsync(
        string projectPath,
        ProjectSystemProjectFactory primordialProjectFactory,
        ProjectId primordialProjectId,
        ImmutableArray<LoadedProject> loadedTargets,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(loadedTargets.IsEmpty, "Expected at least one loaded target");

        // Get all documents from the primordial project that are NOT the canonical document
        var primordialWorkspace = primordialProjectFactory.Workspace;
        var primordialProject = primordialWorkspace.CurrentSolution.GetProject(primordialProjectId);

        if (primordialProject is null)
        {
            _logger.LogWarning($"Primordial project '{projectPath}' not found during transition.");
            await primordialProjectFactory.ApplyChangeToWorkspaceAsync(
                workspace => workspace.OnProjectRemoved(primordialProjectId),
                cancellationToken);
            return;
        }

        // Get all misc documents (excluding the canonical document)
        var miscDocuments = primordialProject.Documents
            .Where(d => !PathUtilities.Comparer.Equals(d.FilePath, _canonicalDocumentPath))
            .ToImmutableArray();

        // Add all misc documents to the loaded project (first target)
        var loadedProject = loadedTargets[0];
        var loadedProjectId = loadedProject.GetProjectSystemProject().Id;

        foreach (var miscDoc in miscDocuments)
        {
            var text = await miscDoc.GetTextAsync(cancellationToken);
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(loadedProjectId),
                name: miscDoc.Name,
                loader: TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                filePath: miscDoc.FilePath);

            await _workspaceFactory.HostProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
            {
                workspace.OnDocumentAdded(documentInfo);
            }, cancellationToken);
        }

        // Now remove the primordial project
        await primordialProjectFactory.ApplyChangeToWorkspaceAsync(
            workspace => workspace.OnProjectRemoved(primordialProjectId),
            cancellationToken);
    }

    private static string GenerateCanonicalProjectXml(string documentFilePath)
    {
        var artifactsPath = GetCanonicalArtifactsPath();
        var targetFramework = "net9.0";

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
                <Compile Include="{SecurityElement.Escape(documentFilePath)}" />
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

    private static string GetCanonicalArtifactsPath()
    {
        // We want a location where permissions are expected to be restricted to the current user.
        string directory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetTempPath()
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Join(directory, "dotnet", "roslyn", "canonical-misc");
    }
}
