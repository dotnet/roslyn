// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class NetCoreTests : MSBuildWorkspaceTestBase
    {
        [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreApp2()
        {
            CreateFiles(GetNetCoreApp2Files());

            var projectFilePath = GetSolutionFileName("Project.csproj");

            DotNetHelper.Restore("Project.csproj", workingDirectory: this.SolutionDirectory.Path);

            using (var workspace = CreateMSBuildWorkspace())
            {
                var project = await workspace.OpenProjectAsync(projectFilePath);

                // Assert that there is a single project loaded.
                Assert.Single(workspace.CurrentSolution.ProjectIds);

                // Assert that the project does not have any diagnostics in Program.cs
                var document = project.Documents.First(d => d.Name == "Program.cs");
                var semanticModel = await document.GetSemanticModelAsync();
                var diagnostics = semanticModel.GetDiagnostics();
                Assert.Empty(diagnostics);
            }
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreMultiTFM()
        {
            CreateFiles(GetNetCoreMultiTFMFiles());

            var projectFilePath = GetSolutionFileName("Project.csproj");

            DotNetHelper.Restore("Project.csproj", workingDirectory: this.SolutionDirectory.Path);

            using (var workspace = CreateMSBuildWorkspace())
            {
                await workspace.OpenProjectAsync(projectFilePath);

                // Assert that three projects have been loaded, one for each TFM.
                Assert.Equal(3, workspace.CurrentSolution.ProjectIds.Count);

                var projectPaths = new HashSet<string>();
                var outputFilePaths = new HashSet<string>();

                foreach (var project in workspace.CurrentSolution.Projects)
                {
                    projectPaths.Add(project.FilePath);
                    outputFilePaths.Add(project.OutputFilePath);
                }

                // Assert that the three projects share the same file path
                Assert.Single(projectPaths);

                // Assert that the three projects have different output file paths
                Assert.Equal(3, outputFilePaths.Count);

                // Assert that none of the projects have any diagnostics in Program.cs
                foreach (var project in workspace.CurrentSolution.Projects)
                {
                    var document = project.Documents.First(d => d.Name == "Program.cs");
                    var semanticModel = await document.GetSemanticModelAsync();
                    var diagnostics = semanticModel.GetDiagnostics();
                    Assert.Empty(diagnostics);
                }
            }
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreMultiTFM_ProjectReference()
        {
            CreateFiles(GetNetCoreMultiTFMFiles_ProjectReference());

            // Restoring for Project.csproj should also restore Library.csproj
            DotNetHelper.Restore(@"Project\Project.csproj", workingDirectory: this.SolutionDirectory.Path);

            var projectFilePath = GetSolutionFileName(@"Project\Project.csproj");

            using (var workspace = CreateMSBuildWorkspace())
            {
                await workspace.OpenProjectAsync(projectFilePath);

                // Assert that four projects have been loaded, one for each TFM.
                Assert.Equal(4, workspace.CurrentSolution.ProjectIds.Count);

                var projectPaths = new HashSet<string>();
                var outputFilePaths = new HashSet<string>();

                foreach (var project in workspace.CurrentSolution.Projects)
                {
                    projectPaths.Add(project.FilePath);
                    outputFilePaths.Add(project.OutputFilePath);
                }

                // Assert that there are two project file path among the four projects
                Assert.Equal(2, projectPaths.Count);

                // Assert that the four projects each have different output file paths
                Assert.Equal(4, outputFilePaths.Count);

                var expectedNames = new HashSet<string>()
                {
                    "Library(netstandard2.0)",
                    "Library(net461)",
                    "Project(netcoreapp2.0)",
                    "Project(net461)"
                };

                var actualNames = new HashSet<string>();

                foreach (var project in workspace.CurrentSolution.Projects)
                {
                    actualNames.Add(project.Name);
                    var fileName = PathUtilities.GetFileName(project.FilePath);

                    Document document;

                    switch (fileName)
                    {
                        case "Project.csproj":
                            document = project.Documents.First(d => d.Name == "Program.cs");
                            break;

                        case "Library.csproj":
                            document = project.Documents.First(d => d.Name == "Class1.cs");
                            break;

                        default:
                            Assert.True(false, $"Encountered unexpected project: {project.FilePath}");
                            return;
                    }

                    // Assert that none of the projects have any diagnostics in their primary .cs file.
                    var semanticModel = await document.GetSemanticModelAsync();
                    var diagnostics = semanticModel.GetDiagnostics();
                    Assert.Empty(diagnostics);
                }

                Assert.True(actualNames.SetEquals(expectedNames), $"Project names differ!{Environment.NewLine}Expected: {actualNames}{Environment.NewLine}Expected: {expectedNames}");
            }
        }
    }
}
