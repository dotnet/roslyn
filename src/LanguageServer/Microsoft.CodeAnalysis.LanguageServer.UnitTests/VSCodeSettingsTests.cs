// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class VSCodeSettingsTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public void Parse_ReadsDotnetDefaultSolution()
    {
        var settings = VSCodeSettings.Parse("""
            {
              "dotnet.defaultSolution": "src/App.sln"
            }
            """);

        Assert.Equal("src/App.sln", settings.DefaultSolution);
    }

    [Fact]
    public void Parse_AllowsJsonCommentsAndTrailingCommas()
    {
        var settings = VSCodeSettings.Parse("""
            {
              // comment
              "dotnet.defaultSolution": "src/App.sln",
            }
            """);

        Assert.Equal("src/App.sln", settings.DefaultSolution);
    }

    [Fact]
    public void Parse_DisableSuppressesDefaultSolution()
    {
        var settings = VSCodeSettings.Parse("""
            {
              "dotnet.defaultSolution": "disable"
            }
            """);

        Assert.Null(settings.DefaultSolution);
    }

    [Fact]
    public void ResolveDefaultSolutionPath_ResolvesRelativePathAgainstWorkspaceFolder()
    {
        var workspaceFolder = _tempRoot.CreateDirectory();
        var settings = VSCodeSettings.Parse("""
            {
              "dotnet.defaultSolution": "src/App.sln"
            }
            """);

        var resolvedPath = settings.ResolveDefaultSolutionPath(workspaceFolder.Path);

        Assert.Equal(Path.Combine(workspaceFolder.Path, "src", "App.sln"), resolvedPath);
    }

    [Fact]
    public void TryGetSolutionToLoadFromVSCodeSettings_SingleFolderDisableSuppressesAutoLoad()
    {
        var workspaceFolder = CreateWorkspaceFolder("single", """
            {
              "dotnet.defaultSolution": "disable"
            }
            """);

        var solutionPath = AutoLoadProjectsInitializer.TryGetSolutionToLoadFromVSCodeSettings([workspaceFolder], NullLogger.Instance);

        Assert.Null(solutionPath);
    }

    [Fact]
    public void TryGetSolutionToLoadFromVSCodeSettings_MultiFolderUsesFirstConfiguredSolution()
    {
        var firstFolder = CreateWorkspaceFolder("first", """
            {
              "dotnet.defaultSolution": "disable"
            }
            """);
        var secondFolder = CreateWorkspaceFolder("second", """
            {
              "dotnet.defaultSolution": "eng/App.slnx"
            }
            """);

        var solutionPath = AutoLoadProjectsInitializer.TryGetSolutionToLoadFromVSCodeSettings([firstFolder, secondFolder], NullLogger.Instance);

        Assert.Equal(Path.Combine(ProtocolConversions.GetDocumentFilePathFromUri(secondFolder.DocumentUri.GetRequiredParsedUri()), "eng", "App.slnx"), solutionPath);
    }

    private WorkspaceFolder CreateWorkspaceFolder(string name, string settingsJson)
    {
        var folder = _tempRoot.CreateDirectory();
        var settingsDirectory = folder.CreateDirectory(".vscode");
        File.WriteAllText(Path.Combine(settingsDirectory.Path, "settings.json"), settingsJson);

        return new WorkspaceFolder
        {
            Name = name,
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder.Path),
        };
    }
}
