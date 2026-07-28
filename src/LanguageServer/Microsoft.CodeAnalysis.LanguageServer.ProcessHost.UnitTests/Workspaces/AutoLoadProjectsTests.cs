// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests.Workspaces;

public sealed class AutoLoadProjectsTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerClientTests(testOutputHelper)
{
    private const string ProjectContent = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Library</OutputType>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    [Fact]
    public async Task LoadsAllProjectsInFolderAndSubdirectories()
    {
        var workspaceContent = CreateAutoLoadWorkspace()
            .WithFile("Project.csproj", ProjectContent)
            .WithFile("Nested/Nested.csproj", ProjectContent);

        await using var testLspServer = await CreateAutoLoadLanguageServerAsync(workspaceContent);

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingProjectsMessage(projectCount: 2), GetLoadedProjectsMessage(projectCount: 2));
    }

    [Fact]
    public async Task LoadsSingleSlnFileAtRoot()
    {
        var workspaceContent = CreateAutoLoadWorkspace()
            .WithFile("App/App.csproj", ProjectContent)
            .WithFile("App.sln", CreateSolutionFile("App/App.csproj"));

        await using var testLspServer = await CreateAutoLoadLanguageServerAsync(workspaceContent);

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingFileMessage(testLspServer, "App.sln"), GetLoadedFileMessage(testLspServer, "App.sln"));
    }

    [Fact]
    public async Task LoadsSingleSlnxFileAtRoot()
    {
        var workspaceContent = CreateAutoLoadWorkspace()
            .WithFile("App/App.csproj", ProjectContent)
            .WithFile("App.slnx", CreateSolutionXFile("App/App.csproj"));

        await using var testLspServer = await CreateAutoLoadLanguageServerAsync(workspaceContent);

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingFileMessage(testLspServer, "App.slnx"), GetLoadedFileMessage(testLspServer, "App.slnx"));
    }

    [Fact]
    public async Task LoadsAllProjectsIfSolutionPresentButNotAtRoot()
    {
        var workspaceContent = CreateAutoLoadWorkspace()
            .WithFile("App/App.csproj", ProjectContent)
            .WithFile("Nested/Nested.csproj", ProjectContent)
            .WithFile("Solutions/App.sln", CreateSolutionFile("App/App.csproj"));

        await using var testLspServer = await CreateAutoLoadLanguageServerAsync(workspaceContent);

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingProjectsMessage(projectCount: 2), GetLoadedProjectsMessage(projectCount: 2));
    }

    [Fact]
    public async Task LoadsAllProjectsIfMultipleSolutionsPresentAtRoot()
    {
        var workspaceContent = CreateAutoLoadWorkspace()
            .WithFile("App/App.csproj", ProjectContent)
            .WithFile("Nested/Nested.csproj", ProjectContent)
            .WithFile("App.sln", CreateSolutionFile("App/App.csproj"))
            .WithFile("App.slnx", CreateSolutionXFile("Nested/Nested.csproj"));

        await using var testLspServer = await CreateAutoLoadLanguageServerAsync(workspaceContent);

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingProjectsMessage(projectCount: 2), GetLoadedProjectsMessage(projectCount: 2));
    }

    [Fact]
    public async Task UsesVSCodeDefaultSolutionSettingIfPresent()
    {
        var workspaceContent = CreateAutoLoadWorkspace()
            .WithFile("App/App.csproj", ProjectContent)
            .WithFile("Nested/Nested.csproj", ProjectContent)
            .WithFile("Solutions/App.slnx", CreateSolutionXFile("../App/App.csproj"))
            .WithFile(".vscode/settings.json", """
                {
                  "dotnet.defaultSolution": "Solutions/App.slnx"
                }
                """);

        await using var testLspServer = await CreateAutoLoadLanguageServerAsync(workspaceContent);

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingFileMessage(testLspServer, "Solutions/App.slnx"), GetLoadedFileMessage(testLspServer, "Solutions/App.slnx"));
    }

    [Fact]
    public async Task ReportsProgressForExplicitSolutionOpen()
    {
        var workspaceContent = LspWorkspaceContent.Empty
            .WithFile("App.csproj", ProjectContent)
            .WithFile("App.sln", CreateSolutionFile("App.csproj"))
            .WithLoadPath("App.sln")
            .WithRestore();

        await using var testLspServer = await CreateLanguageServerAsync(
            workspaceContent,
            new LspServerLaunchOptions(),
            CreateWorkDoneProgressClientCapabilities());

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingFileMessage(testLspServer, "App.sln"), GetLoadedFileMessage(testLspServer, "App.sln"));
    }

    [Fact]
    public async Task ReportsProgressForExplicitProjectOpen()
    {
        var workspaceContent = LspWorkspaceContent.Empty
            .WithFile("Project.csproj", ProjectContent)
            .WithLoadPath("Project.csproj")
            .WithRestore();

        await using var testLspServer = await CreateLanguageServerAsync(
            workspaceContent,
            new LspServerLaunchOptions(),
            CreateWorkDoneProgressClientCapabilities());

        await AssertAutoLoadCompletedAsync(testLspServer, GetLoadingProjectsMessage(projectCount: 1), GetLoadedProjectsMessage(projectCount: 1));
    }

    private Task<TestLspClient> CreateAutoLoadLanguageServerAsync(LspWorkspaceContent workspaceContent)
        => CreateLanguageServerAsync(
            workspaceContent,
            new LspServerLaunchOptions { AutoLoadProjects = true },
            CreateWorkDoneProgressClientCapabilities());

    private static VSInternalClientCapabilities CreateWorkDoneProgressClientCapabilities()
        => new()
        {
            Window = new WindowClientCapabilities
            {
                WorkDoneProgress = true,
            }
        };

    private static LspWorkspaceContent CreateAutoLoadWorkspace()
        => LspWorkspaceContent.Empty.WithRestore();

    private static async Task AssertAutoLoadCompletedAsync(TestLspClient testLspServer, string expectedStartMessage, string expectedEndMessage)
    {
        var unit = await testLspServer.WorkDoneProgress.WaitForWorkDoneProgressCreation(expectedStartMessage).WaitAsync(TimeSpan.FromMinutes(2));
        Assert.NotNull(unit.CreateParams.Token.Value);

        var end = await unit.WaitForEndAsync().WaitAsync(TimeSpan.FromMinutes(2));
        Assert.Equal(expectedEndMessage, end.Message);
    }

    private static string GetLoadingProjectsMessage(int projectCount)
        => string.Format(LanguageServerResources.Loading_0_projects, projectCount);

    private static string GetLoadedProjectsMessage(int projectCount)
        => string.Format(LanguageServerResources.Loaded_0_projects, projectCount);

    private static string GetLoadingFileMessage(TestLspClient testLspServer, string relativePath)
        => string.Format(LanguageServerResources.Loading_0, GetFullPath(testLspServer, relativePath));

    private static string GetLoadedFileMessage(TestLspClient testLspServer, string relativePath)
        => string.Format(LanguageServerResources.Loaded_0, GetFullPath(testLspServer, relativePath));

    private static string GetFullPath(TestLspClient testLspServer, string relativePath)
        => PathUtilities.CombinePathsUnchecked(testLspServer.WorkspaceRootPath, LspWorkspaceContent.NormalizePath(relativePath).Replace(PathUtilities.AltDirectorySeparatorChar, PathUtilities.DirectorySeparatorChar));

    private static string CreateSolutionXFile(params string[] projectPaths)
        => """
            <Solution>
            """ + Environment.NewLine + string.Join(Environment.NewLine, projectPaths.Select(path => $"  <Project Path=\"{path}\" />")) + Environment.NewLine + "</Solution>";

    private static string CreateSolutionFile(params string[] projectPaths)
    {
        var projectEntries = string.Join(Environment.NewLine, projectPaths.Select((path, index) => $$"""
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "{{GetProjectName(path)}}", "{{path.Replace('/', '\\')}}", "{{GetProjectGuid(index)}}"
            EndProject
            """));

        var projectConfigurations = string.Join(Environment.NewLine, projectPaths.SelectMany((_, index) => new[]
        {
            $"\t\t{GetProjectGuid(index)}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
            $"\t\t{GetProjectGuid(index)}.Debug|Any CPU.Build.0 = Debug|Any CPU",
        }));

        return $$"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.5.002.0
            MinimumVisualStudioVersion = 10.0.40219.1
            {{projectEntries}}
            Global
            	GlobalSection(SolutionConfigurationPlatforms) = preSolution
            		Debug|Any CPU = Debug|Any CPU
            	EndGlobalSection
            	GlobalSection(ProjectConfigurationPlatforms) = postSolution
            {{projectConfigurations}}
            	EndGlobalSection
            	GlobalSection(SolutionProperties) = preSolution
            		HideSolutionNode = FALSE
            	EndGlobalSection
            EndGlobal
            """;
    }

    private static string GetProjectName(string projectPath)
        => Path.GetFileNameWithoutExtension(projectPath.Replace('/', Path.DirectorySeparatorChar));

    private static string GetProjectGuid(int index)
        => index switch
        {
            0 => "{11111111-1111-1111-1111-111111111111}",
            1 => "{22222222-2222-2222-2222-222222222222}",
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
}
