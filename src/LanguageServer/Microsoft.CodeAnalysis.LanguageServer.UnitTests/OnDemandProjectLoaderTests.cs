// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class OnDemandProjectLoaderTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerMefHost(testOutputHelper)
{
    [Fact]
    public async Task LoadsNearestProjectContainingRequestedDocument()
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspTestWorkspaces.CreateConsoleApplication("App"),
            CancellationToken.None);
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var documentPath = workspace.GetFullPath("Program.cs");

        await LoadDocumentAsync(server, workspace.RootPath, documentPath);

        var solution = server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution;
        Assert.Equal("App", Assert.Single(solution.Projects).AssemblyName);
        Assert.NotEmpty(solution.GetDocumentIdsWithFilePath(documentPath));
    }

    [Fact]
    public async Task LoadsTransitiveProjectReferences()
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspWorkspaceContent.Empty
                .WithFile("App/App.csproj", CreateProject("../Dependency/Dependency.csproj"))
                .WithFile("App/Program.cs", """Console.WriteLine("Hello");""")
                .WithFile("Dependency/Dependency.csproj", CreateProject("../Shared/Shared.csproj"))
                .WithFile("Dependency/Dependency.cs", "public static class Dependency;")
                .WithFile("Shared/Shared.csproj", CreateProject())
                .WithFile("Shared/Shared.cs", "public static class Shared;")
                .WithRestore(),
            CancellationToken.None);
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);

        await LoadDocumentAsync(server, workspace.RootPath, workspace.GetFullPath("App/Program.cs"));

        var solution = server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution;
        AssertEx.SetEqual(["App", "Dependency", "Shared"], solution.Projects.Select(project => project.AssemblyName));
    }

    [Fact]
    public async Task LoadsAllNearestProjectsAndOverlappingClosures()
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspWorkspaceContent.Empty
                .WithFile("src/A.csproj", CreateProject("../Shared/Shared.csproj"))
                .WithFile("src/B.csproj", CreateProject("../Shared/Shared.csproj"))
                .WithFile("src/Program.cs", """Console.WriteLine("Hello");""")
                .WithFile("Shared/Shared.csproj", CreateProject())
                .WithFile("Shared/Shared.cs", "public static class Shared;")
                .WithRestore(),
            CancellationToken.None);
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);

        await LoadDocumentAsync(server, workspace.RootPath, workspace.GetFullPath("src/Program.cs"));

        var solution = server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution;
        AssertEx.SetEqual(["A", "B", "Shared"], solution.Projects.Select(project => project.AssemblyName));
    }

    [Fact]
    public async Task DisabledOptionDoesNotLoadProjects()
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            CreateUnrestoredConsoleApplication(),
            CancellationToken.None);
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        server.ExportProvider.GetExportedValue<IGlobalOptionService>()
            .SetGlobalOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand, false);

        await LoadDocumentAsync(server, workspace.RootPath, workspace.GetFullPath("Program.cs"));

        Assert.Empty(server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task DevKitDoesNotLoadProjects()
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            CreateUnrestoredConsoleApplication(),
            CancellationToken.None);
        await using var server = await CreateLanguageServerAsync();

        await LoadDocumentAsync(server, workspace.RootPath, workspace.GetFullPath("Program.cs"));

        Assert.Empty(server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution.Projects);
    }

    private static LspWorkspaceContent CreateUnrestoredConsoleApplication()
        => LspWorkspaceContent.Empty
            .WithFile("App.csproj", CreateProject())
            .WithFile("Program.cs", """Console.WriteLine("Hello");""");

    private static string CreateProject(string? projectReference = null)
        => $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            {{(projectReference is null ? "" : $"""
              <ItemGroup>
                <ProjectReference Include="{projectReference}" />
              </ItemGroup>
            """)}}
            </Project>
            """;

    private static async Task LoadDocumentAsync(TestLspServer server, string workspaceRoot, string documentPath)
    {
        server.GetRequiredLspService<IWorkspaceFolderTracker>().Update(
            [new WorkspaceFolder
            {
                DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(workspaceRoot),
                Name = Path.GetFileName(workspaceRoot),
            }],
            removedFolders: null);

        await server.GetRequiredLspService<IOnDemandProjectLoader>()
            .StartLoadingAsync(ProtocolConversions.CreateAbsoluteDocumentUri(documentPath))
            .WaitAsync(TestHelpers.HangMitigatingTimeout);
    }
}
