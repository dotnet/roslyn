// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class OnDemandProjectLoaderDiscoveryTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public void UsesDeepestContainingWorkspaceFolder()
    {
        var outerWorkspace = _tempRoot.CreateDirectory();
        outerWorkspace.CreateFile("Outer.csproj");
        var innerWorkspace = outerWorkspace.CreateDirectory("inner");
        var document = innerWorkspace.CreateDirectory("src").CreateFile("Program.cs");
        var (discovery, workspaceFolders) = CreateDiscovery(outerWorkspace.Path, innerWorkspace.Path);

        Assert.Empty(discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None));
    }

    [Fact]
    public void ReturnsAllSupportedProjectsFromNearestAncestorInOrdinalOrder()
    {
        var workspace = _tempRoot.CreateDirectory();
        workspace.CreateFile("Root.csproj");
        var projectDirectory = workspace.CreateDirectory("src");
        var secondProject = projectDirectory.CreateFile("B.csproj");
        projectDirectory.CreateFile("Unsupported.vbproj");
        var firstProject = projectDirectory.CreateFile("A.csproj");
        var document = projectDirectory.CreateDirectory("nested").CreateFile("Program.cs");
        var (discovery, workspaceFolders) = CreateDiscovery(workspace.Path);

        AssertEx.Equal(
            [firstProject.Path, secondProject.Path],
            discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None));
    }

    [Fact]
    public void FileOutsideWorkspaceDoesNotDiscoverProjects()
    {
        var workspace = _tempRoot.CreateDirectory();
        workspace.CreateFile("Project.csproj");
        var document = _tempRoot.CreateDirectory().CreateFile("Program.cs");
        var (discovery, workspaceFolders) = CreateDiscovery(workspace.Path);

        Assert.Empty(discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None));
    }

    [Fact]
    public void ObservesProjectCreationAndDeletionOnEachDemand()
    {
        var workspace = _tempRoot.CreateDirectory();
        var document = workspace.CreateFile("Program.cs");
        var (discovery, workspaceFolders) = CreateDiscovery(workspace.Path);

        Assert.Empty(discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None));

        var firstProject = workspace.CreateFile("First.csproj");
        var secondProject = workspace.CreateFile("Second.csproj");
        AssertEx.Equal(
            [firstProject.Path, secondProject.Path],
            discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None));

        File.Delete(firstProject.Path);
        AssertEx.Equal(
            [secondProject.Path],
            discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentDemandsPerformIndependentSearches()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var document = workspace.CreateFile("Program.cs");
        var (discovery, workspaceFolders) = CreateDiscovery(workspace.Path);

        var searches = await Task.WhenAll(
            Task.Run(() => discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None)),
            Task.Run(() => discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None)));

        AssertEx.Equal([project.Path], searches[0]);
        AssertEx.Equal([project.Path], searches[1]);
    }

    [Fact]
    public void EnumerationFailureContinuesToParentDirectory()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var missingDocumentPath = Path.Combine(workspace.Path, "missing", "Program.cs");
        var (discovery, workspaceFolders) = CreateDiscovery(workspace.Path);

        AssertEx.Equal(
            [project.Path],
            discovery.DiscoverProjects(missingDocumentPath, workspaceFolders, CancellationToken.None));
    }

    [Fact]
    public void CancellationStopsSearch()
    {
        var workspace = _tempRoot.CreateDirectory();
        var document = workspace.CreateFile("Program.cs");
        var (discovery, workspaceFolders) = CreateDiscovery(workspace.Path);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(
            () => discovery.DiscoverProjects(document.Path, workspaceFolders, cancellationSource.Token));
    }

    [Fact]
    public void WorkspaceContainmentUsesPlatformPathIdentity()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var document = workspace.CreateFile("Program.cs");
        var differentlyCasedWorkspacePath = workspace.Path.ToUpperInvariant();
        var (discovery, workspaceFolders) = CreateDiscovery(differentlyCasedWorkspacePath);

        var projects = discovery.DiscoverProjects(document.Path, workspaceFolders, CancellationToken.None);
        if (PathUtilities.IsUnixLikePlatform)
            Assert.Empty(projects);
        else
            AssertEx.Equal([project.Path], projects);
    }

    private static (OnDemandProjectLoader.ProjectDiscovery discovery, ImmutableHashSet<string> workspaceFolders) CreateDiscovery(
        params string[] workspacePaths)
    {
        var tracker = new WorkspaceFolderTracker();
        tracker.Update(
            [.. workspacePaths.Select(static path => new WorkspaceFolder
            {
                DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(path),
                Name = Path.GetFileName(path),
            })],
            removedFolders: null);

        return (
            new OnDemandProjectLoader.ProjectDiscovery(
                supportedProjectFileExtensions: [".csproj"],
                NullLoggerFactory.Instance),
            tracker.GetRequiredWorkspaceFolderPaths());
    }
}
