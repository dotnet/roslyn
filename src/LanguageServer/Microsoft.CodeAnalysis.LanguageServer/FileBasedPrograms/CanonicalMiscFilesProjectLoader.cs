// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Security;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Handles loading miscellaneous files that are not file-based programs.
/// These files are loaded into a canonical project backed by an empty .cs file in temp.
/// </summary>
internal sealed class CanonicalMiscFilesProjectLoader : LanguageServerProjectLoader, IDisposable
{
    private readonly Lazy<string> _canonicalDocumentPath;

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
        _canonicalDocumentPath = new Lazy<string>(() =>
        {
            // Create a temp directory for the canonical project
            var tempDirectory = Path.Combine(Path.GetTempPath(), "roslyn-canonical-misc", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            var documentPath = Path.Combine(tempDirectory, "Canonical.cs");

            // Create the empty canonical document
            File.WriteAllText(documentPath, string.Empty);

            return documentPath;
        });
    }

    /// <summary>
    /// Adds a miscellaneous document to the canonical project.
    /// If the canonical project doesn't exist, creates a primordial project and starts loading the canonical project.
    /// </summary>
    /// <remarks>
    /// The LSP workspace manager and queue ensure that <see cref="AddMiscellaneousDocumentAsync"/> and <see cref="TryRemoveMiscellaneousDocumentAsync"/> are not called concurrently.
    /// </remarks>
    public async ValueTask<TextDocument> AddMiscellaneousDocumentAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        // Project loading happens asynchronously, so we need to execute this under the load gate to ensure consistency.
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            var canonicalDocumentPath = _canonicalDocumentPath.Value;

            // Check the current state of the canonical project
            if (loadedProjects.TryGetValue(canonicalDocumentPath, out var loadState))
            {
                if (loadState is ProjectLoadState.LoadedTargets loadedTargets)
                {
                    // Case 1: Project is fully loaded with targets

                    // We always expect that the canonical project is either Primordial, or loaded with exactly 1 target (1 TFM).
                    Contract.ThrowIfFalse(loadedTargets.LoadedProjectTargets.Length == 1, "Expected exactly one loaded target for canonical project");
                    var loadedProjectId = loadedTargets.LoadedProjectTargets.Single().ProjectId;
                    return await AddDocumentToExistingProject_NoLockAsync(documentPath, documentText, loadedProjectId, cancellationToken);
                }
                else
                {
                    // Case 2: Primordial project was already created, but hasn't finished loading.
                    var primordialTarget = loadState as ProjectLoadState.Primordial;
                    Contract.ThrowIfNull(primordialTarget, "Expected primordial target");
                    return await AddDocumentToExistingProject_NoLockAsync(documentPath, documentText, primordialTarget.PrimordialProjectId, cancellationToken);
                }
            }
            else
            {
                // Case 3: Project doesn't exist at all
                return CreatePrimordialProjectAndAddDocument_NoLock(documentPath, documentText);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Removes a miscellaneous document from the canonical project.
    /// The canonical project itself is never removed.
    /// </summary>
    /// <remarks>
    /// The LSP workspace manager and queue ensure that <see cref="AddMiscellaneousDocumentAsync"/> and <see cref="TryRemoveMiscellaneousDocumentAsync"/> are not called concurrently.
    /// </remarks>
    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(string documentPath, CancellationToken cancellationToken)
    {
        // Project loading happens asynchronously, so we need to execute this under the load gate to ensure consistency.
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            // Try to find and remove the document from the miscellaneous workspace only
            var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;

            var documents = miscWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(documentPath);
            if (documents.Any())
            {
                await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
                {
                    foreach (var documentId in documents)
                    {
                        workspace.OnDocumentRemoved(documentId);
                    }
                }, cancellationToken);

                return true;
            }

            return false;
        }, cancellationToken);
    }

    private async ValueTask<TextDocument> AddDocumentToExistingProject_NoLockAsync(string documentPath, SourceText documentText, ProjectId existingProjectId, CancellationToken cancellationToken)
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(existingProjectId),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
        {
            workspace.OnDocumentAdded(documentInfo);
        }, cancellationToken);

        var addedDocument = miscWorkspace.CurrentSolution.GetRequiredDocument(documentInfo.Id);
        return addedDocument;
    }

    private TextDocument CreatePrimordialProjectAndAddDocument_NoLock(string documentPath, SourceText documentText)
    {
        var canonicalDocumentPath = _canonicalDocumentPath.Value;

        // Create primordial project with the canonical document
        var canonicalText = SourceText.From(string.Empty);
        var canonicalLoader = new SourceTextLoader(canonicalText, canonicalDocumentPath);

        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace,
            canonicalDocumentPath,
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
            loader: new SourceTextLoader(documentText, documentPath),
            filePath: documentPath);

        _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace =>
        {
            workspace.OnDocumentAdded(documentInfo);
        });

        // Begin loading the canonical project with a design-time build
        BeginLoadingProjectWithPrimordial_NoLock(
            canonicalDocumentPath,
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
            projectInfo.Id,
            doDesignTimeBuild: true);

        // Return the requested document (not the canonical one)
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var addedDocument = miscWorkspace.CurrentSolution.GetRequiredDocument(documentInfo.Id);
        return addedDocument;
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string documentPath, CancellationToken cancellationToken)
    {
        var virtualProjectXml = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <Features>$(Features);RichMiscellaneousFile</Features>
              </PropertyGroup>
            </Project>
            """;

        // When loading a virtual project, the path to the on-disk source file is not used. Instead the path is adjusted to end with .csproj.
        // This is necessary in order to get msbuild to apply the standard c# props/targets to the project.
        var virtualProjectPath = VirtualProjectXmlProvider.GetVirtualProjectPath(documentPath);

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, virtualProjectPath, dotnetPath: null, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(virtualProjectPath, virtualProjectXml, languageName: LanguageNames.CSharp, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        // Transfer any misc documents from the primordial project to the loaded canonical project
        var primordialWorkspace = primordialProjectFactory.Workspace;
        var primordialProject = primordialWorkspace.CurrentSolution.GetRequiredProject(primordialProjectId);

        // Get all misc documents (excluding the canonical document)
        var miscDocuments = primordialProject.Documents
            .Where(d => !PathUtilities.Comparer.Equals(d.FilePath, _canonicalDocumentPath.Value))
            .ToImmutableArray();

        // Add all misc documents to the loaded project
        var loadedProjectId = GetCanonicalProject().Id;

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

    private Project GetCanonicalProject()
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var project = miscWorkspace.CurrentSolution.Projects
            .Single(p => PathUtilities.Comparer.Equals(p.FilePath, _canonicalDocumentPath.Value));

        return project;
    }

    public void Dispose()
    {
        if (_canonicalDocumentPath.IsValueCreated)
        {
            var canonicalTempDirectory = Path.GetDirectoryName(_canonicalDocumentPath.Value);
            IOUtilities.PerformIO(() =>
            {
                if (Directory.Exists(canonicalTempDirectory))
                {
                    Directory.Delete(canonicalTempDirectory, recursive: true);
                }
            });
        }
    }
}
