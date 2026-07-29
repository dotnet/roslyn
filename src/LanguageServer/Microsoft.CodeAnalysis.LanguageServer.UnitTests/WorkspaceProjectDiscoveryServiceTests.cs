// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class WorkspaceProjectDiscoveryServiceTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();
    private static readonly TimeSpan s_eventualTimeout = TimeSpan.FromSeconds(5);

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public async Task DiscoveryService_ReturnsCandidateProjectForFile()
    {
        var workspace = _tempRoot.CreateDirectory();
        var srcDir = workspace.CreateDirectory("src");
        var nestedDir = srcDir.CreateDirectory("Nested").CreateDirectory("Deep");

        var srcProjectFile = srcDir.CreateFile("Src.csproj");
        srcProjectFile.WriteAllText("<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var codeFile = nestedDir.CreateFile("Program.cs");
        codeFile.WriteAllText("class C { }");

        var service = CreateDiscoveryService();
        var accessor = service.GetTestAccessor();
        accessor.AddWorkspaceFolder(workspace.Path);

        var candidates = await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);
        Assert.NotEmpty(candidates);
        Assert.Contains(candidates, p => string.Equals(p, srcProjectFile.Path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DiscoveryService_AddsAndRemovesWorkspaceFolders()
    {
        var workspace1 = _tempRoot.CreateDirectory();
        var workspace2 = _tempRoot.CreateDirectory();

        var workspace1Project = workspace1.CreateFile("Workspace1.csproj");
        workspace1Project.WriteAllText("<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var workspace2Project = workspace2.CreateFile("Workspace2.csproj");
        workspace2Project.WriteAllText("<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var workspace1Code = workspace1.CreateFile("Program1.cs");
        workspace1Code.WriteAllText("class C1 { }");
        var workspace2Code = workspace2.CreateFile("Program2.cs");
        workspace2Code.WriteAllText("class C2 { }");

        var service = CreateDiscoveryService();
        var accessor = service.GetTestAccessor();

        accessor.AddWorkspaceFolder(workspace1.Path);

        var workspace2CandidatesBeforeAdd = await accessor.GetCandidateProjectsAsync(workspace2Code.Path, CancellationToken.None);
        Assert.Empty(workspace2CandidatesBeforeAdd);

        accessor.AddWorkspaceFolder(workspace2.Path);
        var workspace2CandidatesAfterAdd = await accessor.GetCandidateProjectsAsync(workspace2Code.Path, CancellationToken.None);
        Assert.Single(workspace2CandidatesAfterAdd);
        Assert.Equal(workspace2Project.Path, workspace2CandidatesAfterAdd[0], ignoreCase: true);

        accessor.RemoveWorkspaceFolder(workspace1.Path);
        var workspace1CandidatesAfterRemove = await accessor.GetCandidateProjectsAsync(workspace1Code.Path, CancellationToken.None);
        Assert.Empty(workspace1CandidatesAfterRemove);
    }

    [Fact]
    public async Task DiscoveryService_AddsProjectWhenCsprojCreated()
    {
        var workspace = _tempRoot.CreateDirectory();
        var codeFile = workspace.CreateFile("Program.cs");
        codeFile.WriteAllText("class C { }");

        var service = CreateDiscoveryService();
        var accessor = service.GetTestAccessor();
        accessor.AddWorkspaceFolder(workspace.Path);

        var candidatesBeforeCreate = await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);
        Assert.Empty(candidatesBeforeCreate);

        var projectFile = workspace.CreateFile("AddedLater.csproj");
        projectFile.WriteAllText("<Project Sdk=\"Microsoft.NET.Sdk\" />");
        accessor.NotifyProjectFileChanged(workspace.Path, projectFile.Path);

        await AssertEventuallyAsync(async () =>
        {
            var candidates = await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);
            return candidates.Length == 1 && string.Equals(candidates[0], projectFile.Path, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task DiscoveryService_RemovesProjectWhenCsprojDeleted()
    {
        var workspace = _tempRoot.CreateDirectory();
        var firstProjectFile = workspace.CreateFile("First.csproj");
        firstProjectFile.WriteAllText("<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var secondProjectFile = workspace.CreateFile("Second.csproj");
        secondProjectFile.WriteAllText("<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var secondCodeFile = workspace.CreateFile("Second.cs");
        secondCodeFile.WriteAllText("class Second { }");

        var service = CreateDiscoveryService();
        var accessor = service.GetTestAccessor();
        accessor.AddWorkspaceFolder(workspace.Path);

        var secondCandidatesBeforeDelete = await accessor.GetCandidateProjectsAsync(secondCodeFile.Path, CancellationToken.None);
        Assert.Equal(2, secondCandidatesBeforeDelete.Length);
        Assert.Contains(secondCandidatesBeforeDelete, p => string.Equals(p, secondProjectFile.Path, StringComparison.OrdinalIgnoreCase));

        File.Delete(secondProjectFile.Path);
        accessor.NotifyProjectFileChanged(workspace.Path, secondProjectFile.Path);

        await AssertEventuallyAsync(async () =>
        {
            var candidates = await accessor.GetCandidateProjectsAsync(secondCodeFile.Path, CancellationToken.None);
            return candidates.Length == 1
                && !candidates.Any(p => string.Equals(p, secondProjectFile.Path, StringComparison.OrdinalIgnoreCase))
                && candidates.Any(p => string.Equals(p, firstProjectFile.Path, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static WorkspaceProjectDiscoveryService CreateDiscoveryService()
        => new(NullLoggerFactory.Instance, new TestFileChangeWatcher());

    private static async Task AssertEventuallyAsync(Func<Task<bool>> condition)
    {
        var timeoutAt = DateTime.UtcNow + s_eventualTimeout;
        while (DateTime.UtcNow < timeoutAt)
        {
            if (await condition().ConfigureAwait(false))
                return;

            await Task.Delay(50).ConfigureAwait(false);
        }

        Assert.True(false, "Condition was not satisfied within the timeout window.");
    }

    private sealed class TestFileChangeWatcher : IFileChangeWatcher
    {
        public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
            => new TestFileChangeContext();
    }

    private sealed class TestFileChangeContext : IFileChangeContext
    {
#pragma warning disable CS0067
        public event EventHandler<string>? FileChanged;
#pragma warning restore CS0067

        public IWatchedFile EnqueueWatchingFile(string filePath)
            => NoOpWatchedFile.Instance;

        public void Dispose()
        {
        }
    }
}
