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

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class WorkspaceProjectDiscoveryServiceTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public async Task NestedWorkspaceUsesDeepestBoundary()
    {
        var outerWorkspace = _tempRoot.CreateDirectory();
        var outerProject = outerWorkspace.CreateFile("Outer.csproj");
        var innerWorkspace = outerWorkspace.CreateDirectory("inner");
        var codeFile = innerWorkspace.CreateDirectory("src").CreateFile("Program.cs");
        var service = CreateDiscoveryService();

        var candidates = service.DiscoverProjects(codeFile.Path, [outerWorkspace.Path, innerWorkspace.Path], CancellationToken.None);

        Assert.Empty(candidates);
        Assert.True(File.Exists(outerProject.Path));
    }

    [Fact]
    public async Task ReturnsAllSupportedProjectsFromNearestAncestorInOrdinalOrder()
    {
        var workspace = _tempRoot.CreateDirectory();
        workspace.CreateFile("Root.csproj");
        var sourceDirectory = workspace.CreateDirectory("src");
        var secondProject = sourceDirectory.CreateFile("B.csproj");
        sourceDirectory.CreateFile("Unsupported.vbproj");
        var firstProject = sourceDirectory.CreateFile("A.csproj");
        var codeFile = sourceDirectory.CreateDirectory("nested").CreateFile("Program.cs");
        var service = CreateDiscoveryService();

        var candidates = service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None);

        AssertEx.Equal([firstProject.Path, secondProject.Path], candidates);
    }

    [Fact]
    public async Task FileOutsideWorkspaceReturnsNoCandidatesOrEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var codeFile = _tempRoot.CreateDirectory().CreateFile("Program.cs");
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return EnumerateFiles(directory);
            });
        var candidates = service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None);

        Assert.Empty(candidates);
        Assert.Equal(0, enumerationCount);
    }

    [Fact]
    public void FileAtWorkspaceRootEnumeratesTheRootDirectory()
    {
        var root = Path.GetPathRoot(_tempRoot.CreateDirectory().Path);
        Assert.NotNull(root);
        string? enumeratedDirectory = null;
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                enumeratedDirectory = directory;
                return [];
            });

        var candidates = service.DiscoverProjects(Path.Combine(root, "Program.cs"), [root], CancellationToken.None);

        Assert.Empty(candidates);
        Assert.Equal(root, enumeratedDirectory);
    }

    [Fact]
    public async Task RechecksFileSystemOnEveryDemand()
    {
        var workspace = _tempRoot.CreateDirectory();
        var codeFile = workspace.CreateFile("Program.cs");
        var service = CreateDiscoveryService();

        Assert.Empty(service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None));

        var firstProject = workspace.CreateFile("First.csproj");
        var secondProject = workspace.CreateFile("Second.csproj");
        AssertEx.Equal(
            [firstProject.Path, secondProject.Path],
            service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None));

        File.Delete(firstProject.Path);
        AssertEx.Equal(
            [secondProject.Path],
            service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None));

        File.Delete(secondProject.Path);
        Assert.Empty(service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentLookupsEnumerateIndependently()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        using var bothEnumerationsStarted = new CountdownEvent(2);
        using var releaseEnumerations = new ManualResetEventSlim();
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                bothEnumerationsStarted.Signal();
                Assert.True(releaseEnumerations.Wait(TestHelpers.HangMitigatingTimeout));
                return EnumerateFiles(directory);
            });
        var firstLookup = Task.Run(() => service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None));
        var secondLookup = Task.Run(() => service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None));
        Assert.True(bothEnumerationsStarted.Wait(TestHelpers.HangMitigatingTimeout));
        releaseEnumerations.Set();

        var results = await Task.WhenAll(firstLookup, secondLookup).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.All(results, candidates => AssertEx.Equal([project.Path], candidates));
    }

    [Fact]
    public async Task CancellationBeforeLookupStopsEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var codeFile = workspace.CreateFile("Program.cs");
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            enumerateFiles: _ =>
            {
                Interlocked.Increment(ref enumerationCount);
                return [];
            });
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(
            () => service.DiscoverProjects(codeFile.Path, [workspace.Path], cancellationSource.Token));
        Assert.Equal(0, enumerationCount);
    }

    [Fact]
    public async Task EnumerationFailureContinuesToParent()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var childDirectory = workspace.CreateDirectory("src");
        var codeFile = childDirectory.CreateFile("Program.cs");
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(directory, childDirectory.Path))
                    throw new IOException("Expected test failure");

                return EnumerateFiles(directory);
            });
        var candidates = service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None);

        AssertEx.Equal([project.Path], candidates);
    }

    private static WorkspaceProjectDiscoveryService CreateDiscoveryService(
        Func<string, ImmutableArray<string>>? enumerateFiles = null)
    {
        return new(
            NullLoggerFactory.Instance,
            supportedProjectFileExtensions: ["csproj"],
            enumerateFiles);
    }

    private static ImmutableArray<string> EnumerateFiles(string directory)
        => [.. Directory.EnumerateFiles(directory, searchPattern: "*", SearchOption.TopDirectoryOnly)];
}
