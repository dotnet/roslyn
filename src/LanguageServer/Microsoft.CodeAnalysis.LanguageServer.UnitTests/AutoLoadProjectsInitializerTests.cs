// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class AutoLoadProjectsInitializerTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public void TryGetVSCodeSolutionSettings_SingleFolderDisableSuppressesAutoLoad()
    {
        var workspaceFolder = CreateWorkspaceFolder("single", """
            {
              "dotnet.defaultSolution": "disable"
            }
            """);

        var (isLoadingDisabled, solutionPath) = AutoLoadProjectsInitializer.TryGetVSCodeSolutionSettings([workspaceFolder], NullLogger.Instance);

        Assert.True(isLoadingDisabled);
        Assert.Null(solutionPath);
    }

    [Fact]
    public void TryGetVSCodeSolutionSettings_MultiFolderUsesFirstConfiguredSolution()
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
        CreateSolutionFile(secondFolder, "eng/App.slnx");

        var (isLoadingDisabled, solutionPath) = AutoLoadProjectsInitializer.TryGetVSCodeSolutionSettings([firstFolder, secondFolder], NullLogger.Instance);

        Assert.False(isLoadingDisabled);
        Assert.Equal(Path.Combine(ProtocolConversions.GetDocumentFilePathFromUri(secondFolder.DocumentUri.GetRequiredParsedUri()), "eng", "App.slnx"), solutionPath);
    }

    private string WriteSettingsFile(string settingsJson)
    {
        var folder = _tempRoot.CreateDirectory();
        var settingsDirectory = folder.CreateDirectory(".vscode");
        var settingsPath = Path.Combine(settingsDirectory.Path, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);
        return settingsPath;
    }

    private WorkspaceFolder CreateWorkspaceFolder(string name, string settingsJson)
    {
        var settingsPath = WriteSettingsFile(settingsJson);
        var folder = Path.GetDirectoryName(Path.GetDirectoryName(settingsPath))!;

        return new WorkspaceFolder
        {
            Name = name,
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(folder),
        };
    }

    private static void CreateSolutionFile(WorkspaceFolder folder, string relativePath)
    {
        var folderPath = ProtocolConversions.GetDocumentFilePathFromUri(folder.DocumentUri.GetRequiredParsedUri());
        var solutionPath = Path.Combine(folderPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(solutionPath)!);
        File.WriteAllText(solutionPath, string.Empty);
    }
}
