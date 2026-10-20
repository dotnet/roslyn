// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.Extensions.Logging;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using MetadataReferenceItem = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.MetadataReferenceItem;
using ProjectFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.ProjectFileInfo;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LoadedProjectTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
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

        var project = Assert.Single(await loadedProject.GetExistingProjectsAsync());
        var reference = Assert.Single(project.MetadataReferences);
        Assert.Equal(metadataPath, reference.Display);
        AssertEx.Equal(["global", "Interop"], reference.Properties.Aliases);
        Assert.True(reference.Properties.EmbedInteropTypes);

        Assert.Equal(analyzerPath, Assert.Single(project.AnalyzerReferences).FullPath);
    }
}
