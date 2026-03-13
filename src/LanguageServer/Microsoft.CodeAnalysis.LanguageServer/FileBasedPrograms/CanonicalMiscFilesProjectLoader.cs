// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

/// <summary>
/// Handles loading miscellaneous files that are not file-based programs.
/// A canonical project is built once via design-time build to obtain references and options.
/// Subsequent misc files fork the canonical build results without performing additional builds.
/// </summary>
internal sealed class CanonicalMiscFilesProjectLoader : LanguageServerProjectLoader
{
    /// <summary>
    /// Lazily computed canonical build results. The first <see cref="TryLoadProjectInMSBuildHostAsync"/> call
    /// triggers the build; subsequent calls await the cached result and fork it for the specific misc file.
    /// </summary>
    private AsyncLazy<ImmutableArray<ProjectFileInfo>>? _canonicalBuildResult;

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
    }

    public async ValueTask<TextDocument> AddMiscellaneousDocumentAsync(string documentPath, SourceText documentText, CancellationToken cancellationToken)
    {
        var miscWorkspace = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace;
        var sourceTextLoader = new SourceTextLoader(documentText, documentPath);
        var enableFileBasedPrograms = GlobalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms);
        var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(
            miscWorkspace, documentPath, sourceTextLoader, new LanguageInformation(LanguageNames.CSharp, scriptExtension: null), documentText.ChecksumAlgorithm, miscWorkspace.Services.SolutionServices, [], enableFileBasedPrograms);

        _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(projectInfo));

        await BeginLoadingProjectWithPrimordialAsync(
            documentPath,
            _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
            projectInfo.Id,
            doDesignTimeBuild: true);

        var id = projectInfo.Documents.Single().Id;
        return miscWorkspace.CurrentSolution.GetRequiredDocument(id);
    }

    protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
        BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
    {
        // Ensure the canonical build is started. The first call triggers the actual build via the
        // batch's BuildHostProcessManager; subsequent calls reuse the cached result.
        if (_canonicalBuildResult is null)
        {
            var newLazy = AsyncLazy.Create(
                static (args, ct) => LoadCanonicalProjectAsync(args.buildHostProcessManager, ct),
                (self: this, buildHostProcessManager));
            Interlocked.CompareExchange(ref _canonicalBuildResult, newLazy, null);
        }

        var canonicalInfos = await _canonicalBuildResult.GetValueAsync(cancellationToken);
        return ForkResultsForMiscFile(canonicalInfos, projectPath);
    }

    private static async Task<ImmutableArray<ProjectFileInfo>> LoadCanonicalProjectAsync(
        BuildHostProcessManager buildHostProcessManager, CancellationToken cancellationToken)
    {
        // Set the FileBasedProgram feature flag so that '#:' is permitted without errors in rich misc files.
        // This allows us to avoid spurious errors for files which contain '#:' directives yet are not treated
        // as file-based programs (due to not being saved to disk, for example.)
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

        var tempDirectory = Path.Combine(Path.GetTempPath(), "roslyn-canonical-misc", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        var virtualProjectPath = Path.Combine(tempDirectory, "Canonical.csproj");

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        var buildHost = await buildHostProcessManager.GetBuildHostAsync(buildHostKind, virtualProjectPath, dotnetPath: null, cancellationToken);
        var loadedFile = await buildHost.LoadProjectAsync(virtualProjectPath, virtualProjectXml, languageName: LanguageNames.CSharp, cancellationToken);

        return await loadedFile.GetProjectFileInfosAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a <see cref="LanguageServerProjectLoader.RemoteProjectLoadResult" /> for a misc file by forking the cached canonical build results.
    /// The forked result has the same references and options as the canonical project, but with the
    /// misc file's document added.
    /// </summary>
    private RemoteProjectLoadResult ForkResultsForMiscFile(ImmutableArray<ProjectFileInfo> canonicalInfos, string miscDocumentPath)
    {
        var miscDocFileInfo = new DocumentFileInfo(
            filePath: miscDocumentPath,
            logicalPath: Path.GetFileName(miscDocumentPath),
            isLinked: false,
            isGenerated: false,
            folders: []);

        var forkedInfos = canonicalInfos.SelectAsArray(info => info with
        {
            FilePath = VirtualProjectXmlProvider.GetVirtualProjectPath(miscDocumentPath),
            Documents = info.Documents.Add(miscDocFileInfo),
        });

        const BuildHostProcessKind buildHostKind = BuildHostProcessKind.NetCore;
        return new RemoteProjectLoadResult
        {
            ProjectFileInfos = forkedInfos,
            DiagnosticLogItems = [],
            ProjectFactory = _workspaceFactory.MiscellaneousFilesWorkspaceProjectFactory,
            IsFileBasedProgram = false,
            IsMiscellaneousFile = true,
            PreferredBuildHostKind = buildHostKind,
            ActualBuildHostKind = buildHostKind,
        };
    }
}
