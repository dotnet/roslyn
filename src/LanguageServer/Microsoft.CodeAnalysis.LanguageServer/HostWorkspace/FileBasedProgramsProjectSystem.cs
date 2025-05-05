// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Security;
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

    public async Task<TextDocument?> AddMiscellaneousDocumentAsync(Uri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var documentPath = ProtocolConversions.GetDocumentFilePathFromUri(uri);
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

        // For Razor files we need to override the language name to C# as thats what code is generated
        var isRazor = languageInformation.LanguageName == "Razor";
        var languageName = isRazor ? LanguageNames.CSharp : languageInformation.LanguageName;
        var loadedProject = await CreateAndTrackInitialProjectAsync(documentPath, language: languageName);
        var documentFileInfo = new DocumentFileInfo(documentPath, logicalPath: documentPath, isLinked: false, isGenerated: false, folders: default);
        var projectFileInfo = new ProjectFileInfo()
        {
            Language = languageName,
            FilePath = uri.IsFile ? VirtualProject.GetVirtualProjectPath(documentPath) : documentPath,
            CommandLineArgs = uri.IsFile ? ["/langversion:preview", "/features:FileBasedProgram=true"] : ["/langversion:preview"],
            Documents = isRazor ? [] : [documentFileInfo],
            AdditionalDocuments = isRazor ? [documentFileInfo] : [],
            AnalyzerConfigDocuments = [],
            ProjectReferences = [],
            PackageReferences = [],
            ProjectCapabilities = [],
            ContentFilePaths = [],
            FileGlobs = []
        };
        await loadedProject.UpdateWithNewProjectInfoAsync(projectFileInfo, _logger);
        var workspaceProject = ProjectFactory.Workspace.CurrentSolution.GetRequiredProject(loadedProject.ProjectId);
        var document = isRazor ? workspaceProject.AdditionalDocuments.Single() : workspaceProject.Documents.Single();

        if (uri.IsFile && languageInformation.LanguageName == LanguageNames.CSharp)
        {
            // light up the proper file-based program experience.
            ProjectsToLoadAndReload.AddWork(new ProjectToLoad(documentPath, ProjectGuid: null, ReportTelemetry: true));
            loadedProject.NeedsReload += (_, _) => ProjectsToLoadAndReload.AddWork(new ProjectToLoad(documentPath, ProjectGuid: null, ReportTelemetry: false));

            _ = Task.Run(async () =>
            {
                await ProjectsToLoadAndReload.WaitUntilCurrentBatchCompletesAsync();
                await ProjectInitializationHandler.SendProjectInitializationCompleteNotificationAsync();
            });
        }

        Contract.ThrowIfFalse(document.FilePath == documentPath);
        return document;
    }

    public void TryRemoveMiscellaneousDocument(Uri uri, bool removeFromMetadataWorkspace)
    {
        // support unloading
    }

    protected override async Task<(RemoteProjectFile? projectFile, BuildHostProcessKind preferred, BuildHostProcessKind actual)> TryLoadProjectAsync(
        BuildHostProcessManager buildHostProcessManager, ProjectToLoad projectToLoad, CancellationToken cancellationToken)
    {
        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, cancellationToken);
        var documentPath = projectToLoad.Path;
        Contract.ThrowIfFalse(Path.GetExtension(documentPath) == ".cs");

        var fakeProjectPath = VirtualProject.GetVirtualProjectPath(documentPath);
        var contentToLoad = VirtualProject.MakeVirtualProjectContent(documentPath);

        var loadedFile = await buildHost.LoadProjectAsync(fakeProjectPath, contentToLoad, languageName: LanguageNames.CSharp, cancellationToken);
        return (loadedFile, preferred: buildHostKind, actual: buildHostKind);
    }
}
