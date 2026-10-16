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

        Assert.Empty(discovery.DiscoverProjects(sourceFolder.Path, workspaceFolders, CancellationToken.None));
    }

    [Fact]
    public void DiscoveryFiltersUnsupportedProjectExtensions()
    {
        var workspaceFolder = TempRoot.CreateDirectory();
        var supportedProject = workspaceFolder.CreateFile("Supported.csproj").Path;
        workspaceFolder.CreateFile("Unsupported.vbproj");
        var discovery = new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory);

        AssertEx.SetEqual(
            [supportedProject],
            discovery.DiscoverProjects(
                workspaceFolder.Path,
                ImmutableHashSet.Create(PathUtilities.Comparer, workspaceFolder.Path),
                CancellationToken.None));
    }

    [Fact]
    public async Task SelfReferenceCompletes()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var projectPath = folder.CreateFile("Project.csproj").Path;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        using var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        var build = loader.ExpectDesignTimeBuild(projectPath);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        await build.Started.Task;
        var activeLoads = onDemandLoader.WaitForActiveLoadsAsync();
        Assert.False(activeLoads.IsCompleted);
        build.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, projectReferences: [projectPath]);

        await load;
        await activeLoads;
        Assert.Equal(1, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task ShutdownCancelsActiveTraversal()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var projectPath = folder.CreateFile("Project.csproj").Path;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        using var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        var build = loader.ExpectDesignTimeBuild(projectPath);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        await build.Started.Task;
        var activeLoads = onDemandLoader.WaitForActiveLoadsAsync();

        await onDemandLoader.ShutdownAsync();
        await load;
        await activeLoads;

        Assert.False(onDemandLoader.CanLoad(documentUri));
        await onDemandLoader.WaitForActiveLoadsAsync();

        build.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
    }

    [Fact]
    public async Task NoDiscoveredProjectDoesNotLoad()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Loose.cs").Path);
        using var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        await onDemandLoader.TryLoadProjectsAsync(documentUri);
        Assert.Equal(0, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task NonFileUriDoesNotLoad()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        using var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        var load = onDemandLoader.TryLoadProjectsAsync(new LSP.DocumentUri("untitled:Loose.cs"));
        // Unsupported URIs are rejected before project discovery starts.
        var completedSynchronously = load.IsCompletedSuccessfully;
        await load;
        Assert.True(completedSynchronously);
        Assert.Equal(0, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task DisabledOnDemandLoadingDoesNotStartBuild()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        folder.CreateFile("Project.csproj");
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var globalOptions = server.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand, false);
        using var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        Assert.True(load.IsCompletedSuccessfully);
        await load;
        await onDemandLoader.WaitForActiveLoadsAsync();
        Assert.Equal(0, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task DevKitDisablesOnDemandLoading()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        folder.CreateFile("Project.csproj");
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var globalOptions = server.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LspOptionsStorage.LspUsingDevkitFeatures, true);
        using var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        Assert.True(load.IsCompletedSuccessfully);
        await load;
        await onDemandLoader.WaitForActiveLoadsAsync();
        Assert.Equal(0, loader.DesignTimeBuildCount);
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
