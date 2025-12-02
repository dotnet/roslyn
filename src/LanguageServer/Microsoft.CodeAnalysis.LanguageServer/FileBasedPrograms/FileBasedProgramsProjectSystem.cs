// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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

/// <summary>Handles loading both miscellaneous files and file-based program projects.</summary>
internal sealed class FileBasedProgramsProjectSystem : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider, IDisposable
{
    private readonly ILspServices _lspServices;
    private readonly ILogger<FileBasedProgramsProjectSystem> _logger;
    private readonly VirtualProjectXmlProvider _projectXmlProvider;
    private readonly CanonicalMiscFilesProjectLoader _canonicalMiscFilesLoader;

    public void Dispose()
    {
        _canonicalMiscFilesLoader.Dispose();
    }

    public FileBasedProgramsProjectSystem(
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
        _logger = loggerFactory.CreateLogger<FileBasedProgramsProjectSystem>();
        _projectXmlProvider = projectXmlProvider;
        _canonicalMiscFilesLoader = new CanonicalMiscFilesProjectLoader(
                workspaceFactory,
                fileChangeWatcher,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                projectLoadTelemetry,
                serverConfigurationFactory,
                binLogPathProvider,
                dotnetCliHelper);
    }

    private string GetDocumentFilePath(DocumentUri uri) => uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;

    public async ValueTask<bool> IsMiscellaneousFilesDocumentAsync(TextDocument textDocument, CancellationToken cancellationToken)
    {
        // There are a few cases here:
        //   1.  The document is a primordial document (either not loaded yet or doesn't support design time build) - it will be in the misc files workspace.
        //   2.  The document is loaded as a canonical misc file - these are always in the misc files workspace.
        //   3.  The document is loaded as a file based program - then it will be in the main workspace where the project path matches the source file path.

        // NB: The FileBasedProgramsProjectSystem uses the document file path (the on-disk path) as the projectPath in 'IsProjectLoadedAsync'.
        var isLoadedAsFileBasedProgram = textDocument.FilePath is { } filePath && await IsProjectLoadedAsync(filePath, cancellationToken);

        // If this document has a file-based program syntactic marker, but we aren't loading it in a file-based programs project,
        // we need the caller to remove and re-add this document, so that it gets put in a file-based programs project instead.
        // See the check in 'LspWorkspaceManager.GetLspDocumentInfoAsync', which removes a document based on 'IsMiscellaneousFilesDocumentAsync' result,
        // then calls 'GetLspDocumentInfoAsync' again for the same request.
        if (!isLoadedAsFileBasedProgram && VirtualProjectXmlProvider.IsFileBasedProgram(await textDocument.GetTextAsync(cancellationToken)))
            return false;

        if (textDocument.Project.Solution.Workspace == _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace)
        {
            // Do a check to determine if the misc project needs to be re-created with a new HasAllInformation flag value.
            if (!isLoadedAsFileBasedProgram
                && await _canonicalMiscFilesLoader.IsCanonicalProjectLoadedAsync(cancellationToken)
                && textDocument is Document document
                && await document.GetSyntaxTreeAsync(cancellationToken) is { } syntaxTree)
            {
                var newHasAllInformation = await VirtualProjectXmlProvider.ShouldReportSemanticErrorsInPossibleFileBasedProgramAsync(GlobalOptionService, syntaxTree, cancellationToken);
                if (newHasAllInformation != document.Project.State.HasAllInformation)
                {
                    // TODO: replace this method and the call site in LspWorkspaceManager,
                    // with a mechanism for "updating workspace state if needed" based on changes to a document.
                    // Perhaps this could be based on actually listening for changes to particular documents, rather than whenever an LSP request related to a document comes in.
                    // We should be able to do more incremental updates in more cases, rather than needing to throw things away and start over.
                    return false;
                }
            }

            return true;
        }

        if (isLoadedAsFileBasedProgram)
            return true;

        // Document is not managed by this project system. Caller should unload it.
        return false;
    }

    public async ValueTask<TextDocument?> AddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var documentFilePath = GetDocumentFilePath(uri);
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInformation))
        {
            Contract.Fail($"Could not find language information for {uri} with absolute path {documentFilePath}");
        }

        var supportsDesignTimeBuild = languageInformation.LanguageName == LanguageNames.CSharp
            && GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);

        // Check if this is a C# file that should use the canonical misc files loader
        if (supportsDesignTimeBuild)
        {
            // For virtual (non-file) URIs or non-file-based programs, use the canonical loader
            if (uri.ParsedUri is null || !uri.ParsedUri.IsFile || !VirtualProjectXmlProvider.IsFileBasedProgram(documentText))
            {
                return await _canonicalMiscFilesLoader.AddMiscellaneousDocumentAsync(documentFilePath, documentText, CancellationToken.None);
            }
        }

        // Use the original file-based programs logic
        var primordialDoc = AddPrimordialDocument(uri, documentText, languageId);
        Contract.ThrowIfNull(primordialDoc.FilePath);

        var doDesignTimeBuild = uri.ParsedUri?.IsFile is true && supportsDesignTimeBuild;
        await BeginLoadingProjectWithPrimordialAsync(primordialDoc.FilePath, _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory, primordialProjectId: primordialDoc.Project.Id, doDesignTimeBuild);

        return primordialDoc;

        TextDocument AddPrimordialDocument(DocumentUri uri, SourceText documentText, string languageId)
        {
            var workspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
            var sourceTextLoader = new SourceTextLoader(documentText, documentFilePath);
            var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                workspace, documentFilePath, sourceTextLoader, languageInformation, documentText.ChecksumAlgorithm, workspace.Services.SolutionServices, []);

            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(projectInfo));

            // https://github.com/dotnet/roslyn/pull/78267
            // Work around an issue where opening a Razor file in the misc workspace causes a crash.
            if (languageInformation.LanguageName == LanguageInfoProvider.RazorLanguageName)
            {
                var docId = projectInfo.AdditionalDocuments.Single().Id;
                return workspace.CurrentSolution.GetRequiredAdditionalDocument(docId);
            }

            var id = projectInfo.Documents.Single().Id;
            return workspace.CurrentSolution.GetRequiredDocument(id);
        }
    }

    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri)
    {
        var documentPath = GetDocumentFilePath(uri);
        // First try to remove from the canonical misc files loader if it was created
        var removedFromCanonical = await _canonicalMiscFilesLoader.TryRemoveMiscellaneousDocumentAsync(documentPath, CancellationToken.None);
        if (removedFromCanonical)
            return true;

        // Fall back to the file-based programs logic
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

    protected override ValueTask OnProjectUnloadedAsync(string projectFilePath)
    {
        return ValueTask.CompletedTask;
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
