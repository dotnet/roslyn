// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    internal ImmutableArray<string> WorkspaceFoldersOpt { private get; set; }

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
                    var canonicalProject = GetRequiredCanonicalProject();
                    var syntaxTree = CSharpSyntaxTree.ParseText(text: documentText, (CSharpParseOptions?)canonicalProject.ParseOptions, path: documentPath, cancellationToken);
                    return await AddForkedCanonicalProject_NoLockAsync(canonicalProject, loadedProjects, documentPath, syntaxTree, cancellationToken);
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

    private async ValueTask<TextDocument> AddForkedCanonicalProject_NoLockAsync(Project canonicalProject, Dictionary<string, ProjectLoadState> loadedProjects, string documentPath, SyntaxTree syntaxTree, CancellationToken cancellationToken)
    {
        var newProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{documentPath}'");
        var documentText = await syntaxTree.GetTextAsync(cancellationToken);
        var newDocumentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(newProjectId),
            name: Path.GetFileName(documentPath),
            loader: TextLoader.From(TextAndVersion.Create(documentText, VersionStamp.Create())),
            filePath: documentPath);

        var forkedProjectId = ProjectId.CreateNewId(debugName: $"Forked Misc Project for '{documentPath}'");

        bool? containedInCsprojCone = null;
        var hasAllInformation = false;
        if (await CalcHasAllInformation_EasyOutAsync(GlobalOptionService, syntaxTree, cancellationToken))
        {
            var inCone = CalcIsContainedInCsprojCone(documentPath);
            hasAllInformation = !inCone;
            containedInCsprojCone = inCone;
        }

        var forkedProjectAttributes = new ProjectInfo.ProjectAttributes(
            newDocumentInfo.Id.ProjectId,
            version: VersionStamp.Create(),
            name: canonicalProject.Name,
            assemblyName: canonicalProject.AssemblyName,
            language: canonicalProject.Language,
            compilationOutputInfo: default,
            checksumAlgorithm: SourceHashAlgorithm.Sha1,
            filePath: documentPath,
            outputFilePath: canonicalProject.OutputFilePath,
            outputRefFilePath: canonicalProject.OutputRefFilePath,
            hasAllInformation: hasAllInformation);

        var forkedProjectInfo = ProjectInfo.Create(
            attributes: forkedProjectAttributes,
            compilationOptions: canonicalProject.CompilationOptions,
            parseOptions: canonicalProject.ParseOptions,
            documents: [newDocumentInfo, .. await Task.WhenAll(canonicalProject.Documents.Select(document => GetDocumentInfoAsync(document)))],
            projectReferences: canonicalProject.ProjectReferences,
            metadataReferences: canonicalProject.MetadataReferences,
            analyzerReferences: canonicalProject.AnalyzerReferences,
            analyzerConfigDocuments: await canonicalProject.AnalyzerConfigDocuments.SelectAsArrayAsync(async document => await GetDocumentInfoAsync(document)),
            additionalDocuments: await canonicalProject.AdditionalDocuments.SelectAsArrayAsync(async document => await GetDocumentInfoAsync(document)));

        await _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
        {
            workspace.OnProjectAdded(forkedProjectInfo);
        }, cancellationToken);
        loadedProjects[documentPath] = new ProjectLoadState.CanonicalForked(forkedProjectInfo.Id, containedInCsprojCone);

        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var addedDocument = miscWorkspace.CurrentSolution.GetRequiredDocument(newDocumentInfo.Id);
        return addedDocument;

        async Task<DocumentInfo> GetDocumentInfoAsync(TextDocument document)
        {
            var documentPath = document.FilePath;
            return DocumentInfo.Create(
                DocumentId.CreateNewId(forkedProjectId),
                name: Path.GetFileName(documentPath) ?? "",
                loader: TextLoader.From(TextAndVersion.Create(await document.GetTextAsync(cancellationToken).ConfigureAwait(false), VersionStamp.Create())),
                filePath: documentPath);
        }
    }

    internal async Task<bool?> GetHasAllInformation_IncrementalAsync(IGlobalOptionService globalOptionService, SyntaxTree tree, CancellationToken cancellationToken)
    {
        return await ExecuteUnderGateAsync(async loadedProjects =>
        {
            // Note: caller is making a decision on whether to unload a project.
            // If the forked project isn't even fully loaded yet, then, give a null back to indicate they should not unload, so the project has an opportunity to finish loading.
            if (!loadedProjects.TryGetValue(tree.FilePath, out var loadState) || loadState is not ProjectLoadState.CanonicalForked forkedState)
            {
                return (bool?)null;
            }

            if (!await CalcHasAllInformation_EasyOutAsync(globalOptionService, tree, cancellationToken))
                return false;

            // TODO2: figure out better sharing between here and 'AddForkedCanonicalProject'.
            // Correctness/consistency more important than optimizing locks etc
            if (forkedState.ContainedInCsprojCone is null)
            {
                var containedInCsprojCone = CalcIsContainedInCsprojCone(tree.FilePath);
                loadedProjects[tree.FilePath] = forkedState = forkedState with { ContainedInCsprojCone = containedInCsprojCone };
            }

            // TODO2: at least some tests must verify the state of the workspace and project system.
            // In general we should probably verify a consistent state between these, e.g. every project system entry must have corresponding workspace project(s).
            var hasAllInformation = !forkedState.ContainedInCsprojCone.GetValueOrDefault();
            return hasAllInformation;
        }, cancellationToken);
    }

    /// <summary>Check if HasAllInformation should be enabled in a file. Includes only checks which are cheap, i.e. we are OK with performing on keystroke+delay.</summary>
    private static async Task<bool> CalcHasAllInformation_EasyOutAsync(IGlobalOptionService globalOptionService, SyntaxTree tree, CancellationToken cancellationToken)
    {
        if (!globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms)
            || !globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedProgramsWhenAmbiguous))
        {
            return false;
        }

        var root = await tree.GetRootAsync(cancellationToken);
        if (root is not CompilationUnitSyntax compilationUnit)
            return false;

        return compilationUnit.Members.Any(SyntaxKind.GlobalStatement);
    }

    /// <summary>
    /// Determine if this file is contained in the same directory as a .csproj file.
    /// </summary>
    /// <remarks>
    /// The result of this method influences whether semantic errors are displayed in loose files which have top-level statements but no '#:' directives.
    /// The projects for such files are *forked canonical projects*. Displaying semantic errors is controlled by the 'HasAllInformation' flag on the project.
    /// The inputs to the HasAllInformation flag value are effectively the following:
    /// 1. File has top-level statements, and
    /// 2. File is not contained in a .csproj cone
    ///
    /// We want to minimize the amount of work we do incrementally to keep track of this information. Therefore:
    /// - We handle a possible change in (1) by doing a check on the latest syntax tree.
    /// - We handle a possible change in (2) by unloading and reloading relevant forked canonical project(s).
    /// Therefore this is the only place we want to actually do the work to determine (2).
    /// </remarks>
    internal bool CalcIsContainedInCsprojCone(string csFilePath)
    {
        // We only do csproj-in-cone checks if the file is contained in a currently opened workspace folder
        if (WorkspaceFoldersOpt.IsDefaultOrEmpty)
            return false;

        // Precondition: opened workspace folder paths, have already been deduplicated to remove folders in the same hierarchy.
        // e.g. 'workspaceFolderPaths' will not contain both `C:\src\roslyn`, and `C:\src\roslyn\docs`.
        var containingWorkspacePath = WorkspaceFoldersOpt.FirstOrDefault(
            (workspacePath, csFilePath) => PathUtilities.IsSameDirectoryOrChildOf(child: csFilePath, parent: workspacePath), arg: csFilePath);
        if (containingWorkspacePath is null)
            return false;

        // When the path is not absolute (for virtual documents, etc), we can't perform this search.
        // Optimistically assume there is no csproj in cone.
        if (!PathUtilities.IsAbsolute(csFilePath))
            return false;

        var directoryName = PathUtilities.GetDirectoryName(csFilePath);
        while (PathUtilities.IsSameDirectoryOrChildOf(child: directoryName, parent: containingWorkspacePath))
        {
            var containsCsproj = Directory.EnumerateFiles(directoryName, "*.csproj").Any();
            if (containsCsproj)
                return true;

            directoryName = PathUtilities.GetDirectoryName(directoryName);
        }

        return false;
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
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken);

            // Remove the primordial project
            var wasUnloaded = await TryUnloadProject_NoLockAsync(projectPath);
            Contract.ThrowIfFalse(wasUnloaded);

            // Replace with a forked canonical project
            await AddForkedCanonicalProject_NoLockAsync(GetRequiredCanonicalProject(), loadedProjects, projectPath, syntaxTree, cancellationToken);
        }

        // Now remove the primordial canonical project
        await canonicalProjectState.PrimordialProjectFactory.ApplyChangeToWorkspaceAsync(workspace =>
            workspace.OnProjectRemoved(canonicalProjectState.PrimordialProjectId),
            cancellationToken);
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
