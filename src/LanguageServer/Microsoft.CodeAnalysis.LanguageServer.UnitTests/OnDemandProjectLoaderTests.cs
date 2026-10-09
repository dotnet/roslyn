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
            discovery.DiscoverProjects(sourceFolder.Path, ImmutableHashSet.Create(PathUtilities.Comparer, workspaceFolder.Path)));
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

        Assert.Empty(discovery.DiscoverProjects(sourceFolder.Path, workspaceFolders));
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
            discovery.DiscoverProjects(workspaceFolder.Path, ImmutableHashSet.Create(PathUtilities.Comparer, workspaceFolder.Path)));
    }

    [Fact]
    public async Task SelfReferenceCompletes()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var projectPath = folder.CreateFile("Project.csproj").Path;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        var build = loader.ExpectDesignTimeBuild(projectPath);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        await build.Started.Task;
        var snapshot = onDemandLoader.WaitForActiveLoadsAsync();
        Assert.False(snapshot.IsCompleted);
        build.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, projectReferences: [projectPath]);

        Assert.NotNull(await load);
        Assert.NotNull(await snapshot);
        Assert.Equal(1, loader.DesignTimeBuildCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MutualReferencesComplete(bool concurrentDemands)
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var firstFolder = folder.CreateDirectory("First");
        var secondFolder = folder.CreateDirectory("Second");
        var firstProject = firstFolder.CreateFile("First.csproj").Path;
        var secondProject = secondFolder.CreateFile("Second.csproj").Path;
        var firstDocument = ProtocolConversions.CreateAbsoluteDocumentUri(firstFolder.CreateFile("First.cs").Path);
        var secondDocument = ProtocolConversions.CreateAbsoluteDocumentUri(secondFolder.CreateFile("Second.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);
        var firstBuild = loader.ExpectDesignTimeBuild(firstProject);
        var secondBuild = loader.ExpectDesignTimeBuild(secondProject);

        var firstLoad = onDemandLoader.TryLoadProjectsAsync(firstDocument);
        Task<Solution?>? secondLoad = concurrentDemands ? onDemandLoader.TryLoadProjectsAsync(secondDocument).AsTask() : null;
        await firstBuild.Started.Task;
        firstBuild.CompleteSuccessfully(
            loader.WorkspaceFactory.HostProjectFactory,
            firstProject,
            projectReferences: [Path.GetRelativePath(Path.GetDirectoryName(firstProject)!, secondProject)]);
        await secondBuild.Started.Task;
        secondBuild.CompleteSuccessfully(
            loader.WorkspaceFactory.HostProjectFactory,
            secondProject,
            projectReferences: [Path.GetRelativePath(Path.GetDirectoryName(secondProject)!, firstProject)]);

        if (secondLoad is not null)
            Assert.All(await Task.WhenAll(firstLoad.AsTask(), secondLoad), Assert.NotNull);
        else
            Assert.NotNull(await firstLoad);
        Assert.Equal(2, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task NoDiscoveredProjectReturnsNull()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Loose.cs").Path);
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        Assert.Null(await onDemandLoader.TryLoadProjectsAsync(documentUri));
        Assert.Equal(0, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task NonFileUriReturnsNull()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        var load = onDemandLoader.TryLoadProjectsAsync(new LSP.DocumentUri("untitled:Loose.cs"));
        // Unsupported URIs are rejected before project discovery starts.
        var completedSynchronously = load.IsCompleted;
        Assert.Null(await load);
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
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        Assert.True(load.IsCompletedSuccessfully);
        Assert.Null(await load);
        Assert.True(onDemandLoader.WaitForActiveLoadsAsync().IsCompletedSuccessfully);
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
        var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        Assert.True(load.IsCompletedSuccessfully);
        Assert.Null(await load);
        Assert.True(onDemandLoader.WaitForActiveLoadsAsync().IsCompletedSuccessfully);
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
