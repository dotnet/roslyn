// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(LanguageServerWorkspaceFactory)), Shared]
internal sealed class LanguageServerWorkspaceFactory
{
    private readonly ILogger _logger;
    private readonly ImmutableArray<string> _solutionLevelAnalyzerPaths;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LanguageServerWorkspaceFactory(
        HostServicesProvider hostServicesProvider,
        IFileChangeWatcher fileChangeWatcher,
        [ImportMany] IEnumerable<Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata>> dynamicFileInfoProviders,
        ProjectTargetFrameworkManager projectTargetFrameworkManager,
        ExtensionAssemblyManager extensionManager,
        [ImportMany] IEnumerable<IAnalyzerAssemblyRedirector> assemblyRedirectors,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(LanguageServerWorkspaceFactory));

        // Before we can create the workspace, let's figure out the solution-level analyzers; we'll pull in analyzers from our own binaries
        // as well as anything coming from extensions.
        _solutionLevelAnalyzerPaths = new DirectoryInfo(AppContext.BaseDirectory).GetFiles("*.dll")
            .Where(f => f.Name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal) && !f.Name.Contains("LanguageServer", StringComparison.Ordinal))
            .Select(f => f.FullName)
            .Concat(extensionManager.ExtensionAssemblyPaths)
            .ToImmutableArray();

        // Create the workspace and set analyzer references for it
        var workspace = new LanguageServerWorkspace(hostServicesProvider.HostServices, WorkspaceKind.Host);
        workspace.SetCurrentSolution(s => s.WithAnalyzerReferences(CreateSolutionLevelAnalyzerReferencesForWorkspace(workspace)), WorkspaceChangeKind.SolutionChanged);

        HostProjectFactory = new ProjectSystemProjectFactory(
            workspace, fileChangeWatcher, static (_, _) => Task.CompletedTask, _ => { },
            CancellationToken.None); // TODO: do we need to introduce a shutdown cancellation token for this?
        workspace.ProjectSystemProjectFactory = HostProjectFactory;

        // https://github.com/dotnet/roslyn/issues/78560: Move this workspace creation to 'FileBasedProgramsWorkspaceProviderFactory'.
        // 'CreateSolutionLevelAnalyzerReferencesForWorkspace' needs to be broken out into its own service for us to be able to move this.
        var miscellaneousFilesWorkspace = new LanguageServerWorkspace(hostServicesProvider.HostServices, WorkspaceKind.MiscellaneousFiles);
        miscellaneousFilesWorkspace.SetCurrentSolution(s => s.WithAnalyzerReferences(CreateSolutionLevelAnalyzerReferencesForWorkspace(miscellaneousFilesWorkspace)), WorkspaceChangeKind.SolutionChanged);

        MiscellaneousFilesWorkspaceProjectFactory = new ProjectSystemProjectFactory(
            miscellaneousFilesWorkspace, fileChangeWatcher, static (_, _) => Task.CompletedTask, _ => { }, CancellationToken.None);
        miscellaneousFilesWorkspace.ProjectSystemProjectFactory = MiscellaneousFilesWorkspaceProjectFactory;

        ProjectSystemHostInfo = new ProjectSystemHostInfo(
            DynamicFileInfoProviders: [.. dynamicFileInfoProviders],
            AnalyzerAssemblyRedirectors: [.. assemblyRedirectors]);

        TargetFrameworkManager = projectTargetFrameworkManager;
    }

    public Workspace HostWorkspace => HostProjectFactory.Workspace;

    public ProjectSystemProjectFactory HostProjectFactory { get; }
    public ProjectSystemProjectFactory MiscellaneousFilesWorkspaceProjectFactory { get; }

    public ProjectSystemHostInfo ProjectSystemHostInfo { get; }
    public ProjectTargetFrameworkManager TargetFrameworkManager { get; }

    public ImmutableArray<AnalyzerFileReference> CreateSolutionLevelAnalyzerReferencesForWorkspace(Workspace workspace)
    {
        var references = ImmutableArray.CreateBuilder<AnalyzerFileReference>();
        var loaderProvider = workspace.Services.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        // Load all analyzers into a fresh shadow copied load context.  In the future, if we want to support reloading
        // of solution-level analyzer references, we should just need to listen for changes to those analyzer paths and
        // then call back into this method to update the solution accordingly.
        var analyzerLoader = loaderProvider.CreateNewShadowCopyLoader();

        foreach (var analyzerPath in _solutionLevelAnalyzerPaths)
        {
            if (File.Exists(analyzerPath))
            {
                references.Add(new AnalyzerFileReference(analyzerPath, analyzerLoader));
                _logger.LogDebug($"Solution-level analyzer at {analyzerPath} added to workspace.");
            }
            else
            {
                _logger.LogWarning($"Solution-level analyzer at {analyzerPath} could not be found.");
            }
        }

        return references.ToImmutableAndClear();
    }
}
