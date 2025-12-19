// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
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
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Handles loading file-based program files (files with #! or #: directives).
/// This provider only handles files where <see cref="VirtualProjectXmlProvider.IsFileBasedProgram"/> is true.
/// </summary>
internal sealed class FileBasedProgramsMiscFilesProvider : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider, IDisposable
{
    private readonly ILspServices _lspServices;
    private readonly ILogger<FileBasedProgramsMiscFilesProvider> _logger;
    private readonly VirtualProjectXmlProvider _projectXmlProvider;

    public void Dispose()
    {
        // Nothing to dispose in this provider
    }

    public FileBasedProgramsMiscFilesProvider(
        ILspServices lspServices,
        VirtualProjectXmlProvider projectXmlProvider,
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
        _logger = loggerFactory.CreateLogger<FileBasedProgramsMiscFilesProvider>();
        _projectXmlProvider = projectXmlProvider;
    }

    private string GetDocumentFilePath(DocumentUri uri) => uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;

    public async ValueTask<bool> IsMiscellaneousFilesDocumentAsync(TextDocument textDocument, CancellationToken cancellationToken)
    {
        // File-based programs are loaded in the main workspace where the project path matches the source file path.
        // NB: The FileBasedProgramsMiscFilesProvider uses the document file path (the on-disk path) as the projectPath in 'IsProjectLoadedAsync'.
        var isLoadedAsFileBasedProgram = textDocument.FilePath is { } filePath && await IsProjectLoadedAsync(filePath, cancellationToken);

        if (isLoadedAsFileBasedProgram)
            return true;

        // If this is in the misc files workspace but is not loaded as a file-based program, it's not managed by this provider
        if (textDocument.Project.Solution.Workspace == _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace)
            return false;

        // Document is not managed by this project system.
        return false;
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

        // Only handle file-based programs (files with #! or #: directives)
        return VirtualProjectXmlProvider.IsFileBasedProgram(documentText);
    }

    public async ValueTask<TextDocument?> TryAddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var documentFilePath = GetDocumentFilePath(uri);
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

        // Only handle file-based programs
        if (!VirtualProjectXmlProvider.IsFileBasedProgram(documentText))
            return null;

        // Add primordial document and start loading the project
        var primordialDoc = AddPrimordialDocument(uri, documentText, languageId, documentFilePath, languageInformation);
        Contract.ThrowIfNull(primordialDoc.FilePath);

        var doDesignTimeBuild = uri.ParsedUri?.IsFile is true;
        await BeginLoadingProjectWithPrimordialAsync(primordialDoc.FilePath, _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory, primordialProjectId: primordialDoc.Project.Id, doDesignTimeBuild);

        return primordialDoc;
    }

    private TextDocument AddPrimordialDocument(DocumentUri uri, SourceText documentText, string languageId, string documentFilePath, LanguageInformation languageInformation)
    {
        var workspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var sourceTextLoader = new SourceTextLoader(documentText, documentFilePath);
        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            workspace, documentFilePath, sourceTextLoader, languageInformation, documentText.ChecksumAlgorithm, workspace.Services.SolutionServices, []);

        _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(projectInfo));

        var id = projectInfo.Documents.Single().Id;
        return workspace.CurrentSolution.GetRequiredDocument(id);
    }

    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri)
    {
        var documentPath = GetDocumentFilePath(uri);
        return await TryUnloadProjectAsync(documentPath);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string documentPath, CancellationToken cancellationToken)
    {
        var content = await _projectXmlProvider.GetVirtualProjectContentAsync(documentPath, _logger, cancellationToken);
        if (content is not var (virtualProjectContent, diagnostics))
        {
            // https://github.com/dotnet/roslyn/issues/78618: falling back to this until dotnet run-api is more widely available
            _logger.LogInformation($"Failed to obtain virtual project for '{documentPath}' using dotnet run-api. Falling back to directly creating the virtual project.");
            virtualProjectContent = VirtualProjectXmlProvider.MakeVirtualProjectContent_DirectFallback(documentPath);
            diagnostics = [];
        }

        foreach (var diagnostic in diagnostics)
        {
            _logger.LogError($"{diagnostic.Location.Path}{diagnostic.Location.Span.Start}: {diagnostic.Message}");
        }

        // When loading a virtual project, the path to the on-disk source file is not used. Instead the path is adjusted to end with .csproj.
        // This is necessary in order to get msbuild to apply the standard c# props/targets to the project.
        var virtualProjectPath = VirtualProjectXmlProvider.GetVirtualProjectPath(documentPath);
        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, virtualProjectPath, dotnetPath: null, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(virtualProjectPath, virtualProjectContent, languageName: LanguageNames.CSharp, cancellationToken);

        return new RemoteProjectLoadResult
        {
            ProjectFile = loadedFile,
            // If we have made it this far, we must have determined that the document is a file-based program.
            // TODO: we should assert this somehow. However, we cannot use the on-disk state of the file to do so, because the decision to load this as a file-based program was based on in-editor content.
            ProjectFactory = _workspaceFactory.HostProjectFactory,
            IsFileBasedProgram = true,
            IsMiscellaneousFile = false,
            PreferredBuildHostKind = buildHostKind,
            ActualBuildHostKind = buildHostKind,
        };
    }

    protected override async ValueTask OnProjectUnloadedAsync(string projectFilePath)
    {
    }

    protected override async ValueTask TransitionPrimordialProjectToLoaded_NoLockAsync(
        string projectPath,
        ProjectSystemProjectFactory primordialProjectFactory,
        ProjectId primordialProjectId,
        CancellationToken cancellationToken)
    {
        await primordialProjectFactory.ApplyChangeToWorkspaceAsync(
            workspace => workspace.OnProjectRemoved(primordialProjectId),
            cancellationToken);
    }
}
