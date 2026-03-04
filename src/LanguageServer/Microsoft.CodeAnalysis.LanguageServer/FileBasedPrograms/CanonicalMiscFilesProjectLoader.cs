// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Handles loading miscellaneous files that are not file-based programs.
/// These files are loaded into a canonical project backed by an empty .cs file in temp.
/// </summary>
internal sealed class CanonicalMiscFilesProjectLoader : LanguageServerProjectLoader, IDisposable
{
    private readonly Lazy<string> _canonicalDocumentPath;

    /// <summary>
    /// Avoid showing restore notifications for misc files - it ends up being noisy and confusing
    /// as every file is a misc file on first open until we detect a project for it.
    /// </summary>
    protected override bool EnableProgressReporting => false;

    public CanonicalMiscFilesProjectLoader(
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

    private bool IsForkedCanonicalDocument(TextDocument document)
    {
        return document.Project.Documents.Contains(document => PathUtilities.Comparer.Equals(document.FilePath, _canonicalDocumentPath.Value));
    }

    private async ValueTask<TextDocument> TransitionToCanonicalForkedIfNeededAsync(
        string documentPath, TextDocument existingDocument, SourceText documentText, LooseDocumentKind documentKind, CancellationToken cancellationToken)
    {
        // Nothing to do in the following cases:
        // - Document is classified as MiscellaneousFileWithNoReferences
        // - Document is already in a forked canonical project
        // - Canonical project is not loaded yet
        if (documentKind == LooseDocumentKind.MiscellaneousFileWithNoReferences
            || IsForkedCanonicalDocument(existingDocument)
            || GetCanonicalProject() is not { } canonicalProject)
        {
            return existingDocument;
        }

        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            await TryUnloadProject_NoLockAsync(documentPath);
            return await AddForkedCanonicalProject_NoLockAsync(
                canonicalProject, loadedProjects, documentPath, documentText, hasAllInformation: documentKind == LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors, cancellationToken);
        }, cancellationToken);
    }

    private TextDocument UpdateHasAllInformationIfNeeded(DocumentUri documentUri, TextDocument existingDocument, LooseDocumentKind documentKind)
    {
        if (documentKind is LooseDocumentKind.MiscellaneousFileWithNoReferences)
        {
            return existingDocument;
        }

        if (documentKind is LooseDocumentKind.MiscellaneousFileWithStandardReferences
            or LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors)
        {
            // Ensure the HasAllInformation of a forked canonical document is up to date
            var newHasAllInformation = documentKind == LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors;
            if (IsForkedCanonicalDocument(existingDocument)
                && existingDocument.Project.State.HasAllInformation != newHasAllInformation)
            {
                _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(
                    workspace => workspace.OnHasAllInformationChanged(existingDocument.Project.Id, newHasAllInformation));
                return _workspaceFactory.MiscellaneousFilesWorkspace.CurrentSolution.GetTextDocuments(documentUri).OfType<Document>().Single();
            }

            return existingDocument;
        }

        throw ExceptionUtilities.UnexpectedValue(documentKind);
    }

    public async ValueTask<(TextDocument document, bool alreadyExists)> GetOrAddMiscellaneousDocumentAsync(
        DocumentUri documentUri, string documentPath, SourceText documentText, LooseDocumentKind documentKind, LanguageInformation languageInformation, CancellationToken cancellationToken)
    {
        var documents = await _workspaceFactory.MiscellaneousFilesWorkspace.CurrentSolution.GetTextDocumentsAsync(documentUri, cancellationToken).ConfigureAwait(false);
        var miscDoc = documents.SingleOrDefault();
        if (miscDoc is { })
        {
            miscDoc = await TransitionToCanonicalForkedIfNeededAsync(documentPath, miscDoc, documentText, documentKind, cancellationToken);
            miscDoc = UpdateHasAllInformationIfNeeded(documentUri, miscDoc, documentKind);
            return (miscDoc, alreadyExists: true);
        }

        var newDoc = await AddMiscellaneousDocumentAsync(documentPath, documentText, documentKind, languageInformation, cancellationToken);
        return (newDoc, alreadyExists: false);
    }

    private async ValueTask<TextDocument> AddMiscellaneousDocumentAsync(
        string documentPath, SourceText documentText, LooseDocumentKind documentKind, LanguageInformation languageInformation, CancellationToken cancellationToken)
    {
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            if (documentKind == LooseDocumentKind.MiscellaneousFileWithNoReferences)
            {
                // Do not fork or load a canonical project, instead use a primordial misc files project which lacks references, etc.
                return AddPrimordialMiscProject_NoLock(loadedProjects, documentPath, documentText, languageInformation);
            }

            Contract.ThrowIfTrue(MiscellaneousFileUtilities.IsScriptFile(languageInformation, documentPath));
            var canonicalDocumentPath = _canonicalDocumentPath.Value;
            if (loadedProjects.TryGetValue(canonicalDocumentPath, out var canonicalLoadState))
            {
                Contract.ThrowIfFalse(canonicalLoadState is ProjectLoadState.Primordial or ProjectLoadState.LoadedTargets(LoadedProjectTargets: [_]));
                if (canonicalLoadState is ProjectLoadState.LoadedTargets)
                {
                    var canonicalProject = GetCanonicalProject();
                    Contract.ThrowIfNull(canonicalProject);
                    var hasAllInformation = documentKind == LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors;
                    return await AddForkedCanonicalProject_NoLockAsync(canonicalProject, loadedProjects, documentPath, documentText, hasAllInformation, cancellationToken);
                }
            }
            else
            {
                BeginLoadingCanonicalProject_NoLock();
            }

            // Not ready to fork the canonical project. Create a primordial project instead.
            return AddPrimordialMiscProject_NoLock(loadedProjects, documentPath, documentText, languageInformation);
        }, cancellationToken);
    }

    private async ValueTask<TextDocument> AddForkedCanonicalProject_NoLockAsync(Project canonicalProject, Dictionary<string, ProjectLoadState> loadedProjects, string documentPath, SourceText documentText, bool hasAllInformation, CancellationToken cancellationToken)
    {
        var newProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{documentPath}'");
        var newDocumentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(newProjectId),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        var forkedProjectInfo = await GetForkedProjectInfoAsync(canonicalProject, newDocumentInfo, hasAllInformation, cancellationToken);

        await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
        {
            workspace.OnProjectAdded(forkedProjectInfo);
        }, cancellationToken);
        loadedProjects.Add(documentPath, new ProjectLoadState.CanonicalForked(forkedProjectInfo.Id));

        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var addedDocument = miscWorkspace.CurrentSolution.GetRequiredDocument(newDocumentInfo.Id);
        return addedDocument;
    }

    internal async ValueTask<bool> IsCanonicalProjectLoadedAsync(CancellationToken cancellationToken)
    {
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            var canonicalDocumentPath = _canonicalDocumentPath.Value;
            return loadedProjects.TryGetValue(canonicalDocumentPath, out var loadState)
                && loadState is ProjectLoadState.LoadedTargets;
        }, cancellationToken);
    }

    /// <returns>The single document in the misc project.</returns>
    private Document AddPrimordialMiscProject_NoLock(Dictionary<string, ProjectLoadState> loadedProjects, string documentPath, SourceText documentText, LanguageInformation languageInformation)
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var sourceTextLoader = new SourceTextLoader(documentText, documentPath);
        var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            miscWorkspace, documentPath, sourceTextLoader, languageInformation, documentText.ChecksumAlgorithm, miscWorkspace.Services.SolutionServices, [], enableFileBasedPrograms);

        _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(projectInfo));
        loadedProjects.Add(documentPath, new ProjectLoadState.Primordial(_workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory, projectInfo.Id));

        var id = projectInfo.Documents.Single().Id;
        return miscWorkspace.CurrentSolution.GetRequiredDocument(id);
    }

    private void BeginLoadingCanonicalProject_NoLock()
    {
        // Create a canonical project in primordial state, then start a design-time build for it
        var canonicalDocumentPath = _canonicalDocumentPath.Value;
        var canonicalText = SourceText.From(string.Empty);
        var canonicalLoader = new SourceTextLoader(canonicalText, canonicalDocumentPath);

        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace,
            canonicalDocumentPath,
            canonicalLoader,
            new LanguageInformation(LanguageNames.CSharp, scriptExtension: null),
            canonicalText.ChecksumAlgorithm,
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace.Services.SolutionServices,
            metadataReferences: [],
            enableFileBasedPrograms: GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms));

        _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace =>
        {
            workspace.OnProjectAdded(projectInfo);
        });

        BeginLoadingProjectWithPrimordial_NoLock(
            canonicalDocumentPath,
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
            projectInfo.Id,
            doDesignTimeBuild: true);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string canonicalProjectPath, CancellationToken cancellationToken)
    {
        // This loader should only do a design time build on the canonical project
        Contract.ThrowIfFalse(canonicalProjectPath == _canonicalDocumentPath.Value);

        // Set the FileBasedProgram feature flag so that '#:' is permitted without errors in Miscellaneous Files With Standard References.
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
        var virtualProjectPath = VirtualProjectXmlProvider.GetVirtualProjectPath(canonicalProjectPath);

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

    /// <summary>
    /// Creates a new project based on the canonical project with a new document added.
    /// This should only be called when the canonical project is in the FullyLoaded state.
    /// </summary>
    private static async Task<ProjectInfo> GetForkedProjectInfoAsync(Project canonicalProject, DocumentInfo newDocumentInfo, bool hasAllInformation, CancellationToken cancellationToken)
    {
        var newDocumentPath = newDocumentInfo.FilePath;
        Contract.ThrowIfNull(newDocumentPath);

        var forkedProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{newDocumentPath}'");
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

    private Project? GetCanonicalProject()
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var project = miscWorkspace.CurrentSolution.Projects
            .SingleOrDefault(p => PathUtilities.Comparer.Equals(p.FilePath, _canonicalDocumentPath.Value));

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
