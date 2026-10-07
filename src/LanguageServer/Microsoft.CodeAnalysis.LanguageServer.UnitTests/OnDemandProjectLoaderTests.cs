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
        var document = projectFolder.CreateDirectory("Source").CreateFile("Document.cs").Path;
        var discovery = new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory);

        AssertEx.SetEqual(
            [project],
            discovery.DiscoverProjects(document, ImmutableHashSet.Create(PathUtilities.Comparer, workspaceFolder.Path)));
    }

    [Fact]
    public void DiscoveryStopsAtDeepestWorkspaceFolder()
    {
        var outerFolder = TempRoot.CreateDirectory();
        outerFolder.CreateFile("Outer.csproj");
        var innerFolder = outerFolder.CreateDirectory("Inner");
        var document = innerFolder.CreateDirectory("Source").CreateFile("Document.cs").Path;
        var workspaceFolders = ImmutableHashSet.Create(PathUtilities.Comparer, outerFolder.Path, innerFolder.Path);
        var discovery = new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory);

        Assert.Empty(discovery.DiscoverProjects(document, workspaceFolders));
    }

    [Fact]
    public void DiscoveryFiltersUnsupportedProjectExtensions()
    {
        var workspaceFolder = TempRoot.CreateDirectory();
        var supportedProject = workspaceFolder.CreateFile("Supported.csproj").Path;
        workspaceFolder.CreateFile("Unsupported.vbproj");
        var document = workspaceFolder.CreateFile("Document.cs").Path;
        var discovery = new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory);

        AssertEx.SetEqual(
            [supportedProject],
            discovery.DiscoverProjects(document, ImmutableHashSet.Create(PathUtilities.Comparer, workspaceFolder.Path)));
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
        var build = loader.QueueDesignTimeBuild();

        var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
        Assert.Equal(projectPath, await build.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout), PathUtilities.Comparer);
        var snapshot = onDemandLoader.WaitForActiveLoadsAsync();
        Assert.False(snapshot.IsCompleted);
        build.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, projectReferences: [projectPath]);

        Assert.NotNull(await load.AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.NotNull(await snapshot.AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
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
        var firstBuild = loader.QueueDesignTimeBuild();
        var secondBuild = loader.QueueDesignTimeBuild();

        var firstLoad = onDemandLoader.TryLoadProjectsAsync(firstDocument);
        Task<Solution?>? secondLoad = concurrentDemands ? onDemandLoader.TryLoadProjectsAsync(secondDocument).AsTask() : null;
        var firstStartedPath = await firstBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        firstBuild.CompleteSuccessfully(
            loader.WorkspaceFactory.HostProjectFactory,
            firstStartedPath,
            projectReferences: [PathUtilities.Comparer.Equals(firstStartedPath, firstProject) ? secondProject : firstProject]);
        var secondStartedPath = await secondBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        secondBuild.CompleteSuccessfully(
            loader.WorkspaceFactory.HostProjectFactory,
            secondStartedPath,
            projectReferences: [PathUtilities.Comparer.Equals(secondStartedPath, firstProject) ? secondProject : firstProject]);

        if (secondLoad is not null)
            Assert.All(await Task.WhenAll(firstLoad.AsTask(), secondLoad).WaitAsync(TestHelpers.HangMitigatingTimeout), Assert.NotNull);
        else
            Assert.NotNull(await firstLoad.AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
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

        Assert.Null(await onDemandLoader.TryLoadProjectsAsync(documentUri).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
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
        Assert.True(load.IsCompletedSuccessfully);
        Assert.Null(await load);
        Assert.Equal(0, loader.DesignTimeBuildCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisabledOnDemandLoadingDoesNotStartBuild(bool disableOnDemandLoading)
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var folder = TempRoot.CreateDirectory();
        folder.CreateFile("Project.csproj");
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.CreateFile("Document.cs").Path);
        var globalOptions = server.ExportProvider.GetExportedValue<IGlobalOptionService>();
        var option = disableOnDemandLoading
            ? LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand
            : LspOptionsStorage.LspUsingDevkitFeatures;
        var originalValue = globalOptions.GetOption(option);

        try
        {
            globalOptions.SetGlobalOption(option, !disableOnDemandLoading);
            var onDemandLoader = CreateOnDemandLoader(server, loader, folder.Path);

            var load = onDemandLoader.TryLoadProjectsAsync(documentUri);
            Assert.True(load.IsCompletedSuccessfully);
            Assert.Null(await load);
            Assert.True(onDemandLoader.WaitForActiveLoadsAsync().IsCompletedSuccessfully);
            Assert.Equal(0, loader.DesignTimeBuildCount);
        }
        finally
        {
            globalOptions.SetGlobalOption(option, originalValue);
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
