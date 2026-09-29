// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerProjectLoadingTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerMefHost(testOutputHelper)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadProjectAsync(bool useDaemon)
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspTestWorkspaces.CreateConsoleApplication("ConsoleApplication"),
            CancellationToken.None);
        var projectPath = workspace.GetFullPath(workspace.Content.LoadPath!);

        if (useDaemon)
        {
            await using var daemon = await CreateDaemonServerAsync();
            await using var server = await daemon.CreateClientAsync();
            await VerifyProjectLoadsAsync(server, projectPath);
        }
        else
        {
            await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
            await VerifyProjectLoadsAsync(server, projectPath);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadSolutionAsync(bool useDaemon)
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspTestWorkspaces.CreateConsoleApplication("ConsoleApplication")
                .WithFile("ConsoleApplication.slnx", """
                    <Solution>
                      <Project Path="ConsoleApplication.csproj" />
                    </Solution>
                    """),
            CancellationToken.None);
        var solutionPath = workspace.GetFullPath("ConsoleApplication.slnx");

        if (useDaemon)
        {
            await using var daemon = await CreateDaemonServerAsync();
            await using var server = await daemon.CreateClientAsync();
            await VerifySolutionLoadsAsync(server, solutionPath);
        }
        else
        {
            await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
            await VerifySolutionLoadsAsync(server, solutionPath);
        }
    }

    [Fact]
    public async Task LoadProjectsIntoSeparateStandaloneServersAsync()
    {
        var firstWorkspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspTestWorkspaces.CreateConsoleApplication("FirstConsoleApplication"),
            CancellationToken.None);
        var secondWorkspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspTestWorkspaces.CreateConsoleApplication("SecondConsoleApplication"),
            CancellationToken.None);
        var firstProjectPath = firstWorkspace.GetFullPath(firstWorkspace.Content.LoadPath!);
        var secondProjectPath = secondWorkspace.GetFullPath(secondWorkspace.Content.LoadPath!);

        await using var firstServer = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        await using var secondServer = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        await Task.WhenAll(
            VerifyProjectLoadsAsync(firstServer, firstProjectPath, "FirstConsoleApplication"),
            VerifyProjectLoadsAsync(secondServer, secondProjectPath, "SecondConsoleApplication"));
    }

    private static async Task VerifyProjectLoadsAsync(TestLspServer server, string projectPath)
        => await VerifyProjectLoadsAsync(server, projectPath, "ConsoleApplication");

    private static async Task VerifyProjectLoadsAsync(TestLspServer server, string projectPath, string expectedAssemblyName)
    {
        await server.OpenProjectsAsync([projectPath], CancellationToken.None);

        var workspaceFactory = server.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        Assert.Equal(expectedAssemblyName, workspaceFactory.HostWorkspace.CurrentSolution.Projects.Single().AssemblyName);
    }

    private static async Task VerifySolutionLoadsAsync(TestLspServer server, string solutionPath)
    {
        await server.OpenSolutionAsync(solutionPath, CancellationToken.None);

        var workspaceFactory = server.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        Assert.Equal("ConsoleApplication", workspaceFactory.HostWorkspace.CurrentSolution.Projects.Single().AssemblyName);
    }
}
