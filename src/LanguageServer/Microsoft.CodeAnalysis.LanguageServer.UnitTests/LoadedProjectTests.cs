// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.Extensions.Logging;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using DocumentFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.DocumentFileInfo;
using MetadataReferenceItem = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.MetadataReferenceItem;
using ProjectFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.ProjectFileInfo;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LoadedProjectTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Fact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/9885")]
    public async Task ProjectFileInfoUpdatesDocumentFolders()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var workspaceFactory = server.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        var directory = TempRoot.CreateDirectory();
        var projectPath = directory.CreateFile("Project.csproj").Path;
        var documentPath = Path.Combine(directory.Path, "File.cs");
        var additionalPath = Path.Combine(directory.Path, "Additional.txt");
        await using var loadedProject = new LoadedProject(projectPath, workspaceFactory.HostProjectFactory.FileChangeWatcher);

        // First apply project information without folders, then verify that updating only the folders is reflected in the documents.
        foreach (string[] folders in new string[][] { [], ["B", "C"] })
        {
            var projectInfo = ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
            {
                CommandLineArgs = ["/target:library"],
                Documents = [new DocumentFileInfo(documentPath, documentPath, isLinked: false, isGenerated: false, folders)],
                AdditionalDocuments = [new DocumentFileInfo(additionalPath, additionalPath, isLinked: false, isGenerated: false, folders)],
            };

            await ApplyProjectInfoAsync(server, loadedProject, projectInfo);

            var project = Assert.Single(await loadedProject.GetExistingProjectsAsync());
            var document = Assert.Single(project.Documents);
            Assert.Equal(documentPath, document.FilePath);
            AssertEx.Equal(folders, document.Folders);

            var additionalDocument = Assert.Single(project.AdditionalDocuments);
            Assert.Equal(additionalPath, additionalDocument.FilePath);
            AssertEx.Equal(folders, additionalDocument.Folders);
        }
    }

    [Fact]
    public async Task ProjectFileInfoPreservesDocumentsWithDuplicatePaths()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var workspaceFactory = server.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        var directory = TempRoot.CreateDirectory();
        var projectPath = directory.CreateFile("Project.csproj").Path;
        var documentPath = Path.Combine(directory.Path, "File.cs");
        var additionalPath = Path.Combine(directory.Path, "Additional.txt");
        var analyzerConfigPath = Path.Combine(directory.Path, ".editorconfig");
        var projectInfo = ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
        {
            CommandLineArgs = ["/target:library"],
            Documents = CreateDuplicateDocuments(documentPath),
            AdditionalDocuments = CreateDuplicateDocuments(additionalPath),
            AnalyzerConfigDocuments = CreateDuplicateDocuments(analyzerConfigPath),
        };
        await using var loadedProject = new LoadedProject(projectPath, workspaceFactory.HostProjectFactory.FileChangeWatcher);

        // Apply the same snapshot twice to verify duplicate paths are handled on both initial load and reload.
        for (var i = 0; i < 2; i++)
        {
            await ApplyProjectInfoAsync(server, loadedProject, projectInfo);

            var project = Assert.Single(await loadedProject.GetExistingProjectsAsync());
            var document = Assert.Single(project.Documents);
            Assert.Equal(documentPath, document.FilePath);
            Assert.Equal("A", Assert.Single(document.Folders));

            var additionalDocument = Assert.Single(project.AdditionalDocuments);
            Assert.Equal(additionalPath, additionalDocument.FilePath);
            Assert.Equal("A", Assert.Single(additionalDocument.Folders));

            Assert.Equal(analyzerConfigPath, Assert.Single(project.AnalyzerConfigDocuments).FilePath);
        }

        static DocumentFileInfo[] CreateDuplicateDocuments(string path)
            =>
            [
                new DocumentFileInfo(path, path, isLinked: false, isGenerated: false, folders: ["A"]),
                new DocumentFileInfo(path, path, isLinked: false, isGenerated: false, folders: ["B"]),
            ];
    }

    [Fact]
    public async Task ProjectFileInfoReferencesAreApplied()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var workspaceFactory = server.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        var directory = TempRoot.CreateDirectory();
        var projectPath = Path.Combine(directory.Path, "Project.csproj");
        var metadataPath = Path.Combine(directory.Path, "Reference.dll");
        var analyzerPath = Path.Combine(directory.Path, "Analyzer.dll");
        var projectInfo = ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
        {
            CommandLineArgs = ["/target:library"],
            MetadataReferences = [new MetadataReferenceItem(metadataPath, ["global", "Interop"], EmbedInteropTypes: true)],
            AnalyzerReferences = [analyzerPath],
        };
        await using var loadedProject = new LoadedProject(projectPath, workspaceFactory.HostProjectFactory.FileChangeWatcher);

        await ApplyProjectInfoAsync(server, loadedProject, projectInfo);

        var project = Assert.Single(await loadedProject.GetExistingProjectsAsync());
        var reference = Assert.Single(project.MetadataReferences);
        Assert.Equal(metadataPath, reference.Display);
        AssertEx.Equal(["global", "Interop"], reference.Properties.Aliases);
        Assert.True(reference.Properties.EmbedInteropTypes);

        Assert.Equal(analyzerPath, Assert.Single(project.AnalyzerReferences).FullPath);
    }

    private async Task ApplyProjectInfoAsync(TestLspServer server, LoadedProject loadedProject, ProjectFileInfo projectInfo)
    {
        var workspaceFactory = server.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        Assert.True(await loadedProject.TryApplyLoadedProjectInfosAsync(
            [projectInfo],
            isMiscellaneousFile: false,
            hasAllInformation: true,
            workspaceFactory.HostProjectFactory,
            server.GetRequiredLspService<ProjectTargetFrameworkManager>(),
            server.GetRequiredLspService<ProjectCapabilityManager>(),
            workspaceFactory,
            LoggerFactory.CreateLogger<LoadedProjectTests>(),
            CancellationToken.None));
    }
}
