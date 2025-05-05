// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Security;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class FileBasedProgramsProjectSystem : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider
{
    private readonly ILspServices _lspServices;
    private readonly ILogger<FileBasedProgramsProjectSystem> _logger;
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

    public FileBasedProgramsProjectSystem(
        ILspServices lspServices,
        IMetadataAsSourceFileService metadataAsSourceFileService,
        LanguageServerWorkspaceFactory workspaceFactory,
        IFileChangeWatcher fileChangeWatcher,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ProjectLoadTelemetryReporter projectLoadTelemetry,
        ServerConfigurationFactory serverConfigurationFactory,
        BinlogNamer binlogNamer)
            : base(
                workspaceFactory.FileBasedProgramsProjectFactory,
                workspaceFactory.TargetFrameworkManager,
                workspaceFactory.ProjectSystemHostInfo,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binlogNamer)
    {
        _lspServices = lspServices;
        _logger = loggerFactory.CreateLogger<FileBasedProgramsProjectSystem>();
        _metadataAsSourceFileService = metadataAsSourceFileService;
    }

    public Workspace Workspace => ProjectFactory.Workspace;

    public async Task<TextDocument?> AddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var (documentPath, isFile) = uri.ParsedUri is { } parsedUri
            ? (ProtocolConversions.GetDocumentFilePathFromUri(parsedUri), isFile: parsedUri.IsFile)
            : (uri.UriString, isFile: false);

        var container = documentText.Container;
        if (_metadataAsSourceFileService.TryAddDocumentToWorkspace(documentPath, container, out var documentId))
        {
            var metadataWorkspace = _metadataAsSourceFileService.TryGetWorkspace();
            Contract.ThrowIfNull(metadataWorkspace);
            var metadataDoc = metadataWorkspace.CurrentSolution.GetRequiredDocument(documentId);
            return metadataDoc;
        }

        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInformation))
        {
            // Only log here since throwing here could take down the LSP server.
            logger.LogError($"Could not find language information for {uri} with absolute path {documentPath}");
            return null;
        }

        if (!isFile || !GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms))
        {
            // For now, we cannot provide intellisense etc on files which are not on disk or are not C#.
            var sourceTextLoader = new SourceTextLoader(documentText, documentPath);
            var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                Workspace, documentPath, sourceTextLoader, languageInformation, documentText.ChecksumAlgorithm, Workspace.CurrentSolution.Services, []);
            await ProjectFactory.ApplyChangeToWorkspaceAsync(ws => ws.OnProjectAdded(projectInfo), cancellationToken: default);

            var newSolution = Workspace.CurrentSolution;
            if (languageInformation.LanguageName == "Razor")
            {
                var docId = projectInfo.AdditionalDocuments.Single().Id;
                return newSolution.GetRequiredAdditionalDocument(docId);
            }

            var id = projectInfo.Documents.Single().Id;
            return newSolution.GetRequiredDocument(id);
        }

        // We have a file on disk. Light up the file-based program experience.
        // For Razor files we need to override the language name to C# as that's what code is generated
        var isRazor = languageInformation.LanguageName == "Razor";
        var languageName = isRazor ? LanguageNames.CSharp : languageInformation.LanguageName;
        var documentFileInfo = new DocumentFileInfo(documentPath, logicalPath: documentPath, isLinked: false, isGenerated: false, folders: default);
        var projectFileInfo = new ProjectFileInfo()
        {
            Language = languageName,
            FilePath = VirtualProject.GetVirtualProjectPath(documentPath),
            CommandLineArgs = ["/langversion:preview", "/features:FileBasedProgram=true"],
            Documents = isRazor ? [] : [documentFileInfo],
            AdditionalDocuments = isRazor ? [documentFileInfo] : [],
            AnalyzerConfigDocuments = [],
            ProjectReferences = [],
            PackageReferences = [],
            ProjectCapabilities = [],
            ContentFilePaths = [],
            FileGlobs = []
        };

        var projectSet = AddLoadedProjectSet(documentPath);
        Project workspaceProject;
        using (await projectSet.Semaphore.DisposableWaitAsync())
        {
            var loadedProject = await this.CreateAndTrackInitialProjectAsync_NoLock(projectSet, documentPath, language: languageName);
            await loadedProject.UpdateWithNewProjectInfoAsync(projectFileInfo, hasAllInformation: false, _logger);

            ProjectsToLoadAndReload.AddWork(new ProjectToLoad(documentPath, ProjectGuid: null, ReportTelemetry: true));
            loadedProject.NeedsReload += (_, _) => ProjectsToLoadAndReload.AddWork(new ProjectToLoad(documentPath, ProjectGuid: null, ReportTelemetry: false));
            workspaceProject = ProjectFactory.Workspace.CurrentSolution.GetRequiredProject(loadedProject.ProjectId);
        }

        var document = isRazor ? workspaceProject.AdditionalDocuments.Single() : workspaceProject.Documents.Single();

        _ = Task.Run(async () =>
        {
            await ProjectsToLoadAndReload.WaitUntilCurrentBatchCompletesAsync();
            await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
        });

        Contract.ThrowIfFalse(document.FilePath == documentPath);
        return document;
    }

    public async ValueTask TryRemoveMiscellaneousDocumentAsync(DocumentUri uri, bool removeFromMetadataWorkspace)
    {
        var documentPath = uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;
        await TryUnloadProjectSetAsync(documentPath);

        // also do an unload in case this was the non-file scenario
        if (removeFromMetadataWorkspace && uri.ParsedUri is not null && _metadataAsSourceFileService.TryRemoveDocumentFromWorkspace(ProtocolConversions.GetDocumentFilePathFromUri(uri.ParsedUri)))
        {
            return;
        }

        var matchingDocument = Workspace.CurrentSolution.GetDocumentIds(uri).SingleOrDefault();
        if (matchingDocument != null)
        {
            var project = Workspace.CurrentSolution.GetRequiredProject(matchingDocument.ProjectId);
            Workspace.OnProjectRemoved(project.Id);
        }
    }

    protected override async Task<(RemoteProjectFile? projectFile, bool hasAllInformation, BuildHostProcessKind preferred, BuildHostProcessKind actual)> TryLoadProjectAsync(
        BuildHostProcessManager buildHostProcessManager, string documentPath, CancellationToken cancellationToken)
    {
        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, cancellationToken);
        Contract.ThrowIfFalse(Path.GetExtension(documentPath) == ".cs");

        var fakeProjectPath = VirtualProject.GetVirtualProjectPath(documentPath);
        var loader = ProjectFactory.CreateFileTextLoader(documentPath);
        var textAndVersion = await loader.LoadTextAsync(new LoadTextOptions(SourceHashAlgorithms.Default), cancellationToken: default);
        var (contentToLoad, isFileBasedProgram) = VirtualProject.MakeVirtualProjectContent(documentPath, textAndVersion.Text);

        var loadedFile = await buildHost.LoadProjectAsync(fakeProjectPath, contentToLoad, languageName: LanguageNames.CSharp, cancellationToken);
        return (loadedFile, hasAllInformation: isFileBasedProgram, preferred: buildHostKind, actual: buildHostKind);
    }
}
