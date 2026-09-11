// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.FileBasedPrograms;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Options;
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
internal sealed class FileBasedProgramsProjectSystem : LanguageServerProjectLoader, ILspMiscellaneousFilesWorkspaceProvider
{
    private readonly ILspServices _lspServices;
    private readonly ILogger<FileBasedProgramsProjectSystem> _logger;
    private readonly CanonicalMiscellaneousFilesProjectProvider _canonicalProjectProvider;
    private readonly IFileBasedProgramService _fileBasedProgramService;

    /// <summary>Protects all file-based project graph state below.</summary>
    private readonly object _projectGraphGate = new();

    /// <summary>Files currently open in the LSP client.</summary>
    private readonly HashSet<string> _openDocumentPaths = new(PathUtilities.Comparer);

    /// <summary>
    /// Entry points
    /// - discovered by <see cref="FileBasedProgramsEntryPointDiscovery"/>, or
    /// - explicitly opened and classified as file-based app by <see cref="ClassifyDocumentAsync"/>.
    /// </summary>
    private readonly HashSet<string> _rootPaths = new(PathUtilities.Comparer);

    /// <summary>Evaluated #:ref edges keyed by the referencing project path.</summary>
    private readonly Dictionary<string, ImmutableHashSet<string>> _referencesByProjectPath = new(PathUtilities.Comparer);

    /// <summary>Projects currently reachable from any root.</summary>
    private readonly HashSet<string> _reachablePaths = new(PathUtilities.Comparer);

    /// <summary>Reachable projects that must bypass entry-point classification.</summary>
    private readonly HashSet<string> _referencedProjectPaths = new(PathUtilities.Comparer);

    /// <summary>One-shot project data produced while loading another project in the same #:ref graph.</summary>
    private readonly Dictionary<string, PreparedProjectLoad> _preparedProjectLoads = new(PathUtilities.Comparer);

    /// <summary>Projects to reload once after their restore attempt completes.</summary>
    private readonly HashSet<string> _pendingPostRestoreReloadPaths = new(PathUtilities.Comparer);

    /// <summary>Projects that already reloaded after their latest restore attempt.</summary>
    private readonly HashSet<string> _postRestoreReloadAttemptedPaths = new(PathUtilities.Comparer);

    /// <summary>Serializes aggregate graph loads that share one MSBuild project collection.</summary>
    private readonly SemaphoreSlim _projectGraphLoadGate = new(initialCount: 1);

    /// <summary>Changes whenever reconciliation may need to recheck the desired load state.</summary>
    private long _projectGraphVersion;

    /// <summary>
    /// Virtual (in-memory) projects don't exist on disk, so MSBuild worker nodes
    /// can't re-evaluate them. Force single-node builds to keep everything in-process.
    /// </summary>
    protected override int MaxNodeCount => 1;

    public FileBasedProgramsProjectSystem(
        ILspServices lspServices,
        IGlobalOptionService globalOptionService,
        ILoggerFactory loggerFactory,
        IAsynchronousOperationListenerProvider listenerProvider,
        ServerConfigurationFactory serverConfigurationFactory,
        IBinLogPathProvider binLogPathProvider,
        DotnetCliHelper dotnetCliHelper)
            : base(
                lspServices,
                globalOptionService,
                loggerFactory,
                listenerProvider,
                serverConfigurationFactory,
                binLogPathProvider,
                dotnetCliHelper)
    {
        _lspServices = lspServices;
        _logger = loggerFactory.CreateLogger<FileBasedProgramsProjectSystem>();
        _canonicalProjectProvider = new CanonicalMiscellaneousFilesProjectProvider(lspServices.GetRequiredService<IHostWorkspaceProvider>(), loggerFactory);
        _fileBasedProgramService = _workspaceFactory.HostWorkspace.Services.GetRequiredService<IFileBasedProgramService>();

        globalOptionService.AddOptionChangedHandler(this, OnGlobalOptionChanged);
    }

    public override ValueTask DisposeAsync()
    {
        GlobalOptionService.RemoveOptionChangedHandler(this, OnGlobalOptionChanged);

        lock (_projectGraphGate)
        {
            _openDocumentPaths.Clear();
            _rootPaths.Clear();
            _referencesByProjectPath.Clear();
            _reachablePaths.Clear();
            _referencedProjectPaths.Clear();
            _preparedProjectLoads.Clear();
            _pendingPostRestoreReloadPaths.Clear();
            _postRestoreReloadAttemptedPaths.Clear();
        }

        return base.DisposeAsync();
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

                if (!value)
                {
                    lock (_projectGraphGate)
                    {
                        _rootPaths.Clear();
                        _referencesByProjectPath.Clear();
                        _reachablePaths.Clear();
                        _referencedProjectPaths.Clear();
                        _preparedProjectLoads.Clear();
                        _pendingPostRestoreReloadPaths.Clear();
                        _postRestoreReloadAttemptedPaths.Clear();
                        _projectGraphVersion++;
                    }
                }

                await UnloadAllProjectsAsync();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }

    private static string GetDocumentFilePath(DocumentUri uri) => uri.GetDocumentFilePathFromUri();

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

    private async ValueTask<LooseDocumentKind> ClassifyDocumentAsync(
        string filePath, string languageId, SourceText? sourceText, CancellationToken cancellationToken)
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

        sourceText ??= IOUtilities.PerformIO(() =>
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

        var parseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]);
        var tokenizer = SyntaxFactory.CreateTokenParser(sourceText, parseOptions);
        var result = tokenizer.ParseLeadingTrivia();
        var leadingTrivia = result.Token.LeadingTrivia;

        // 5. Does the file have '#!' directives?
        // - Yes → Classify as File-Based App. Restore if needed and show semantic errors.
        // - No → Continue to next check
        if (leadingTrivia.Any(SyntaxKind.ShebangDirectiveTrivia))
        {
            return LooseDocumentKind.FileBasedApp;
        }

        // 6. Does the file have `#:` directives?
        // - No → Go to (8)
        // - Yes → Continue to next check
        if (leadingTrivia.Any(SyntaxKind.IgnoredDirectiveTrivia))
        {
            // 7. Does the file have top-level statements?
            // - Yes → Classify as File-Based App. Restore if needed and show semantic errors.
            // - No → Classify as Miscellaneous File With Standard References
            if (ContainsTopLevelStatements())
            {
                return LooseDocumentKind.FileBasedApp;
            }

            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        // 8. Is `enableFileBasedProgramsWhenAmbiguous` enabled? (default: `false` in release, `true` in prerelease)
        // - No → Classify as Miscellaneous File With Standard References
        // - Yes → Continue to heuristic detection

        if (!GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableSemanticErrorsInMiscellaneousFiles))
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        // Heuristic Detection:

        // 9. Are top-level statements present?
        // - No → Classify as Miscellaneous File With Standard References
        // - Yes → Continue to next check

        if (!ContainsTopLevelStatements())
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        // 10. Is the file included in a `.csproj` cone?
        // - Yes → Classify as Miscellaneous File With Standard References (wait for project to load)
        // - No → Classify as Miscellaneous File With Standard References and Semantic Errors
        var csprojInConeChecker = _lspServices.GetRequiredService<CsprojInConeChecker>();
        if (csprojInConeChecker.IsContainedInCsprojCone(filePath))
        {
            return LooseDocumentKind.MiscellaneousFileWithStandardReferences;
        }

        return LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors;

        bool ContainsTopLevelStatements()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, options: parseOptions, cancellationToken: cancellationToken);
            return syntaxTree.GetRoot(cancellationToken) is CompilationUnitSyntax compilationUnit && compilationUnit.Members.Any(SyntaxKind.GlobalStatement);
        }
    }

    public async ValueTask OpenDocumentAsync(DocumentUri documentUri, TrackedDocumentInfo documentInfo)
    {
        var documentFilePath = GetDocumentFilePath(documentUri);
        lock (_projectGraphGate)
            _openDocumentPaths.Add(documentFilePath);

        if (!IsReferencedProject(documentFilePath))
            return;

        if (await ClassifyDocumentAsync(documentFilePath, documentInfo.LanguageId, documentInfo.SourceText, CancellationToken.None) == LooseDocumentKind.FileBasedApp)
            RegisterRoot(documentFilePath);
    }

    public async ValueTask<TextDocument?> AddDocumentAsync(DocumentUri documentUri, TrackedDocumentInfo? documentInfo)
    {
        if (documentInfo is null && documentUri.ParsedDocumentUri?.IsFile != true)
            return null;

        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(documentUri, documentInfo?.LanguageId, out var languageInformation))
        {
            // Requests are invalid when the client specifies an unsupported language, or when it omits the language
            // and we cannot infer one from the URI path.
            Contract.Fail($"Could not find language information for '{documentUri}'");
        }

        var documentFilePath = GetDocumentFilePath(documentUri);
        if (documentInfo is null)
        {
            Contract.ThrowIfFalse(documentUri.ParsedDocumentUri?.IsFile == true);

            var projectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;
            var projectInfo = CreateMiscellaneousProjectInfo(projectFactory.CreateFileTextLoader(documentFilePath), SourceHashAlgorithms.Default);
            var solution = projectFactory.Workspace.CurrentSolution.AddProject(projectInfo);
            return solution.GetTextDocuments(documentUri).Single();
        }

        var sourceTextLoader = new SourceTextLoader(documentInfo.Value.SourceText, documentFilePath);
        var doDesignTimeBuild = !ClassifyAsMiscellaneousFileWithNoReferences(documentFilePath, languageInformation);
        var documents = await this.GetOrLoadEntryPointDocumentAsync(
            documentFilePath, sourceTextLoader, languageInformation, documentInfo.Value.SourceText.ChecksumAlgorithm, doDesignTimeBuild);

        ProjectInfo CreateMiscellaneousProjectInfo(TextLoader textLoader, SourceHashAlgorithm checksumAlgorithm)
        {
            var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
            var projectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;
            return MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                projectFactory.Workspace, documentFilePath, textLoader, languageInformation, checksumAlgorithm, projectFactory.Workspace.Services.SolutionServices, [], enableFileBasedPrograms);
        }

        // It's possible that when we searched for the document when dispatching this LSP request, there were no documents, but by the time we got here,
        // the document was a file-based app that we loaded in the background and potentially could have more tha one target. In this case, since we're just dispatching
        // a request and it's expected to be a misc files experience, any project can be picked here.
        return documents.FirstOrDefault();
    }

    /// <summary>
    /// Used to begin loading a file-based app project for a file-based app on disk, if it hasn't started already,
    /// when the caller doesn't need to use any results of the loading process.
    /// </summary>
    public async ValueTask TryBeginLoadingFileBasedAppAsync(string documentFilePath)
    {
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(documentFilePath));
        RegisterRoot(documentFilePath);

        var sourceTextLoader = new WorkspaceFileTextLoader(_workspaceFactory.HostWorkspace.CurrentSolution.Services, documentFilePath, defaultEncoding: null);
        var languageInfoProvider = _lspServices.GetRequiredService<ILanguageInfoProvider>();
        if (!languageInfoProvider.TryGetLanguageInformation(ProtocolConversions.CreateAbsoluteDocumentUri(documentFilePath), lspLanguageId: "csharp", out var languageInformation))
        {
            Contract.Fail($"Could not find language information for '{documentFilePath}'");
        }

        await GetOrLoadEntryPointDocumentAsync(documentFilePath, sourceTextLoader, languageInformation, SourceHashAlgorithms.Default, doDesignTimeBuild: true);
    }

    public async ValueTask<ImmutableArray<TextDocument>> GetOrLoadEntryPointDocumentAsync(string documentFilePath, TextLoader textLoader, LanguageInformation languageInformation, SourceHashAlgorithm checksumAlgorithm, bool doDesignTimeBuild)
    {
        documentFilePath = NormalizeProjectPath(documentFilePath);
        var projects = await base.GetOrLoadProjectAsync(documentFilePath, _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory, CreatePrimordialProjectInfo, doDesignTimeBuild);
        return projects.Select(p => LookupExistingDocument(p)).WhereNotNull().ToImmutableArray();

        TextDocument? LookupExistingDocument(Project project)
        {
            var document = project.Documents.FirstOrDefault(document => PathUtilities.Comparer.Equals(document.FilePath, documentFilePath))
                ?? project.AdditionalDocuments.FirstOrDefault(document => PathUtilities.Comparer.Equals(document.FilePath, documentFilePath));
            if (document is null)
            {
                _logger.LogWarning("Could not get a document for '{documentFilePath}' because its project doesn't contain a document for it", documentFilePath);
            }

            return document;
        }

        ProjectInfo CreatePrimordialProjectInfo(ProjectSystemProjectFactory projectFactory, string normalizedDocumentFilePath)
        {
            var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
            return MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
                projectFactory.Workspace, normalizedDocumentFilePath, textLoader, languageInformation, checksumAlgorithm, projectFactory.Workspace.Services.SolutionServices, [], enableFileBasedPrograms);
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
        var documentPath = GetDocumentFilePath(uri);
        lock (_projectGraphGate)
            _openDocumentPaths.Remove(documentPath);

        if (GlobalOptionService.GetOption(FileBasedAppsOptionsStorage.EnableAutomaticDiscovery))
        {
            await TryUnloadProjectAsync(documentPath, _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory);
            return;
        }

        var graphUpdate = RemoveRoot(documentPath);
        if (!graphUpdate.WasReachable)
        {
            await ReconcileProjectAsync(documentPath);
            return;
        }

        await ReconcileProjectsAsync(graphUpdate.ProjectsToReconcile);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string documentPath, CancellationToken cancellationToken)
    {
        // Note: we assume that if we made it this far, the document is for the C# language.
        var isReferencedProject = IsReferencedProject(documentPath);
        var documentKind = isReferencedProject
            ? LooseDocumentKind.FileBasedApp
            : await ClassifyDocumentAsync(documentPath, languageId: "csharp", sourceText: null, cancellationToken);
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

            // Note: We might enter this path when loading a file-based app with no directives.
            // i.e. whether a file with no directives in it, depends on how user is using the file.
            // The project system doesn't determine this with 100% certainty and instead just ensures we provide semantic info which is satisfactory for the 99% case.
            // For telemetry purposes, we will consider this file a file-based app, if we see that build artifacts exist for it in the default location.
            // This implies that the user used a command like `dotnet run app.cs` with it recently.
            var isFileBasedProgram = PathUtilities.IsAbsolute(documentPath)
                && _workspaceFactory.HostWorkspace.Services.GetService<IFileBasedProgramService>() is { } fileBasedProgramService
                && Directory.Exists(fileBasedProgramService.GetArtifactsPath(documentPath));

            return new RemoteProjectLoadResult
            {
                ProjectFileInfos = projectInfos,
                DiagnosticLogItems = [],
                // This points to the Canonical.csproj, which always exists on disk and can be restored regardless of SDK.
                ProjectRestorePath = projectInfos.FirstOrDefault()?.FilePath,
                ProjectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
                IsFileBasedProgram = isFileBasedProgram,
                HasFileBasedAppDirectives = false,
                IsMiscellaneousFile = true,
                HasAllInformation = documentKind is LooseDocumentKind.MiscellaneousFileWithStandardReferencesAndSemanticErrors,
                PreferredBuildHostKind = BuildHostProcessKind.NetCore,
                ActualBuildHostKind = BuildHostProcessKind.NetCore,
            };
        }

        // Fall through to ordinary file-based app handling.
        Contract.ThrowIfFalse(documentKind is LooseDocumentKind.FileBasedApp);
        if (!isReferencedProject && IsDocumentOpen(documentPath))
            RegisterRoot(documentPath);

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        if (TryTakePreparedProjectLoad(documentPath, out var preparedLoad))
        {
            return CreateProjectLoadResult(
                preparedLoad.Result,
                projectRestorePath: documentPath,
                preparedLoad.ProjectGraphLoads);
        }

        FileBasedProgramsProjectLoader.ProjectGraphLoadResult graphLoadResults;
        using (await _projectGraphLoadGate.DisposableWaitAsync(cancellationToken))
        {
            var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, documentPath, dotnetPath: null, cancellationToken);
            graphLoadResults = await FileBasedProgramsProjectLoader.LoadFileBasedAppProjectGraphAsync(
                buildHost,
                _fileBasedProgramService,
                documentPath,
                (error) => _logger.LogError(error),
                cancellationToken);
        }

        return CreateProjectLoadResult(
            graphLoadResults.Root,
            documentPath,
            graphLoadResults.ReferencedProjects);

        RemoteProjectLoadResult CreateProjectLoadResult(
            FileBasedProgramsProjectLoader.ProjectLoadResult loadResult,
            string? projectRestorePath,
            ImmutableArray<FileBasedProgramsProjectLoader.ProjectLoadResult> preparedProjectLoads = default)
        {
            return new()
            {
                ProjectFileInfos = loadResult.ProjectFileInfos,
                DiagnosticLogItems = loadResult.DiagnosticLogItems,
                ProjectRestorePath = projectRestorePath,
                ProjectFactory = _workspaceFactory.HostProjectFactory,
                IsFileBasedProgram = true,
                HasFileBasedAppDirectives = true,
                IsMiscellaneousFile = false,
                HasAllInformation = true,
                PreferredBuildHostKind = buildHostKind,
                ActualBuildHostKind = buildHostKind,
                PreparedProjectLoads = preparedProjectLoads,
            };
        }
    }

    protected override async ValueTask OnProjectLoadedAsync(
        string projectPath, RemoteProjectLoadResult projectLoadResult, bool needsRestore, CancellationToken cancellationToken)
    {
        if (!projectLoadResult.IsFileBasedProgram || projectLoadResult.IsMiscellaneousFile)
        {
            var graphUpdate = RemoveProjectFromGraph(projectPath);
            await ReconcileProjectsAsync(graphUpdate.ProjectsToReconcile, projectPath);
            return;
        }

        if (!IsReachable(projectPath))
        {
            RemovePreparedProjectLoadsOwnedBy(projectPath);
            await ReconcileProjectAsync(projectPath);
            return;
        }

        var referencedPaths = projectLoadResult.ProjectFileInfos
            .SelectMany(static projectInfo => projectInfo.ProjectReferences)
            .Select(static projectReference => projectReference.Path)
            .Where(path => PathUtilities.IsAbsolute(path) && _fileBasedProgramService.IsValidEntryPointPath(path))
            .ToImmutableHashSet(PathUtilities.Comparer);

        var update = UpdateProjectReferences(projectPath, referencedPaths);
        if (!update.ReferencesApplied)
        {
            await ReconcileProjectAsync(projectPath);
            return;
        }

        if (needsRestore &&
            !projectLoadResult.PreparedProjectLoads.IsDefaultOrEmpty &&
            GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableAutomaticRestore))
        {
            await ReconcileProjectsAsync(update.ProjectsToUnload);

            lock (_projectGraphGate)
            {
                if (!_postRestoreReloadAttemptedPaths.Contains(projectPath))
                    _pendingPostRestoreReloadPaths.Add(projectPath);
            }

            return;
        }

        lock (_projectGraphGate)
        {
            _pendingPostRestoreReloadPaths.Remove(projectPath);
            _postRestoreReloadAttemptedPaths.Remove(projectPath);
        }

        PublishPreparedProjectLoads(projectPath, referencedPaths, projectLoadResult.PreparedProjectLoads);
        await ReconcileProjectsAsync(update.ProjectsToReconcile);

        foreach (var referencedPath in referencedPaths)
        {
            if (!update.ProjectsToReconcile.Contains(referencedPath, PathUtilities.Comparer) &&
                IsReachable(referencedPath) &&
                TrySchedulePreparedProjectLoad(referencedPath))
            {
                await BeginLoadingProjectAsync(referencedPath, reloadIfAlreadyLoaded: true);
            }
        }
    }

    protected override async ValueTask OnProjectsRestoredAsync(
        ImmutableArray<string> restoredProjectPaths, CancellationToken cancellationToken)
    {
        foreach (var projectPath in restoredProjectPaths)
        {
            bool shouldReload;
            lock (_projectGraphGate)
            {
                shouldReload = _pendingPostRestoreReloadPaths.Remove(projectPath) &&
                    _reachablePaths.Contains(projectPath);
                if (shouldReload)
                    _postRestoreReloadAttemptedPaths.Add(projectPath);
            }

            if (shouldReload)
                await BeginLoadingProjectAsync(projectPath, reloadIfAlreadyLoaded: true);
        }
    }

    private void RegisterRoot(string projectPath)
    {
        lock (_projectGraphGate)
        {
            if (_rootPaths.Add(projectPath))
            {
                _reachablePaths.Add(projectPath);
                _projectGraphVersion++;
            }
        }
    }

    private bool IsReferencedProject(string projectPath)
    {
        lock (_projectGraphGate)
            return _referencedProjectPaths.Contains(projectPath);
    }

    private bool IsDocumentOpen(string projectPath)
    {
        lock (_projectGraphGate)
            return _openDocumentPaths.Contains(projectPath);
    }

    private bool IsReachable(string projectPath)
    {
        lock (_projectGraphGate)
            return _reachablePaths.Contains(projectPath);
    }

    private bool TrySchedulePreparedProjectLoad(string projectPath)
    {
        lock (_projectGraphGate)
        {
            if (!_preparedProjectLoads.TryGetValue(projectPath, out var preparedLoad) ||
                preparedLoad.IsScheduled)
            {
                return false;
            }

            _preparedProjectLoads[projectPath] = preparedLoad with { IsScheduled = true };
            return true;
        }
    }

    private bool TryTakePreparedProjectLoad(string projectPath, [NotNullWhen(true)] out PreparedProjectLoad? preparedLoad)
    {
        lock (_projectGraphGate)
            _preparedProjectLoads.Remove(projectPath, out preparedLoad);

        if (preparedLoad is not null)
        {
            var currentLastWriteTimeUtc = File.GetLastWriteTimeUtc(projectPath);
            if (currentLastWriteTimeUtc == preparedLoad.Result.EntryPointLastWriteTimeUtc &&
                currentLastWriteTimeUtc < preparedLoad.Result.GraphLoadStartTimeUtc)
            {
                return true;
            }
        }

        preparedLoad = null;
        return false;
    }

    private void PublishPreparedProjectLoads(
        string ownerProjectPath,
        ImmutableHashSet<string> referencedPaths,
        ImmutableArray<FileBasedProgramsProjectLoader.ProjectLoadResult> preparedProjectLoads)
    {
        if (preparedProjectLoads.IsDefaultOrEmpty)
            return;

        lock (_projectGraphGate)
        {
            foreach (var preparedProjectLoad in preparedProjectLoads)
            {
                if (!referencedPaths.Contains(preparedProjectLoad.EntryPointFilePath))
                    continue;

                var isScheduled = _preparedProjectLoads.TryGetValue(preparedProjectLoad.EntryPointFilePath, out var existingLoad) &&
                    existingLoad.IsScheduled;
                _preparedProjectLoads[preparedProjectLoad.EntryPointFilePath] = new(
                    ownerProjectPath,
                    preparedProjectLoad,
                    preparedProjectLoads,
                    isScheduled);
            }
        }
    }

    private void RemovePreparedProjectLoadsOwnedBy(string projectPath)
    {
        lock (_projectGraphGate)
        {
            foreach (var (preparedProjectPath, preparedLoad) in _preparedProjectLoads)
            {
                if (PathUtilities.Comparer.Equals(preparedLoad.OwnerProjectPath, projectPath))
                    _preparedProjectLoads.Remove(preparedProjectPath);
            }
        }
    }

    private ProjectGraphUpdate UpdateProjectReferences(string projectPath, ImmutableHashSet<string> referencedPaths)
    {
        lock (_projectGraphGate)
        {
            if (!_reachablePaths.Contains(projectPath))
            {
                return new(
                    WasReachable: false,
                    ReferencesApplied: false,
                    ProjectsToReconcile: [projectPath],
                    ProjectsToUnload: [projectPath]);
            }

            _referencesByProjectPath[projectPath] = referencedPaths;
            _projectGraphVersion++;
            return RecomputeProjectGraph_NoLock();
        }
    }

    private ProjectGraphUpdate RemoveRoot(string projectPath)
    {
        lock (_projectGraphGate)
        {
            var wasReachable = _reachablePaths.Contains(projectPath);
            _rootPaths.Remove(projectPath);
            _projectGraphVersion++;
            return RecomputeProjectGraph_NoLock() with { WasReachable = wasReachable };
        }
    }

    private ProjectGraphUpdate RemoveProjectFromGraph(string projectPath)
    {
        lock (_projectGraphGate)
        {
            _rootPaths.Remove(projectPath);
            _referencesByProjectPath.Remove(projectPath);
            _projectGraphVersion++;
            return RecomputeProjectGraph_NoLock();
        }
    }

    private ProjectGraphUpdate RecomputeProjectGraph_NoLock()
    {
        var previousReachablePaths = _reachablePaths.ToImmutableHashSet(PathUtilities.Comparer);
        var reachablePaths = new HashSet<string>(PathUtilities.Comparer);
        var referencedProjectPaths = new HashSet<string>(PathUtilities.Comparer);
        var pathsToVisit = new Stack<string>(_rootPaths);

        while (pathsToVisit.TryPop(out var path))
        {
            if (!reachablePaths.Add(path) ||
                !_referencesByProjectPath.TryGetValue(path, out var referencedPaths))
            {
                continue;
            }

            foreach (var referencedPath in referencedPaths)
            {
                referencedProjectPaths.Add(referencedPath);
                pathsToVisit.Push(referencedPath);
            }
        }

        var projectsToLoad = reachablePaths.Except(previousReachablePaths).ToImmutableArray();
        var projectsToUnload = previousReachablePaths.Except(reachablePaths).ToImmutableArray();
        ImmutableArray<string> projectsToReconcile = [.. projectsToLoad, .. projectsToUnload];

        _reachablePaths.Clear();
        _reachablePaths.UnionWith(reachablePaths);

        _referencedProjectPaths.Clear();
        _referencedProjectPaths.UnionWith(referencedProjectPaths);

        foreach (var projectToReconcile in projectsToReconcile)
        {
            if (!reachablePaths.Contains(projectToReconcile))
            {
                _referencesByProjectPath.Remove(projectToReconcile);
                _preparedProjectLoads.Remove(projectToReconcile);
                _pendingPostRestoreReloadPaths.Remove(projectToReconcile);
                _postRestoreReloadAttemptedPaths.Remove(projectToReconcile);

                foreach (var (preparedProjectPath, preparedLoad) in _preparedProjectLoads)
                {
                    if (PathUtilities.Comparer.Equals(preparedLoad.OwnerProjectPath, projectToReconcile))
                        _preparedProjectLoads.Remove(preparedProjectPath);
                }
            }
        }

        return new(
            WasReachable: false,
            ReferencesApplied: true,
            ProjectsToReconcile: projectsToReconcile,
            ProjectsToUnload: projectsToUnload);
    }

    private async ValueTask ReconcileProjectsAsync(ImmutableArray<string> projectPaths, string? exceptProjectPath = null)
    {
        foreach (var projectPath in projectPaths)
        {
            if (!PathUtilities.Comparer.Equals(projectPath, exceptProjectPath))
                await ReconcileProjectAsync(projectPath);
        }
    }

    private async ValueTask ReconcileProjectAsync(string projectPath)
    {
        while (true)
        {
            long version;
            bool shouldBeLoaded;
            lock (_projectGraphGate)
            {
                version = _projectGraphVersion;
                shouldBeLoaded = _reachablePaths.Contains(projectPath);
            }

            if (shouldBeLoaded)
            {
                var reloadIfAlreadyLoaded = TrySchedulePreparedProjectLoad(projectPath);
                await BeginLoadingProjectAsync(
                    projectPath,
                    reloadIfAlreadyLoaded: reloadIfAlreadyLoaded);
            }
            else
                await TryUnloadProjectAsync(projectPath);

            lock (_projectGraphGate)
            {
                if (version == _projectGraphVersion)
                    return;
            }
        }
    }

    private readonly record struct ProjectGraphUpdate(
        bool WasReachable,
        bool ReferencesApplied,
        ImmutableArray<string> ProjectsToReconcile,
        ImmutableArray<string> ProjectsToUnload);

    private sealed record PreparedProjectLoad(
        string OwnerProjectPath,
        FileBasedProgramsProjectLoader.ProjectLoadResult Result,
        ImmutableArray<FileBasedProgramsProjectLoader.ProjectLoadResult> ProjectGraphLoads,
        bool IsScheduled);
}
