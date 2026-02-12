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

    public async ValueTask<TextDocument> AddMiscellaneousDocumentAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            var canonicalDocumentPath = _canonicalDocumentPath.Value;
            if (loadedProjects.TryGetValue(canonicalDocumentPath, out var canonicalLoadState))
            {
                Contract.ThrowIfFalse(canonicalLoadState is ProjectLoadState.Primordial or ProjectLoadState.LoadedTargets(LoadedProjectTargets: [_]));
                if (canonicalLoadState is ProjectLoadState.LoadedTargets)
                {
                    return await AddForkedCanonicalProject_NoLockAsync(loadedProjects, documentPath, documentText, cancellationToken);
                }
            }
            else
            {
                BeginLoadingCanonicalProject_NoLock();
            }

            // Not ready to fork the canonical project. Create a primordial project instead.
            return AddPrimordialMiscProject_NoLock(loadedProjects, documentPath, documentText);
        }, cancellationToken);
    }

    private async ValueTask<TextDocument> AddForkedCanonicalProject_NoLockAsync(Dictionary<string, ProjectLoadState> loadedProjects, string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        var newProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{documentPath}'");
        var newDocumentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(newProjectId),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        var forkedProjectInfo = await GetForkedProjectInfoAsync(GetRequiredCanonicalProject(), newDocumentInfo, documentText, GlobalOptionService, cancellationToken);

        await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
        {
            workspace.OnProjectAdded(forkedProjectInfo);
        }, cancellationToken);
        loadedProjects[documentPath] = new ProjectLoadState.CanonicalForked(forkedProjectInfo.Id);

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
    private Document AddPrimordialMiscProject_NoLock(Dictionary<string, ProjectLoadState> loadedProjects, string documentPath, SourceText documentText)
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var sourceTextLoader = new SourceTextLoader(documentText, documentPath);
        var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            miscWorkspace, documentPath, sourceTextLoader, new LanguageInformation(LanguageNames.CSharp, scriptExtension: null), documentText.ChecksumAlgorithm, miscWorkspace.Services.SolutionServices, [], enableFileBasedPrograms);

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

        // Set the FileBasedProgram feature flag so that '#:' is permitted without errors in rich misc files.
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

    protected override async ValueTask TransitionPrimordialProjectToLoaded_NoLockAsync(
        Dictionary<string, ProjectLoadState> loadedProjects,
        string canonicalProjectPath,
        ProjectLoadState.Primordial canonicalProjectState,
        CancellationToken cancellationToken)
    {
        // This loader should only do a design time build on the canonical project
        Contract.ThrowIfFalse(canonicalProjectPath == _canonicalDocumentPath.Value);

        var entriesToReplace = loadedProjects
            .Where(entry => entry.Key != canonicalProjectPath && entry.Value is ProjectLoadState.Primordial)
            .ToArray();

        // Replace all primordial projects in 'loadedProjects' with forked canonical projects
        foreach (var (projectPath, projectLoadState) in entriesToReplace)
        {
            // Get the text from the primordial project
            var primordial = (ProjectLoadState.Primordial)projectLoadState;
            var solution = primordial.PrimordialProjectFactory.Workspace.CurrentSolution;
            var document = solution.GetRequiredProject(primordial.PrimordialProjectId).Documents.Single();
            var text = await document.GetTextAsync(cancellationToken);

            // Remove the primordial project
            var wasUnloaded = await TryUnloadProject_NoLockAsync(projectPath);
            Contract.ThrowIfFalse(wasUnloaded);

            // Replace with a forked canonical project
            await AddForkedCanonicalProject_NoLockAsync(loadedProjects, projectPath, text, cancellationToken);
        }

        // Now remove the primordial canonical project
        await canonicalProjectState.PrimordialProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
            workspace.OnProjectRemoved(canonicalProjectState.PrimordialProjectId),
            cancellationToken);
    }

    /// <summary>
    /// Creates a new project based on the canonical project with a new document added.
    /// This should only be called when the canonical project is in the FullyLoaded state.
    /// </summary>
    private static async Task<ProjectInfo> GetForkedProjectInfoAsync(Project canonicalProject, DocumentInfo newDocumentInfo, SourceText documentText, IGlobalOptionService globalOptionService, CancellationToken cancellationToken)
    {
        var newDocumentPath = newDocumentInfo.FilePath;
        Contract.ThrowIfNull(newDocumentPath);

        var forkedProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{newDocumentPath}'");
        var syntaxTree = CSharpSyntaxTree.ParseText(text: documentText, canonicalProject.ParseOptions as CSharpParseOptions, path: newDocumentPath, cancellationToken);
        var hasAllInformation = await VirtualProjectXmlProvider.ShouldReportSemanticErrorsInPossibleFileBasedProgramAsync(globalOptionService, syntaxTree, cancellationToken);
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

    private Project GetRequiredCanonicalProject()
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var project = miscWorkspace.CurrentSolution.Projects
            .Single(p => PathUtilities.Comparer.Equals(p.FilePath, _canonicalDocumentPath.Value));

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
