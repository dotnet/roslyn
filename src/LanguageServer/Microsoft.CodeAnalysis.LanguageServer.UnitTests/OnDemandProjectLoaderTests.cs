// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class OnDemandProjectLoaderTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerMefHost(testOutputHelper)
{
    [Fact]
    public async Task LoadsProjectContainingRequestedDocument()
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
    public async Task LoadsTransitiveProjectReferencesFromEvaluationData()
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            LspWorkspaceContent.Empty
                .WithFile("App/App.csproj", """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <OutputType>Exe</OutputType>
                        <TargetFramework>net10.0</TargetFramework>
                      </PropertyGroup>
                      <ItemGroup>
                        <ProjectReference Include="../Dependency/Dependency.csproj" />
                      </ItemGroup>
                    </Project>
                    """)
                .WithFile("App/Program.cs", """Console.WriteLine("Hello");""")
                .WithFile("Dependency/Dependency.csproj", """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                      </PropertyGroup>
                    </Project>
                    """)
                .WithFile("Dependency/Dependency.cs", "public static class Dependency;")
                .WithRestore(),
            CancellationToken.None);
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);

        await LoadDocumentAsync(server, workspace.RootPath, workspace.GetFullPath("App/Program.cs"));

        var solution = server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution;
        AssertEx.SetEqual(["App", "Dependency"], solution.Projects.Select(project => project.AssemblyName));
    }

    [Fact]
    public async Task DisabledDoesNotLoadProject()
    {
        var workspace = MaterializedLspWorkspace.Create(
            TempRoot,
            CreateUnrestoredConsoleApplication(),
            CancellationToken.None);
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var globalOptions = server.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand, false);

        await LoadDocumentAsync(server, workspace.RootPath, workspace.GetFullPath("Program.cs"));

        Assert.Empty(server.GetRequiredLspService<LanguageServerWorkspaceFactory>().HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task DevKitDoesNotLoadProject()
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
            .WithFile("App.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """)
            .WithFile("Program.cs", """Console.WriteLine("Hello");""");

    private static async Task LoadDocumentAsync(TestLspServer server, string workspaceRoot, string documentPath)
    {
        var loader = server.GetRequiredLspService<IOnDemandProjectLoader>();
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
        await loader.StartLoadingAsync(documentUri, [workspaceRoot]).WaitAsync(TestHelpers.HangMitigatingTimeout);
    }
}
