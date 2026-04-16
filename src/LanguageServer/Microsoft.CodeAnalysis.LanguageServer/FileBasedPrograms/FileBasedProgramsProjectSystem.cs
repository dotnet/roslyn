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
using Microsoft.CodeAnalysis.Shared.Utilities;
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
    private readonly CanonicalMiscellaneousFilesProjectProvider _canonicalProjectProvider;

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
        _canonicalProjectProvider = new CanonicalMiscellaneousFilesProjectProvider(workspaceFactory, loggerFactory);

        globalOptionService.AddOptionChangedHandler(this, OnGlobalOptionChanged);
    }

    public void Dispose()
    {
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
                _logger.LogDebug($"Detected enableFileBasedPrograms changed to '{value}'. Unloading loose file projects.");
                await UnloadAllProjectsAsync();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }

    private static string GetDocumentFilePath(DocumentUri uri) => uri.ParsedUri is { } parsedUri ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) : uri.UriString;

    private bool ClassifyAsMiscellaneousFileWithNoReferences(string filePath, LanguageInformation languageInformation)
    {
        // 2. Is `enableFileBasedPrograms` enabled?
        //    - No → Classify as Miscellaneous File With No References
        //    - Yes → Continue to next check
        var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
        if (!enableFileBasedPrograms)
        {
            return true;
        }

        // 3. Is the file a regular C# file? (i.e. not a `.csx` script, and not a file using a language besides C#)
        // - No → Classify as Miscellaneous File With No References
        // - Yes → Continue to next check
        if (languageInformation.LanguageName != LanguageNames.CSharp
            || MiscellaneousFileUtilities.IsScriptFile(languageInformation, filePath))
        {
            return true;
        }

        return false;
    }

    private async ValueTask<LooseDocumentKind> ClassifyDocumentAsync(string filePath, string languageId, CancellationToken cancellationToken)
    {
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(ProtocolConversions.CreateAbsoluteDocumentUri(filePath), languageId, out var languageInformation))
        {
            Contract.Fail($"Could not find language information for '{filePath}'");
        }

        // The design of this is described in docs/features/file-based-programs-vscode.md
        // Note: Step (1) is skipped, as we assume a first-chance lookup in the host workspace will handle this case.

        // Steps (2) and (3)
        if (ClassifyAsMiscellaneousFileWithNoReferences(filePath, languageInformation))
        {
            return LooseDocumentKind.MiscellaneousFileWithNoReferences;
        }

        // 4. Does the file have an absolute path and exist on disk? (i.e. it is not a "virtual document" created for a new, not-yet-saved file, or similar.)
        // - Yes → Go to (5)
        // - No → Classify as Miscellaneous File With Standard References
        if (!PathUtilities.IsAbsolute(filePath))
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;

        SourceText? sourceText = IOUtilities.PerformIO(() =>
        {
            // Note: SourceText.From eagerly reads the entire file
            using var fileStream = File.OpenRead(filePath);
            return SourceText.From(fileStream);
        });

        // File had an absolute path but we were unable to read it, due to it not existing or to some other I/O issue.
        if (sourceText is null)
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        // 5. Does the file have `#:` or `#!` directives?
        // - Yes → Classify as File-Based App. Restore if needed and show semantic errors.
        // - No → Continue to next check
        if (VirtualProjectXmlProvider.HasFileBasedAppDirectives(sourceText))
        {
            return LooseDocumentKind.FileBasedApp;
        }

        // 6. Is `enableFileBasedProgramsWhenAmbiguous` enabled? (default: `false` in release, `true` in prerelease)
        // - No → Classify as Miscellaneous File With Standard References
        // - Yes → Continue to heuristic detection

        if (!GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableSemanticErrorsInMiscellaneousFiles))
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        // Heuristic Detection:

        // 7. Are top-level statements present?
        // - No → Classify as Miscellaneous File With Standard References
        // - Yes → Continue to next check

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);
        var containsTopLevelStatements = syntaxTree.GetRoot(cancellationToken) is CompilationUnitSyntax compilationUnit && compilationUnit.Members.Any(SyntaxKind.GlobalStatement);
        if (!containsTopLevelStatements)
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        // 8. Is the file included in a `.csproj` cone?
        // - Yes → Classify as Miscellaneous File With Standard References (wait for project to load)
        // - No → Classify as Miscellaneous File With Standard References and Semantic Errors
        var csprojInConeChecker = _lspServices.GetRequiredService<CsprojInConeChecker>();
        if (csprojInConeChecker.IsContainedInCsprojCone(filePath))
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        return LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors;
    }

    public async ValueTask<TextDocument?> AddDocumentAsync(DocumentUri documentUri, TrackedDocumentInfo documentInfo)
    {
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(documentUri, documentInfo.LanguageId, out var languageInformation))
        {
            Contract.Fail($"Could not find language information for '{documentUri}'");
        }

        var documentFilePath = GetDocumentFilePath(documentUri);
        var sourceTextLoader = new SourceTextLoader(documentInfo.SourceText, documentFilePath);
        var doDesignTimeBuild = !ClassifyAsMiscellaneousFileWithNoReferences(documentFilePath, languageInformation);
        return await this.GetOrLoadEntryPointDocumentAsync(
            documentFilePath, sourceTextLoader, languageInformation, documentInfo.SourceText.ChecksumAlgorithm, doDesignTimeBuild);
    }

    /// <summary>
    /// Used to begin loading a file-based app project for a file-based app on disk, if it hasn't started already,
    /// when the caller doesn't need to use any results of the loading process.
    /// </summary>
    public async ValueTask TryBeginLoadingFileBasedAppAsync(string documentFilePath)
    {
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(documentFilePath));
        var sourceTextLoader = new WorkspaceFileTextLoader(_workspaceFactory.HostWorkspace.CurrentSolution.Services, documentFilePath, defaultEncoding: null);
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(ProtocolConversions.CreateAbsoluteDocumentUri(documentFilePath), lspLanguageId: "csharp", out var languageInformation))
        {
            Contract.Fail($"Could not find language information for '{documentFilePath}'");
        }

        await GetOrLoadEntryPointDocumentAsync(documentFilePath, sourceTextLoader, languageInformation, SourceHashAlgorithms.Default, doDesignTimeBuild: true);
    }

    public async ValueTask<TextDocument?> GetOrLoadEntryPointDocumentAsync(string documentFilePath, TextLoader textLoader, LanguageInformation languageInformation, SourceHashAlgorithm checksumAlgorithm, bool doDesignTimeBuild)
    {
        var project = await base.GetOrLoadProjectAsync(documentFilePath, _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory, CreatePrimordialProjectInfo, doDesignTimeBuild);
        return project is null ? null : LookupExistingDocument(project);

        TextDocument? LookupExistingDocument(Project project)
        {
            var document = project.Documents.FirstOrDefault(document => document.FilePath == documentFilePath)
                ?? project.AdditionalDocuments.FirstOrDefault(document => document.FilePath == documentFilePath);
            if (document is null)
            {
                _logger.LogWarning("Could not get a document for '{documentFilePath}' because its project doesn't contain a document for it", documentFilePath);
            }

            return document;
        }

        ProjectInfo CreatePrimordialProjectInfo(ProjectSystemProjectFactory projectFactory)
        {
            var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
            return MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                projectFactory.Workspace, documentFilePath, textLoader, languageInformation, checksumAlgorithm, projectFactory.Workspace.Services.SolutionServices, [], enableFileBasedPrograms);
        }
    }

    public async ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri)
    {
        // Note: we intentionally do not unload file-based apps in this path.
        // This is because we want to unload from the miscellaneous files workspace only, when a file is found in the host workspace.
        var documentPath = GetDocumentFilePath(uri);
        return await TryUnloadProjectAsync(documentPath, fromProjectFactory: _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory);
    }

    public async ValueTask CloseDocumentAsync(DocumentUri uri)
    {
        // If automatic discovery is enabled, we don't want to unload a file-based app upon closing a document.
        var unloadFromProjectFactory = GlobalOptionService.GetOption(FileBasedAppsOptionsStorage.EnableAutomaticDiscovery)
            ? _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory
            : null;

        var documentPath = GetDocumentFilePath(uri);
        await TryUnloadProjectAsync(documentPath, unloadFromProjectFactory);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string documentPath, CancellationToken cancellationToken)
    {
        // Note: we assume that if we made it this far, the document is for the C# language.
        var documentKind = await ClassifyDocumentAsync(documentPath, languageId: "csharp", cancellationToken);
        _logger.LogDebug("Classified '{documentPath}' as '{documentKind}'.", documentPath, documentKind);

        if (documentKind == LooseDocumentKind.MiscellaneousFileWithNoReferences)
        {
            // This might happen due to a race involving changes to option values.
            // Just don't proceed with the reload and assume the option change handler will unload this project if needed.
            _logger.LogWarning("A document classified as {documentKind} should not be design-time built.", documentKind);
            return null;
        }

        if (documentKind is LooseDocumentKind.MiscellaneousFileWithStandardReferences or LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors)
        {
            var projectInfos = await _canonicalProjectProvider.GetProjectInfoAsync(documentPath, cancellationToken).ConfigureAwait(false);
            return new RemoteProjectLoadResult
            {
                ProjectFileInfos = projectInfos,
                DiagnosticLogItems = [],
                // This points to the Canonical.csproj, which always exists on disk and can be restored regardless of SDK.
                ProjectRestorePath = projectInfos.FirstOrDefault()?.FilePath,
                ProjectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
                IsFileBasedProgram = false,
                IsMiscellaneousFile = true,
                HasAllInformation = documentKind is LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors,
                PreferredBuildHostKind = BuildHostProcessKind.NetCore,
                ActualBuildHostKind = BuildHostProcessKind.NetCore,
            };
        }

        // Fall through to ordinary file-based app handling.
        Contract.ThrowIfFalse(documentKind is LooseDocumentKind.FileBasedApp);

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
            ProjectFileInfos = await loadedFile.GetProjectFileInfosAsync(cancellationToken),
            DiagnosticLogItems = await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken),
            ProjectRestorePath = documentPath,
            ProjectFactory = _workspaceFactory.HostProjectFactory,
            IsFileBasedProgram = true,
            IsMiscellaneousFile = false,
            HasAllInformation = true,
            PreferredBuildHostKind = buildHostKind,
            ActualBuildHostKind = buildHostKind,
        };
    }
}
