// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.MSBuild.BuildHostProcessManager;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>Handles loading both miscellaneous files and file-based program projects.</summary>
internal sealed class FileBasedProgramsProjectSystem : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider
{
    private readonly ILspServices _lspServices;
    private readonly ILogger<FileBasedProgramsProjectSystem> _logger;
    private readonly VirtualProjectXmlProvider _projectXmlProvider;

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
        _lspServices = lspServices;
        _logger = loggerFactory.CreateLogger<FileBasedProgramsProjectSystem>();
        _projectXmlProvider = projectXmlProvider;
    }

    private string GetDocumentFilePath(DocumentUri uri) => uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;

    public async ValueTask<bool> IsMiscellaneousFilesDocumentAsync(TextDocument document, CancellationToken cancellationToken)
    {
        // There are two cases here: if it's a primordial document, it'll be in the MiscellaneousFilesWorkspace and thus we definitely know it's
        // a miscellaneous file. Otherwise, it might be a file-based program that we loaded in the main workspace; in this case, the project's path
        // is also the source file path, and that's what we consider the 'project' path that is loaded.
        return document.Project.Solution.Workspace == _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace ||
            document.Project.FilePath is not null && await IsProjectLoadedAsync(document.Project.FilePath, cancellationToken);
    }

    public async ValueTask<TextDocument?> AddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var documentFilePath = GetDocumentFilePath(uri);
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInformation))
        {
            Contract.Fail($"Could not find language information for {uri} with absolute path {documentFilePath}");
        }

        var primordialDoc = AddPrimordialDocument(uri, documentText, languageId);
        Contract.ThrowIfNull(primordialDoc.FilePath);

        var doDesignTimeBuild = uri.ParsedUri?.IsFile is true
            && languageInformation.LanguageName == LanguageNames.CSharp
            && GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
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

        var loader = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.CreateFileTextLoader(documentPath);
        var textAndVersion = await loader.LoadTextAsync(new LoadTextOptions(SourceHashAlgorithms.Default), cancellationToken);
        var isFileBasedProgram = VirtualProjectXmlProvider.IsFileBasedProgram(documentPath, textAndVersion.Text);

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, virtualProjectPath, dotnetPath: null, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(virtualProjectPath, virtualProjectContent, languageName: LanguageNames.CSharp, cancellationToken);

        return new RemoteProjectLoadResult
        {
            ProjectFile = loadedFile,
            // If it's a proper file based program, we'll put it in the main host workspace factory since we want cross-project references to work.
            // Otherwise, we'll keep it in miscellaneous files.
            ProjectFactory = isFileBasedProgram ? _workspaceFactory.HostProjectFactory : _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
            IsFileBasedProgram = isFileBasedProgram,
            IsMiscellaneousFile = !isFileBasedProgram,
            PreferredBuildHostKind = buildHostKind,
            ActualBuildHostKind = buildHostKind,
        };
    }

    protected override async ValueTask OnProjectUnloadedAsync(string projectFilePath)
    {
        await _projectXmlProvider.UnloadCachedDiagnosticsAsync(projectFilePath);
    }
}
