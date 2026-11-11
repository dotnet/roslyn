// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerProjectSystemTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact]
    public async Task OpenProjectsDoesNotLoadOrLogSharedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var logMessages = new ConcurrentQueue<string>();
        server.LogMessageReceived += logMessage => logMessages.Enqueue(logMessage.Message);
        var projectPath = TempRoot.CreateDirectory().CreateFile("Unsupported.shproj").Path;

        await server.OpenProjectsAsync([projectPath], CancellationToken.None);

        var projectSystem = server.GetRequiredLspService<LanguageServerProjectSystem>();
        Assert.Empty(await projectSystem.GetTestAccessor().GetLoadedProjectPathsAsync());
        Assert.Empty(server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution.Projects);
        Assert.DoesNotContain(logMessages, message => IsUnsupportedProjectLog(message, projectPath));
    }

    [Fact]
    public async Task OpenProjectsLogsUnsupportedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var logMessages = new ConcurrentQueue<string>();
        server.LogMessageReceived += logMessage => logMessages.Enqueue(logMessage.Message);
        var projectPath = TempRoot.CreateDirectory().CreateFile("Unsupported.unsupportedproj").Path;

        await server.OpenProjectsAsync([projectPath], CancellationToken.None);

        var projectSystem = server.GetRequiredLspService<LanguageServerProjectSystem>();
        Assert.Empty(await projectSystem.GetTestAccessor().GetLoadedProjectPathsAsync());
        Assert.Empty(server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution.Projects);
        Assert.Contains(logMessages, message => IsUnsupportedProjectLog(message, projectPath));
    }

    private static bool IsUnsupportedProjectLog(string message, string projectPath)
        => message.Contains($"Not loading project '{projectPath}' because its language is not supported.", StringComparison.Ordinal);
}
