// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Security;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
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

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>Handles loading both miscellaneous files and file-based program projects.</summary>
internal sealed class FileBasedProgramsProjectSystem : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider
{
    private readonly ILspServices _lspServices;
    private readonly ILogger<FileBasedProgramsProjectSystem> _logger;
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly VirtualProjectXmlProvider _projectXmlProvider;

    public FileBasedProgramsProjectSystem(
        ILspServices lspServices,
        IMetadataAsSourceFileService metadataAsSourceFileService,
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
                workspaceFactory.FileBasedProgramsProjectFactory,
                workspaceFactory.TargetFrameworkManager,
                workspaceFactory.ProjectSystemHostInfo,
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
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _projectXmlProvider = projectXmlProvider;
    }

    public Workspace Workspace => ProjectFactory.Workspace;

    private string GetDocumentFilePath(DocumentUri uri) => uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;

    public async ValueTask<TextDocument?> AddMiscellaneousDocumentAsync(DocumentUri uri, SourceText documentText, string languageId, ILspLogger logger)
    {
        var documentFilePath = GetDocumentFilePath(uri);

        // https://github.com/dotnet/roslyn/issues/78421: MetadataAsSource should be its own workspace
        if (_metadataAsSourceFileService.TryAddDocumentToWorkspace(documentFilePath, documentText.Container, out var documentId))
        {
            var metadataWorkspace = _metadataAsSourceFileService.TryGetWorkspace();
            Contract.ThrowIfNull(metadataWorkspace);
            return metadataWorkspace.CurrentSolution.GetRequiredDocument(documentId);
        }

        var primordialDoc = AddPrimordialDocument(uri, documentText, languageId);
        Contract.ThrowIfNull(primordialDoc.FilePath);

        var doDesignTimeBuild = uri.ParsedUri?.IsFile is true
            && primordialDoc.Project.Language == LanguageNames.CSharp
            && GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
        await BeginLoadingProjectWithPrimordialAsync(primordialDoc.FilePath, primordialProjectId: primordialDoc.Project.Id, doDesignTimeBuild);

        return primordialDoc;

        TextDocument AddPrimordialDocument(DocumentUri uri, SourceText documentText, string languageId)
        {
            var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
            if (!languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInformation))
            {
                Contract.Fail($"Could not find language information for {uri} with absolute path {documentFilePath}");
            }

            var workspace = Workspace;
            var sourceTextLoader = new SourceTextLoader(documentText, documentFilePath);
            var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                workspace, documentFilePath, sourceTextLoader, languageInformation, documentText.ChecksumAlgorithm, workspace.Services.SolutionServices, []);

            ProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(projectInfo));

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

    public async ValueTask TryRemoveMiscellaneousDocumentAsync(DocumentUri uri, bool removeFromMetadataWorkspace)
    {
        var documentPath = GetDocumentFilePath(uri);
        if (removeFromMetadataWorkspace && _metadataAsSourceFileService.TryRemoveDocumentFromWorkspace(documentPath))
        {
            return;
        }

        await UnloadProjectAsync(documentPath);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string documentPath, CancellationToken cancellationToken)
    {
        var content = await _projectXmlProvider.GetVirtualProjectContentAsync(documentPath, cancellationToken);
        if (content is not var (virtualProjectContent, diagnostics))
        {
            // https://github.com/dotnet/roslyn/issues/78618: falling back to this until dotnet run-api is more widely available
            _logger.LogInformation($"Failed to obtain virtual project for '{documentPath}' using dotnet run-api. Falling back to directly creating the virtual project.");
            virtualProjectContent = VirtualProjectXmlProvider.MakeVirtualProjectContent_DirectFallback(documentPath);
            diagnostics = [];
        }

        foreach (var diagnostic in diagnostics)
        {
            // https://github.com/dotnet/roslyn/issues/78688: Surface diagnostics in editor
            _logger.LogError($"{diagnostic.Location.Path}{diagnostic.Location.Span.Start}: {diagnostic.Message}");
        }

        // When loading a virtual project, the path to the on-disk source file is not used. Instead the path is adjusted to end with .csproj.
        // This is necessary in order to get msbuild to apply the standard c# props/targets to the project.
        var virtualProjectPath = VirtualProjectXmlProvider.GetVirtualProjectPath(documentPath);

        var loader = ProjectFactory.CreateFileTextLoader(documentPath);
        var textAndVersion = await loader.LoadTextAsync(new LoadTextOptions(SourceHashAlgorithms.Default), cancellationToken);
        var isFileBasedProgram = VirtualProjectXmlProvider.IsFileBasedProgram(documentPath, textAndVersion.Text);

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(virtualProjectPath, virtualProjectContent, languageName: LanguageNames.CSharp, cancellationToken);
        return new RemoteProjectLoadResult(loadedFile, HasAllInformation: isFileBasedProgram, Preferred: buildHostKind, Actual: buildHostKind);
    }
}
