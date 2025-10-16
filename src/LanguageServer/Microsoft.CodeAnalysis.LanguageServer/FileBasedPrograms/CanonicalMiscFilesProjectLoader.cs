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
    private readonly Lazy<(string ProjectPath, string DocumentPath)> _canonicalPaths;

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
        _canonicalPaths = new Lazy<(string ProjectPath, string DocumentPath)>(() =>
        {
            // Create a temp directory for the canonical project
            var tempDirectory = Path.Combine(Path.GetTempPath(), "roslyn-canonical-misc", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            var documentPath = Path.Combine(tempDirectory, "Canonical.cs");
            var projectPath = Path.ChangeExtension(documentPath, ".csproj");

            // Create the empty canonical document
            File.WriteAllText(documentPath, string.Empty);

            return (projectPath, documentPath);
        });
    }

    /// <summary>
    /// Adds a miscellaneous document to the canonical project.
    /// If the canonical project doesn't exist, creates a primordial project and starts loading the canonical project.
    /// </summary>
    public async ValueTask<TextDocument> AddMiscellaneousDocumentAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        // Execute under the gate to ensure thread-safety
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            var (canonicalProjectPath, canonicalDocumentPath) = _canonicalPaths.Value;

            // Check the current state of the canonical project
            if (loadedProjects.TryGetValue(canonicalProjectPath, out var loadState))
            {
                if (loadState is ProjectLoadState.LoadedTargets loadedTargets && loadedTargets.LoadedProjectTargets.Length > 0)
                {
                    // Case 1: Project is fully loaded with targets
                    // Verify that there is a LoadedProject with the canonical path and use it
                    var loadedProject = loadedTargets.LoadedProjectTargets.Single(p => 
                        PathUtilities.Comparer.Equals(p.GetProjectSystemProject().FilePath, canonicalProjectPath));
                    Contract.ThrowIfFalse(loadedTargets.LoadedProjectTargets.Length == 1, "Expected exactly one loaded target for canonical project");
                    
                    return await AddDocumentToLoadedProjectAsync(documentPath, documentText, loadedProject, cancellationToken);
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
    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(string documentPath, CancellationToken cancellationToken)
    {
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            // Try to find and remove the document from the miscellaneous workspace only
            var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;

            var document = miscWorkspace.CurrentSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => PathUtilities.Comparer.Equals(d.FilePath, documentPath));

            if (document is not null)
            {
                await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
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
            var (canonicalProjectPath, _) = _canonicalPaths.Value;

            // Check only the miscellaneous workspace
            var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;

            var inMisc = miscWorkspace.CurrentSolution.Projects
                .Where(p => PathUtilities.Comparer.Equals(p.FilePath, canonicalProjectPath))
                .SelectMany(p => p.Documents)
                .Any(d => PathUtilities.Comparer.Equals(d.FilePath, documentPath));

            return ValueTask.FromResult(inMisc);
        }, cancellationToken);
    }

    private async ValueTask<TextDocument> AddDocumentToLoadedProjectAsync(string documentPath, SourceText documentText, LoadedProject loadedProject, CancellationToken cancellationToken)
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var project = miscWorkspace.CurrentSolution.GetProject(loadedProject.GetProjectSystemProject().Id);

        Contract.ThrowIfNull(project, "Canonical project must exist in workspace");

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

        Contract.ThrowIfNull(addedDocument);
        return addedDocument;
    }

    private async ValueTask<TextDocument> AddDocumentToPrimordialProjectAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        var (canonicalProjectPath, _) = _canonicalPaths.Value;

        // Add to the primordial project in the miscellaneous files workspace
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var project = miscWorkspace.CurrentSolution.Projects
            .Single(p => PathUtilities.Comparer.Equals(p.FilePath, canonicalProjectPath));

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

        Contract.ThrowIfNull(addedDocument);
        return addedDocument;
    }

    private async ValueTask<TextDocument> CreatePrimordialProjectAndAddDocumentAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        var (canonicalProjectPath, canonicalDocumentPath) = _canonicalPaths.Value;

        // Create primordial project with the canonical document
        var canonicalText = SourceText.From(string.Empty);
        var canonicalLoader = new SourceTextLoader(canonicalText, canonicalDocumentPath);

        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace,
            canonicalProjectPath,
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
            canonicalProjectPath,
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
            projectInfo.Id,
            doDesignTimeBuild: true);

        // Return the requested document (not the canonical one)
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var addedDocument = miscWorkspace.CurrentSolution.GetDocument(documentInfo.Id);
        Contract.ThrowIfNull(addedDocument);
        return addedDocument;
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
    {
        var (_, canonicalDocumentPath) = _canonicalPaths.Value;

        // Generate a simple class library csproj XML
        var virtualProjectXml = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="{SecurityElement.Escape(canonicalDocumentPath)}" />
              </ItemGroup>
            </Project>
            """;

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, projectPath, dotnetPath: null, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(projectPath, virtualProjectXml, languageName: LanguageNames.CSharp, cancellationToken);

        return new RemoteProjectLoadResult
        {
            ProjectFile = loadedFile,
            ProjectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
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
        Contract.ThrowIfFalse(loadedTargets.Length == 1, "Expected exactly one loaded target for canonical project");

        var (_, canonicalDocumentPath) = _canonicalPaths.Value;

        // Get all documents from the primordial project that are NOT the canonical document
        var primordialWorkspace = primordialProjectFactory.Workspace;
        var primordialProject = primordialWorkspace.CurrentSolution.GetProject(primordialProjectId);

        if (primordialProject is null)
        {
            Contract.Fail($"Primordial project '{projectPath}' not found during transition.");
            return;
        }

        // Get all misc documents (excluding the canonical document)
        var miscDocuments = primordialProject.Documents
            .Where(d => !PathUtilities.Comparer.Equals(d.FilePath, canonicalDocumentPath))
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

            await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
            {
                workspace.OnDocumentAdded(documentInfo);
            }, cancellationToken);
        }

        // Now remove the primordial project
        await primordialProjectFactory.ApplyChangeToWorkspaceAsync(
            workspace => workspace.OnProjectRemoved(primordialProjectId),
            cancellationToken);
    }
}
