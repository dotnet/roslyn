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
        var service = CreateDiscoveryService();
        var candidates = service.DiscoverProjects(codeFile.Path, [workspace.Path], CancellationToken.None);

        Assert.Empty(candidates);
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
    public async Task CancellationBeforeLookupStopsEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var codeFile = workspace.CreateFile("Program.cs");
        var service = CreateDiscoveryService();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(
            () => service.DiscoverProjects(codeFile.Path, [workspace.Path], cancellationSource.Token));
    }

    [Fact]
    public async Task EnumerationFailureContinuesToParent()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var missingCodeFilePath = Path.Combine(workspace.Path, "missing", "Program.cs");
        var service = CreateDiscoveryService();
        var candidates = service.DiscoverProjects(missingCodeFilePath, [workspace.Path], CancellationToken.None);

        AssertEx.Equal([project.Path], candidates);
    }

    private static WorkspaceProjectDiscoveryService CreateDiscoveryService()
        => new(
            NullLoggerFactory.Instance,
            supportedProjectFileExtensions: [".csproj"]);
}
