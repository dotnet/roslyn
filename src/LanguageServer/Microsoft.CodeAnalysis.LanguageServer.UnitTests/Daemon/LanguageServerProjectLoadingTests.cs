// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerProjectLoadingTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerMefHost(testOutputHelper)
{
    [Fact]
    public async Task DefinitionRequestLoadsProjectOnDemandAsync()
    {
        const string source = "Target value = new();";
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspTestWorkspaces.CreateConsoleApplication("ConsoleApplication")
                .WithFile("Program.cs", source)
                .WithFile("Target.cs", "class Target { }"),
            CancellationToken.None);
        var sourceUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.GetFullPath("Program.cs"));
        var targetUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.GetFullPath("Target.cs"));

        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        using var timeout = new CancellationTokenSource(TestHelpers.HangMitigatingTimeout);
        await server.ExecuteNotificationAsync(Methods.WorkspaceDidChangeWorkspaceFoldersName, new DidChangeWorkspaceFoldersParams
        {
            Event = new WorkspaceFoldersChangeEvent
            {
                Added = [new WorkspaceFolder { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.RootPath), Name = "workspace" }],
                Removed = []
            }
        });
        await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                DocumentUri = sourceUri,
                LanguageId = "csharp",
                Version = 1,
                Text = source
            }
        }, timeout.Token);

        var hostWorkspace = server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace;
        Assert.Empty(hostWorkspace.CurrentSolution.Projects);

        var definitions = await server.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(
            Methods.TextDocumentDefinitionName,
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { DocumentUri = sourceUri },
                Position = new Position(0, 1)
            },
            timeout.Token);

        Assert.NotNull(definitions);
        var definition = Assert.Single(definitions);
        Assert.Equal(targetUri, definition.DocumentUri);
        Assert.Equal(0, definition.Range.Start.Line);
        Assert.Equal(6, definition.Range.Start.Character);
        Assert.Equal("ConsoleApplication", Assert.Single(hostWorkspace.CurrentSolution.Projects).AssemblyName);
        Assert.Single(hostWorkspace.CurrentSolution.GetDocumentIds(sourceUri));
    }

    [Fact]
    public async Task DefinitionRequestLoadsReferencedProjectOnDemandAsync()
    {
        const string source = "Target value = new();";
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspWorkspaceContent.Empty
                .WithFile("Application/Application.csproj", """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                      </PropertyGroup>
                      <ItemGroup>
                        <ProjectReference Include="../Library/Library.csproj" />
                      </ItemGroup>
                    </Project>
                    """)
                .WithFile("Application/Program.cs", source)
                .WithFile("Library/Library.csproj", """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                      </PropertyGroup>
                    </Project>
                    """)
                .WithFile("Library/Target.cs", "public class Target { }")
                .WithRestore(),
            CancellationToken.None);
        var sourceUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.GetFullPath("Application/Program.cs"));
        var targetUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.GetFullPath("Library/Target.cs"));

        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        using var timeout = new CancellationTokenSource(TestHelpers.HangMitigatingTimeout);
        await server.ExecuteNotificationAsync(Methods.WorkspaceDidChangeWorkspaceFoldersName, new DidChangeWorkspaceFoldersParams
        {
            Event = new WorkspaceFoldersChangeEvent
            {
                Added = [new WorkspaceFolder { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspace.RootPath), Name = "workspace" }],
                Removed = []
            }
        });
        await server.ExecuteRequestAsync<DidOpenTextDocumentParams, object>(Methods.TextDocumentDidOpenName, new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                DocumentUri = sourceUri,
                LanguageId = "csharp",
                Version = 1,
                Text = source
            }
        }, timeout.Token);

        var hostWorkspace = server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace;
        Assert.Empty(hostWorkspace.CurrentSolution.Projects);

        var definitions = await server.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(
            Methods.TextDocumentDefinitionName,
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { DocumentUri = sourceUri },
                Position = new Position(0, 1)
            },
            timeout.Token);

        var definition = Assert.Single(Assert.IsType<LSP.Location[]>(definitions));
        Assert.Equal(targetUri, definition.DocumentUri);
        Assert.Equal(0, definition.Range.Start.Line);
        Assert.Equal(13, definition.Range.Start.Character);
        Assert.Equal(2, hostWorkspace.CurrentSolution.Projects.Count());
        Assert.Single(hostWorkspace.CurrentSolution.GetDocumentIds(sourceUri));
        Assert.Single(hostWorkspace.CurrentSolution.GetDocumentIds(targetUri));
    }

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
