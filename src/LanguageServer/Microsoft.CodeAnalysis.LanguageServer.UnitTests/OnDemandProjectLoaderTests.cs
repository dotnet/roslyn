// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;
using TestProjectLoader = Microsoft.CodeAnalysis.LanguageServer.UnitTests.LanguageServerProjectLoaderTests.TestProjectLoader;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class OnDemandProjectLoaderTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    private protected override Task<ExportProvider> CreateExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader)
        => Task.FromResult(LanguageServerTestComposition.GetSharedExportProvider(
            serverConfiguration, loggerFactory, typeof(LanguageServerProjectLoaderTests.TestProjectLoaderFactory)));

    [Fact]
    public void DiscoveryFindsProjectsInNearestAncestorDirectory()
    {
        var workspaceFolder = TempRoot.CreateDirectory();
        workspaceFolder.CreateFile("Outer.csproj");
        var projectFolder = workspaceFolder.CreateDirectory("Projects");
        var project = projectFolder.CreateFile("Project.csproj").Path;
        var sourceFolder = projectFolder.CreateDirectory("Source");
        var discovery = new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory);

        // Searching upward from Source must stop at Projects/Project.csproj rather than continuing to Outer.csproj.
        AssertEx.SetEqual(
            [project],
            discovery.DiscoverProjects(
                sourceFolder.Path,
                ImmutableHashSet.Create(PathUtilities.Comparer, workspaceFolder.Path),
                CancellationToken.None));
    }

    [Fact]
    public void DiscoveryStopsAtDeepestWorkspaceFolder()
    {
        var outerFolder = TempRoot.CreateDirectory();
        outerFolder.CreateFile("Outer.csproj");
        var innerFolder = outerFolder.CreateDirectory("Inner");
        var sourceFolder = innerFolder.CreateDirectory("Source");
        var workspaceFolders = ImmutableHashSet.Create(PathUtilities.Comparer, outerFolder.Path, innerFolder.Path);
        var discovery = new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory);

        // The inner workspace is the search boundary, even though another workspace contains a project above it.
        Assert.Empty(discovery.DiscoverProjects(sourceFolder.Path, workspaceFolders, CancellationToken.None));
    }

    [Fact]
    public void DiscoveryFiltersUnsupportedProjectExtensions()
    {
        var workspaceFolder = TempRoot.CreateDirectory();
        var supportedProject = workspaceFolder.CreateFile("Supported.csproj").Path;
        workspaceFolder.CreateFile("Unsupported.vbproj");
        workspaceFolder.CreateFile("Document.cs");
        workspaceFolder.CreateFile("NotAProject.csproj.backup");
        workspaceFolder.CreateFile("README");
        var discovery = new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory);

        // Only the registered extension qualifies; a name containing ".csproj" with another suffix must not match.
        AssertEx.SetEqual(
            [supportedProject],
            discovery.DiscoverProjects(
                workspaceFolder.Path,
                ImmutableHashSet.Create(PathUtilities.Comparer, workspaceFolder.Path),
                CancellationToken.None));
    }

    [Fact]
    public async Task TransitiveProjectReferencesLoadAllProjects()
    {
        // Follow Application -> Library -> Dependency, including references discovered after the initial build.
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var projectFolder = folder.CreateDirectory("Application");
        var projectPath = projectFolder.CreateFile("Application.csproj").Path;
        // Sibling directories ensure Library and Dependency are reached through references, not directory discovery.
        var referencedProjectPath = folder.CreateDirectory("Library").CreateFile("Library.csproj").Path;
        var transitiveProjectPath = folder.CreateDirectory("Dependency").CreateFile("Dependency.csproj").Path;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(projectFolder.CreateFile("Document.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        try
        {
            // Register the controlled results before traversal can request any of these builds.
            var projectBuild = loader.ExpectDesignTimeBuild(projectPath);
            var referencedProjectBuild = loader.ExpectDesignTimeBuild(referencedProjectPath);
            var transitiveProjectBuild = loader.ExpectDesignTimeBuild(transitiveProjectPath);

            var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
            await projectBuild.Started.Task;
            // Capture the wait before any references are known; it must include projects discovered later.
            var activeLoads = onDemandLoader.WaitForActiveLoadsAsync();
            projectBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, projectReferences: [referencedProjectPath]);

            await referencedProjectBuild.Started.Task;
            // Completing Application must not finish either wait while Library is still loading.
            Assert.False(load.IsCompleted);
            Assert.False(activeLoads.IsCompleted);
            // Library reveals another reference that the same traversal must follow.
            referencedProjectBuild.CompleteSuccessfully(
                loader.WorkspaceFactory.HostProjectFactory, referencedProjectPath, projectReferences: [transitiveProjectPath]);

            await transitiveProjectBuild.Started.Task;
            // The waits must also include the transitive dependency, not just Application's direct reference.
            Assert.False(load.IsCompleted);
            Assert.False(activeLoads.IsCompleted);
            transitiveProjectBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, transitiveProjectPath);

            // Once the leaf finishes, all three projects must be committed to the workspace with one build each.
            await load;
            await activeLoads;
            Assert.Equal(3, loader.DesignTimeBuildCount);
            AssertEx.SetEqual(
                [projectPath, referencedProjectPath, transitiveProjectPath],
                loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects.Select(static project => project.FilePath));
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }
    }

    [Fact]
    public async Task CircularProjectReferencesComplete()
    {
        // Simulate Application -> Library -> Application. The traversal must terminate and load each project only once.
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var projectFolder = folder.CreateDirectory("Application");
        var projectPath = projectFolder.CreateFile("Application.csproj").Path;
        // Library is outside the starting directory so only Application is discovered directly.
        var referencedProjectPath = folder.CreateDirectory("Library").CreateFile("Library.csproj").Path;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(projectFolder.CreateFile("Document.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        try
        {
            var projectBuild = loader.ExpectDesignTimeBuild(projectPath);
            var referencedProjectBuild = loader.ExpectDesignTimeBuild(referencedProjectPath);

            var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
            await projectBuild.Started.Task;
            // This wait must remain attached to the traversal as it discovers Library.
            var activeLoads = onDemandLoader.WaitForActiveLoadsAsync();
            projectBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, projectReferences: [referencedProjectPath]);

            await referencedProjectBuild.Started.Task;
            // Completing Application must not finish either wait while Library is still loading.
            Assert.False(load.IsCompleted);
            Assert.False(activeLoads.IsCompleted);
            // Close the cycle by making Library reference the already-loaded Application project.
            referencedProjectBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, referencedProjectPath, projectReferences: [projectPath]);

            // Both waits must finish despite the cycle, without building either project again.
            await load;
            await activeLoads;
            Assert.Equal(2, loader.DesignTimeBuildCount);
            AssertEx.SetEqual(
                [projectPath, referencedProjectPath],
                loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects.Select(static project => project.FilePath));
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }
    }

    [Fact]
    public async Task SelfReferencingProjectTraversalCompletes()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var projectPath = folder.CreateFile("Project.csproj").Path;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        try
        {
            // Pre-complete the result: this test checks cycle termination, not waiting for an unfinished build.
            var build = loader.ExpectDesignTimeBuild(projectPath);
            // Simulate A -> A: the project's design-time build reports a reference to itself.
            build.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, projectReferences: [projectPath]);

            // Traversal must terminate without repeatedly following the self-reference or rebuilding the project.
            await onDemandLoader.TryLoadProjectsAsync(documentUri);
            Assert.Equal(1, loader.DesignTimeBuildCount);

            var loadedProject = Assert.Single(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
            Assert.Equal(projectPath, loadedProject.FilePath);
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }
    }

    [Fact]
    public async Task ShutdownCancelsActiveTraversal()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var projectPath = folder.CreateFile("Project.csproj").Path;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        // Leave the build pending so only cancellation, not normal build completion, can end the traversal.
        var build = loader.ExpectDesignTimeBuild(projectPath);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        Task activeLoads;
        try
        {
            // Shut down while the traversal is waiting for an unfinished design-time build.
            await build.Started.Task;
            activeLoads = onDemandLoader.WaitForActiveLoadsAsync();
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }

        // The traversal handles cancellation internally, so both waits must finish without completing the build.
        await load;
        await activeLoads;

        // Traversal shutdown does not cancel the underlying build; complete it separately for cleanup.
        build.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
    }

    [Fact]
    public async Task NoDiscoveredProjectDoesNotLoad()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Loose.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        try
        {
            // The loose file has no project in its workspace, so discovery must not initiate a design-time build.
            await onDemandLoader.TryLoadProjectsAsync(documentUri);
            Assert.Equal(0, loader.DesignTimeBuildCount);
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }
    }

    [Fact]
    public async Task NonFileUriDoesNotLoad()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        try
        {
            // Untitled documents have no file-system directory from which to discover projects.
            var load = onDemandLoader.TryLoadProjectsAsync(new LSP.DocumentUri("untitled:Loose.cs"));
            // Unsupported URIs are rejected before project discovery starts.
            var completedSynchronously = load.IsCompletedSuccessfully;
            await load;
            Assert.True(completedSynchronously);
            Assert.Equal(0, loader.DesignTimeBuildCount);
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }
    }

    [Fact]
    public async Task DisabledOnDemandLoadingDoesNotStartBuild()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        // Supply a discoverable project so the option, rather than missing input, prevents a build.
        folder.CreateFile("Project.csproj");
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var globalOptions = server.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand, false);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        try
        {
            // With loading disabled, the request must complete immediately and neither API should start a build.
            var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
            Assert.True(load.IsCompletedSuccessfully);
            await load;
            await onDemandLoader.WaitForActiveLoadsAsync();
            Assert.Equal(0, loader.DesignTimeBuildCount);
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }
    }

    [Fact]
    public async Task DevKitDisablesOnDemandLoading()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        // Keep a valid project available so this exercises the DevKit opt-out rather than the no-project path.
        folder.CreateFile("Project.csproj");
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var globalOptions = server.ExportProvider.GetExportedValue<IGlobalOptionService>();
        // Simulate DevKit owning project loading instead of Roslyn's on-demand loader.
        globalOptions.SetGlobalOption(LspOptionsStorage.LspUsingDevkitFeatures, true);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        try
        {
            // Loading and waiting must leave project loading to DevKit, without starting a Roslyn build.
            var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
            Assert.True(load.IsCompletedSuccessfully);
            await load;
            await onDemandLoader.WaitForActiveLoadsAsync();
            Assert.Equal(0, loader.DesignTimeBuildCount);
        }
        finally
        {
            await onDemandLoader.ShutdownAsync();
        }
    }

    private OnDemandProjectLoader CreateOnDemandLoader(SingleServerTestLspServer server, TestProjectLoader loader, string workspaceFolder)
    {
        var tracker = new WorkspaceFolderTracker();
        tracker.Update([new LSP.WorkspaceFolder { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspaceFolder), Name = "workspace" }], removedFolders: null);
        var globalOptions = server.ExportProvider.GetExportedValue<IGlobalOptionService>();
        var listener = server.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.Workspace);
        return new OnDemandProjectLoader(
            new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory),
            tracker, loader, globalOptions, listener, LoggerFactory);
    }
}
