// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
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
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Handles loading miscellaneous files that can run design time builds but are not file-based programs.
/// These files are loaded into a canonical project backed by an empty .cs file in temp.
/// </summary>
internal sealed class CanonicalMiscFilesProvider : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider, IDisposable
{
    private readonly ILspServices _lspServices;
    private readonly Lazy<string> _canonicalDocumentPath;

    /// <summary>
    /// Avoid showing restore notifications for misc files - it ends up being noisy and confusing
    /// as every file is a misc file on first open until we detect a project for it.
    /// </summary>
    protected override bool EnableProgressReporting => false;

    public CanonicalMiscFilesProvider(
        ILspServices lspServices,
        LanguageServerWorkspaceFactory workspaceFactory,
        IFileChangeWatcher fileChangeWatcher,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ProjectLoadTelemetryReporter projectLoadTelemetry,
        ServerConfigurationFactory serverConfigurationFactory,
        IBinLogPathProvider binLogPathProvider,
        DotnetCliHelper dotnetCliHelper)
            : base(
                workspaceFactory,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binLogPathProvider,
                dotnetCliHelper)
    {
        _lspServices = lspServices;
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

    private string GetDocumentFilePath(DocumentUri uri) => uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;

    public async ValueTask<bool> IsMiscellaneousFilesDocumentAsync(TextDocument textDocument, CancellationToken cancellationToken)
    {
        // Documents managed by this provider are in the misc files workspace
        if (textDocument.Project.Solution.Workspace != _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace)
            return false;

        // Check if this is one of our forked canonical projects by checking the file path
        var documentFilePath = textDocument.FilePath;
        if (documentFilePath is null)
            return false;

        // If the document is in the canonical project itself, it's managed by us
        if (_canonicalDocumentPath.IsValueCreated && PathUtilities.Comparer.Equals(textDocument.Project.FilePath, _canonicalDocumentPath.Value))
            return true;

        // Check if this document is part of a forked canonical project by checking if the canonical project exists
        return await IsCanonicalProjectLoadedAsync(cancellationToken);
    }

    public async ValueTask<bool> CanTakeOwnership(SourceText documentText, string documentFilePath, string languageId)
    {
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        var uri = new DocumentUri(documentFilePath);
        if (!languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInformation))
        {
            return false;
        }

        // Only handle C# files when file-based programs are enabled
        if (languageInformation.LanguageName != LanguageNames.CSharp)
            return false;

        if (!GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms))
            return false;

        // Don't handle file-based programs (those with #! or #: directives)
        if (VirtualProjectXmlProvider.IsFileBasedProgram(documentText))
            return false;

        // This provider handles files that can run design time builds
        // For virtual (non-file) URIs, we can handle them
        if (uri.ParsedUri is null || !uri.ParsedUri.IsFile)
            return true;

        return true;
    }

    /// <summary>
    /// Adds a miscellaneous document to the canonical project.
    /// If the canonical project doesn't exist, creates a primordial project and starts loading the canonical project.
    /// </summary>
    /// <remarks>
    /// The LSP workspace manager and queue ensure that <see cref="TryAddMiscellaneousDocumentAsync"/> and <see cref="TryRemoveMiscellaneousDocumentAsync"/> are not called concurrently.
    /// </remarks>
    public async ValueTask<TextDocument?> TryAddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var documentPath = GetDocumentFilePath(uri);
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInformation))
        {
            return null;
        }

        // Only handle C# files when file-based programs are enabled
        if (languageInformation.LanguageName != LanguageNames.CSharp)
            return null;

        if (!GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms))
            return null;

        // Don't handle file-based programs
        if (VirtualProjectXmlProvider.IsFileBasedProgram(documentText))
            return null;

        // Project loading happens asynchronously, so we need to execute this under the load gate to ensure consistency.
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            var canonicalDocumentPath = _canonicalDocumentPath.Value;

            // Check the current state of the canonical project
            if (loadedProjects.TryGetValue(canonicalDocumentPath, out var loadState))
            {
                if (loadState is ProjectLoadState.LoadedTargets loadedTargets)
                {
                    // Case 1: Canonical project is fully loaded with targets
                    // We always expect that the canonical project is either Primordial, or loaded with exactly 1 target (1 TFM).
                    Contract.ThrowIfFalse(loadedTargets.LoadedProjectTargets.Length == 1, "Expected exactly one loaded target for canonical project");
                    return await ForkCanonicalProjectAndAddDocument_NoLockAsync(documentPath, documentText, CancellationToken.None);
                }
                else
                {
                    // Case 2: Primordial canonical project was already created, but hasn't finished loading.
                    var primordialTarget = loadState as ProjectLoadState.Primordial;
                    Contract.ThrowIfNull(primordialTarget, "Expected primordial target");
                    return await AddDocumentToPrimordialProject_NoLockAsync(documentPath, documentText, primordialTarget.PrimordialProjectId, CancellationToken.None);
                }
            }
            else
            {
                // Case 3: Canonical project doesn't exist at all
                return CreatePrimordialProjectAndAddDocument_NoLock(documentPath, documentText);
            }
        }, CancellationToken.None);
    }

    /// <summary>
    /// Removes a miscellaneous document from the canonical project.
    /// The canonical project itself is never removed.
    /// </summary>
    /// <remarks>
    /// The LSP workspace manager and queue ensure that <see cref="TryAddMiscellaneousDocumentAsync"/> and <see cref="TryRemoveMiscellaneousDocumentAsync"/> are not called concurrently.
    /// </remarks>
    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri)
    {
        var documentPath = GetDocumentFilePath(uri);

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
                }, CancellationToken.None);

                return true;
            }

            return false;
        }, CancellationToken.None);
    }

    private async ValueTask<TextDocument> ForkCanonicalProjectAndAddDocument_NoLockAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        var newProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{documentPath}'");
        var newDocumentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(newProjectId),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        var forkedProjectInfo = await GetForkedProjectInfoAsync(GetCanonicalProject(), newDocumentInfo, documentText, GlobalOptionService, cancellationToken);

        await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
        {
            workspace.OnProjectAdded(forkedProjectInfo);
        }, cancellationToken);

        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var addedDocument = miscWorkspace.CurrentSolution.GetRequiredDocument(newDocumentInfo.Id);
        return addedDocument;
    }

    internal async ValueTask<bool> IsCanonicalProjectLoadedAsync(CancellationToken cancellationToken)
    {
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            if (!_canonicalDocumentPath.IsValueCreated)
                return false;

            var canonicalDocumentPath = _canonicalDocumentPath.Value;
            return loadedProjects.TryGetValue(canonicalDocumentPath, out var loadState)
                && loadState is ProjectLoadState.LoadedTargets;
        }, cancellationToken);
    }

    private async ValueTask<TextDocument> AddDocumentToPrimordialProject_NoLockAsync(string documentPath, SourceText documentText, ProjectId existingProjectId, CancellationToken cancellationToken)
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
        // Set the FileBasedProgram feature flag so that '#:' is permitted without errors in rich misc files.
        // This allows us to avoid spurious errors for files which contain '#:' directives yet are not treated as file-based programs (due to not being saved to disk, for example.)
        var virtualProjectXml = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <Features>$(Features);FileBasedProgram</Features>
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

    protected override async ValueTask OnProjectUnloadedAsync(string projectFilePath)
    {
        // Nothing special to do on unload for canonical project
    }

    protected override async ValueTask TransitionPrimordialProjectToLoaded_NoLockAsync(
        string projectPath,
        ProjectSystemProjectFactory primordialProjectFactory,
        ProjectId primordialProjectId,
        CancellationToken cancellationToken)
    {
        // We only pass 'doDesignTimeBuild: true' for the canonical project. So that's the only time we should get called back for this.
        Contract.ThrowIfFalse(projectPath == _canonicalDocumentPath.Value);

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
            Contract.ThrowIfNull(miscDoc.FilePath);
            await ForkCanonicalProjectAndAddDocument_NoLockAsync(miscDoc.FilePath, await miscDoc.GetTextAsync(cancellationToken), cancellationToken);
        }

        // Now remove the primordial project
        await primordialProjectFactory.ApplyChangeToWorkspaceAsync(
            workspace => workspace.OnProjectRemoved(primordialProjectId),
            cancellationToken);
    }

    /// <summary>
    /// Creates a new project based on the canonical project with a new document added.
    /// This should only be called when the canonical project is in the FullyLoaded state.
    /// </summary>
    private static async Task<ProjectInfo> GetForkedProjectInfoAsync(Project canonicalProject, DocumentInfo newDocumentInfo, SourceText documentText, IGlobalOptionService globalOptionService, CancellationToken cancellationToken)
    {
        var newDocumentPath = newDocumentInfo.FilePath;
        Contract.ThrowIfNull(newDocumentPath);

        var forkedProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{newDocumentPath}'");
        var syntaxTree = CSharpSyntaxTree.ParseText(text: documentText, canonicalProject.ParseOptions as CSharpParseOptions, path: newDocumentPath, cancellationToken);
        var hasAllInformation = await VirtualProjectXmlProvider.ShouldReportSemanticErrorsInPossibleFileBasedProgramAsync(globalOptionService, syntaxTree, cancellationToken);
        var forkedProjectAttributes = new ProjectInfo.ProjectAttributes(
            newDocumentInfo.Id.ProjectId,
            version: VersionStamp.Create(),
            name: canonicalProject.Name,
            assemblyName: canonicalProject.AssemblyName,
            language: canonicalProject.Language,
            compilationOutputInfo: default,
            checksumAlgorithm: SourceHashAlgorithm.Sha1,
            filePath: newDocumentPath,
            outputFilePath: canonicalProject.OutputFilePath,
            outputRefFilePath: canonicalProject.OutputRefFilePath,
            hasAllInformation: hasAllInformation);

        var forkedProjectInfo = ProjectInfo.Create(
            attributes: forkedProjectAttributes,
            compilationOptions: canonicalProject.CompilationOptions,
            parseOptions: canonicalProject.ParseOptions,
            documents: [newDocumentInfo, .. await Task.WhenAll(canonicalProject.Documents.Select(document => GetDocumentInfoAsync(document, document.FilePath)))],
            projectReferences: canonicalProject.ProjectReferences,
            metadataReferences: canonicalProject.MetadataReferences,
            analyzerReferences: canonicalProject.AnalyzerReferences,
            analyzerConfigDocuments: await canonicalProject.AnalyzerConfigDocuments.SelectAsArrayAsync(async document => await GetDocumentInfoAsync(document, document.FilePath)),
            additionalDocuments: await canonicalProject.AdditionalDocuments.SelectAsArrayAsync(async document => await GetDocumentInfoAsync(document, document.FilePath)));
        return forkedProjectInfo;

        async Task<DocumentInfo> GetDocumentInfoAsync(TextDocument document, string? documentPath) =>
            DocumentInfo.Create(
                DocumentId.CreateNewId(forkedProjectId),
                name: Path.GetFileName(documentPath) ?? "",
                loader: TextLoader.From(TextAndVersion.Create(await document.GetTextAsync(cancellationToken).ConfigureAwait(false), VersionStamp.Create())),
                filePath: documentPath);
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
