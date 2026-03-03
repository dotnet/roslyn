// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
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

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>Handles loading both miscellaneous files and file-based program projects.</summary>
internal sealed class FileBasedProgramsProjectSystem : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider, IDisposable
{
    private readonly ILspServices _lspServices;
    private readonly ILogger<FileBasedProgramsProjectSystem> _logger;
    private readonly VirtualProjectXmlProvider _projectXmlProvider;
    private readonly CanonicalMiscFilesProjectLoader _canonicalMiscFilesLoader;

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

        globalOptionService.AddOptionChangedHandler(this, OnGlobalOptionChanged);
    }

    public void Dispose()
    {
        _canonicalMiscFilesLoader.Dispose();
        GlobalOptionService.RemoveOptionChangedHandler(this, OnGlobalOptionChanged);
    }

    private void OnGlobalOptionChanged(object sender, object target, OptionChangedEventArgs args)
    {
        foreach (var (key, value) in args.ChangedOptions)
        {
            if (key.Option.Equals(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms))
            {
                // This event handler can't be async, so we ignore the resulting task here,
                // and take care that the ignored call doesn't throw an exception
                _ = HandleEnableFileBasedProgramsChangedAsync((bool)value!);
                break;
            }
        }

        async Task HandleEnableFileBasedProgramsChangedAsync(bool value)
        {
            using var token = Listener.BeginAsyncOperation(nameof(HandleEnableFileBasedProgramsChangedAsync));
            try
            {
                // Note: Changing the 'enableFileBasedPrograms' setting causes many subtle differences in how loose files are handled.
                // For example, loose files which don't look like file-based programs, are put in projects forked from the canonical project loader, only when the setting is enabled, etc.
                // We anticipate that changing this setting will be infrequent, and, the cost of needing to reload will be acceptable given that.
                _logger.LogInformation($"Detected enableFileBasedPrograms changed to '{value}'. Unloading loose file projects.");
                await UnloadAllProjectsAsync();
                await _canonicalMiscFilesLoader.UnloadAllProjectsAsync();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }

    private static string GetDocumentFilePath(DocumentUri uri) => uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;

    public bool ManagesWorkspace(Workspace workspace)
    {
        return workspace == _workspaceFactory.MiscellaneousFilesWorkspace;
    }

    public async ValueTask<(TextDocument document, bool alreadyExists)?> GetOrAddDocumentAsync(DocumentUri documentUri, TrackedDocumentInfo documentInfo, CancellationToken cancellationToken)
    {
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(documentUri, documentInfo.LanguageId, out var languageInformation))
        {
            Contract.Fail($"Could not find language information for '{documentUri}'");
        }

        var documentKind = await ClassifyDocumentAsync(documentUri, documentInfo.SourceText, languageInformation, cancellationToken);
        _logger.LogDebug("Classified '{documentUri}' as '{documentKind}'", documentUri, documentKind);
        UpdateHasAllInformationIfNeeded(documentUri, documentKind);
        return await GetOrLoadDocumentCoreAsync(documentUri, documentKind, documentInfo, languageInformation, cancellationToken);
    }

    private async ValueTask<LooseDocumentKind> ClassifyDocumentAsync(DocumentUri documentUri, SourceText sourceText, LanguageInformation languageInformation, CancellationToken cancellationToken)
    {
        // roslyn/docs/features/file-based-programs-vscode.md
        // Note: Step (1) is skipped, as we assume a first-chance lookup in the host workspace will handle this case.

        // 2. Is `enableFileBasedPrograms` enabled?
        //    - No → Classify as Miscellaneous File With No References
        //    - Yes → Continue to next check
        var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
        if (!enableFileBasedPrograms)
        {
            return LooseDocumentKind.MiscellaneousFileWithNoReferences;
        }

        // 3. Is the file a regular C# file? (i.e. not a `.csx` script, and not a file using a language besides C#)
        // - No → Classify as Miscellaneous File With No References
        // - Yes → Continue to next check
        var filePath = GetDocumentFilePath(documentUri);
        if (languageInformation.LanguageName != LanguageNames.CSharp
            || MiscellaneousFileUtilities.IsScriptFile(languageInformation, filePath))
        {
            return LooseDocumentKind.MiscellaneousFileWithNoReferences;
        }

        // 3. Does the file have an absolute path? (i.e. it represents a file on disk, and it is not a "virtual document" created for a new, not-yet-saved file, or similar.)
        // - Yes → Go to (4)
        // - No → Go to (5)

        // 4. Does the file have `#:` or `#!` directives?
        // - Yes → Classify as File-Based App. Restore if needed and show semantic errors.
        // - No → Continue to next check
        if (filePath is { }
            && PathUtilities.IsAbsolute(filePath)
            && VirtualProjectXmlProvider.HasFileBasedAppDirectives(sourceText))
        {
            return LooseDocumentKind.FileBasedApp;
        }

        // 5. Is `enableFileBasedProgramsWhenAmbiguous` enabled? (default: `false` in release, `true` in prerelease)
        // - No → Classify as Miscellaneous File With Standard References
        // - Yes → Continue to heuristic detection

        // Note: Option 'EnableFileBasedProgramsWhenAmbiguous' is confusingly named.
        // What it actually controls is whether to show semantic errors in miscellaneous files with top-level statements and no #: directives.
        if (!GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedProgramsWhenAmbiguous))
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        // Heuristic Detection:

        // 6. Are top-level statements present?
        // - No → Classify as Miscellaneous File With Standard References
        // - Yes → Continue to next check

        // Use an existing syntax tree from misc files workspace, if present.
        // Otherwise we will have to do a parse (unfortunately).
        var existingDoc = _workspaceFactory.MiscellaneousFilesWorkspace.CurrentSolution.GetTextDocuments(documentUri).OfType<Document>().FirstOrDefault();
        var syntaxTree = existingDoc is { } ? await existingDoc.GetSyntaxTreeAsync(cancellationToken) : null;
        syntaxTree ??= CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);

        var containsTopLevelStatements = syntaxTree.GetRoot(cancellationToken) is CompilationUnitSyntax compilationUnit
            && compilationUnit.Members.Any(SyntaxKind.GlobalStatement);
        if (!containsTopLevelStatements)
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        return LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors;
    }

    private async ValueTask<(TextDocument document, bool alreadyExists)?> GetOrLoadDocumentCoreAsync(DocumentUri documentUri, LooseDocumentKind documentKind, TrackedDocumentInfo documentInfo, LanguageInformation languageInformation, CancellationToken cancellationToken)
    {
        if (documentKind is LooseDocumentKind.FileBasedApp)
        {
            return (await BeginLoadingFileBasedAppAsync(), alreadyExists: false);
        }
        else if (documentKind is LooseDocumentKind.MiscellaneousFileWithNoReferences
            or LooseDocumentKind.MiscellaneousFileWithStandardReferences
            or LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors)
        {
            return await GetOrLoadMiscFileAsync();
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(documentKind);
        }

        async ValueTask<TextDocument> BeginLoadingFileBasedAppAsync()
        {
            Contract.ThrowIfFalse(documentKind is LooseDocumentKind.FileBasedApp);
            var documentFilePath = GetDocumentFilePath(documentUri);
            Contract.ThrowIfFalse(languageInformation.LanguageName == LanguageNames.CSharp && !MiscellaneousFileUtilities.IsScriptFile(languageInformation, documentFilePath));
            // Note: for simplicity, the file-based app projects are always put in the host workspace, even when in the primordial state.
            var workspace = _workspaceFactory.HostWorkspace;
            var sourceTextLoader = new SourceTextLoader(documentInfo.SourceText, documentFilePath);
            var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                workspace, documentFilePath, sourceTextLoader, languageInformation, documentInfo.SourceText.ChecksumAlgorithm, workspace.Services.SolutionServices, [], enableFileBasedPrograms: true);
            _workspaceFactory.HostProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(projectInfo));
            var id = projectInfo.Documents.Single().Id;
            var primordialDoc = workspace.CurrentSolution.GetRequiredDocument(id);
            await BeginLoadingProjectWithPrimordialAsync(documentFilePath, _workspaceFactory.HostProjectFactory, primordialProjectId: primordialDoc.Project.Id, doDesignTimeBuild: true);
            return primordialDoc;
        }

        async ValueTask<(TextDocument document, bool alreadyExists)> GetOrLoadMiscFileAsync()
        {
            Contract.ThrowIfFalse(documentKind is LooseDocumentKind.MiscellaneousFileWithNoReferences
                or LooseDocumentKind.MiscellaneousFileWithStandardReferences
                or LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors);
            var documents = await _workspaceFactory.MiscellaneousFilesWorkspace.CurrentSolution.GetTextDocumentsAsync(documentUri, cancellationToken).ConfigureAwait(false);
            var miscDoc = documents.SingleOrDefault();
            if (miscDoc is { })
                return (miscDoc, alreadyExists: true);

            var newDoc = await _canonicalMiscFilesLoader.AddMiscellaneousDocumentAsync(GetDocumentFilePath(documentUri), documentInfo.SourceText, documentKind, languageInformation, cancellationToken);
            return (newDoc, alreadyExists: false);
        }
    }

    private void UpdateHasAllInformationIfNeeded(DocumentUri documentUri, LooseDocumentKind documentKind)
    {
        var filePath = GetDocumentFilePath(documentUri);
        if (documentKind is LooseDocumentKind.FileBasedApp or LooseDocumentKind.MiscellaneousFileWithNoReferences)
        {
            // Nothing to do.
        }
        else if (documentKind is LooseDocumentKind.MiscellaneousFileWithStandardReferences
            or LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors)
        {
            // If the misc files project has references, ensure HasAllInformation is up to date
            var newHasAllInformation = documentKind == LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors;
            var miscDocument = _workspaceFactory.MiscellaneousFilesWorkspace.CurrentSolution.GetTextDocuments(documentUri).SingleOrDefault();
            if (miscDocument is { Project: { MetadataReferences: not [] } miscProject }
                && miscProject.State.HasAllInformation != newHasAllInformation)
            {
                _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(
                    workspace => workspace.OnHasAllInformationChanged(miscProject.Id, newHasAllInformation));
            }
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(documentKind);
        }
    }

    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri)
    {
        // Note: we intentionally do not unload file-based apps in this path.
        // This is because we want to unload from the miscellaneous files workspace only, when a file is found in the host workspace.
        var documentPath = GetDocumentFilePath(uri);
        return await _canonicalMiscFilesLoader.TryUnloadProjectAsync(documentPath);
    }

    public async ValueTask CloseDocumentAsync(DocumentUri uri)
    {
        var documentPath = GetDocumentFilePath(uri);
        await _canonicalMiscFilesLoader.TryUnloadProjectAsync(documentPath);
        await TryUnloadProjectAsync(documentPath);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string documentPath, CancellationToken cancellationToken)
    {
        // Check the host workspace to determine whether the entry point file is still a file-based app.
        // Note: the state of the file in the workspace may not precisely match the content on disk here.
        // We are assuming it will be accurate enough for purposes of this check.
        var solution = _workspaceFactory.HostWorkspace.CurrentSolution;
        var entryPointDocId = solution.GetDocumentIdsWithFilePath(documentPath).FirstOrDefault();
        var document = solution.GetDocument(entryPointDocId);
        if (document is null)
        {
            _logger.LogWarning($"Unable to find a document for entry point file, didn't already we begin loading it?");
            await TryUnloadProjectAsync(documentPath);
            return null;
        }

        if (!VirtualProjectXmlProvider.HasFileBasedAppDirectives(await document.GetTextAsync(cancellationToken)))
        {
            // The file has changed and is no longer a file-based app entry point.
            // Unload it from this project system and cancel the reload.
            await TryUnloadProjectAsync(documentPath);
            return null;
        }

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

    protected override async ValueTask TransitionPrimordialProjectToLoaded_NoLockAsync(
        Dictionary<string, ProjectLoadState> loadedProjects,
        string projectPath,
        ProjectLoadState.Primordial projectState,
        CancellationToken cancellationToken)
    {
        await projectState.PrimordialProjectFactory.ApplyChangeToWorkspaceAsync(
            workspace => workspace.OnProjectRemoved(projectState.PrimordialProjectId),
            cancellationToken);
    }
}
