// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class NetCoreTests : MSBuildWorkspaceTestBase
    {
        private readonly TempDirectory _nugetCacheDir;

        public NetCoreTests()
        {
            _nugetCacheDir = SolutionDirectory.CreateDirectory(".packages");
        }

        private void RunDotNet(string arguments)
        {
            Assert.NotNull(DotNetCoreSdk.ExePath);

            var environmentVariables = new Dictionary<string, string>()
            {
                ["NUGET_PACKAGES"] = _nugetCacheDir.Path
            };

            var restoreResult = ProcessUtilities.Run(
                DotNetCoreSdk.ExePath, arguments,
                workingDirectory: SolutionDirectory.Path,
                additionalEnvironmentVars: environmentVariables);

            Assert.True(restoreResult.ExitCode == 0, $"{DotNetCoreSdk.ExePath} failed with exit code {restoreResult.ExitCode}: {restoreResult.Output}");
        }

        private void DotNetRestore(string solutionOrProjectFileName)
        {
            var arguments = $@"msbuild ""{solutionOrProjectFileName}"" /t:restore /bl:{Path.Combine(SolutionDirectory.Path, "restore.binlog")}";
            RunDotNet(arguments);
        }

        private void DotNetBuild(string solutionOrProjectFileName, string configuration = null)
        {
            var arguments = $@"msbuild ""{solutionOrProjectFileName}"" /bl:{Path.Combine(SolutionDirectory.Path, "build.binlog")}";

            if (configuration != null)
            {
                arguments += $" /p:Configuration={configuration}";
            }

            RunDotNet(arguments);
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreApp2()
        {
            CreateFiles(GetNetCoreApp2Files());

            var projectFilePath = GetSolutionFileName("Project.csproj");

            DotNetRestore("Project.csproj");

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

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProjectTwice_NetCoreApp2AndLibrary()
        {
            CreateFiles(GetNetCoreApp2AndLibraryFiles());

            var projectFilePath = GetSolutionFileName(@"Project\Project.csproj");
            var libraryFilePath = GetSolutionFileName(@"Library\Library.csproj");

            DotNetRestore(@"Project\Project.csproj");

            using var workspace = CreateMSBuildWorkspace();
            var libraryProject = await workspace.OpenProjectAsync(libraryFilePath);

            // Assert that there is a single project loaded.
            Assert.Single(workspace.CurrentSolution.ProjectIds);

            // Assert that the project does not have any diagnostics in Class1.cs
            var document = libraryProject.Documents.First(d => d.Name == "Class1.cs");
            var semanticModel = await document.GetSemanticModelAsync();
            var diagnostics = semanticModel.GetDiagnostics();
            Assert.Empty(diagnostics);

            var project = await workspace.OpenProjectAsync(projectFilePath);

            // Assert that there are only two projects opened.
            Assert.Equal(2, workspace.CurrentSolution.ProjectIds.Count);

            // Assert that there is a project reference between Project.csproj and Library.csproj
            var projectReference = Assert.Single(project.ProjectReferences);

            var projectRefId = projectReference.ProjectId;
            Assert.Equal(libraryProject.Id, projectRefId);
            Assert.Equal(libraryProject.FilePath, workspace.CurrentSolution.GetProject(projectRefId).FilePath);
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProjectTwice_NetCoreApp2AndTwoLibraries()
        {
            CreateFiles(GetNetCoreApp2AndTwoLibrariesFiles());

            var projectFilePath = GetSolutionFileName(@"Project\Project.csproj");
            var library1FilePath = GetSolutionFileName(@"Library1\Library1.csproj");
            var library2FilePath = GetSolutionFileName(@"Library2\Library2.csproj");

            DotNetRestore(@"Project\Project.csproj");
            DotNetRestore(@"Library2\Library2.csproj");

            using (var workspace = CreateMSBuildWorkspace())
            {
                var project = await workspace.OpenProjectAsync(projectFilePath);

                // Assert that there is are two projects loaded (Project.csproj references Library1.csproj).
                Assert.Equal(2, workspace.CurrentSolution.ProjectIds.Count);

                // Assert that the project does not have any diagnostics in Program.cs
                var document = project.Documents.First(d => d.Name == "Program.cs");
                var semanticModel = await document.GetSemanticModelAsync();
                var diagnostics = semanticModel.GetDiagnostics();
                Assert.Empty(diagnostics);

                var library2 = await workspace.OpenProjectAsync(library2FilePath);

                // Assert that there are now three projects loaded (Library2.csproj also references Library1.csproj)
                Assert.Equal(3, workspace.CurrentSolution.ProjectIds.Count);

                // Assert that there is a project reference between Project.csproj and Library1.csproj
                AssertSingleProjectReference(project, library1FilePath);

                // Assert that there is a project reference between Library2.csproj and Library1.csproj
                AssertSingleProjectReference(library2, library1FilePath);
            }

            static void AssertSingleProjectReference(Project project, string projectRefFilePath)
            {
                var projectReference = Assert.Single(project.ProjectReferences);

                var projectRefId = projectReference.ProjectId;
                Assert.Equal(projectRefFilePath, project.Solution.GetProject(projectRefId).FilePath);
            }
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreMultiTFM()
        {
            CreateFiles(GetNetCoreMultiTFMFiles());

            var projectFilePath = GetSolutionFileName("Project.csproj");

            DotNetRestore("Project.csproj");

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

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreMultiTFM_ProjectReference()
        {
            CreateFiles(GetNetCoreMultiTFMFiles_ProjectReference());

            // Restoring for Project.csproj should also restore Library.csproj
            DotNetRestore(@"Project\Project.csproj");

            var projectFilePath = GetSolutionFileName(@"Project\Project.csproj");

            await AssertNetCoreMultiTFMProject(projectFilePath);
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_NetCoreMultiTFM_ProjectReferenceWithReversedTFMs()
        {
            CreateFiles(GetNetCoreMultiTFMFiles_ProjectReferenceWithReversedTFMs());

            // Restoring for Project.csproj should also restore Library.csproj
            DotNetRestore(@"Project\Project.csproj");

            var projectFilePath = GetSolutionFileName(@"Project\Project.csproj");

            await AssertNetCoreMultiTFMProject(projectFilePath);
        }

        private async Task AssertNetCoreMultiTFMProject(string projectFilePath)
        {
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
                    "Library(netstandard2",
                    "Library(net461)",
                    "Project(netcoreapp2",
                    "Project(net461)"
                };

                var actualNames = new HashSet<string>();

                foreach (var project in workspace.CurrentSolution.Projects)
                {
                    var dotIndex = project.Name.IndexOf('.');
                    var projectName = dotIndex >= 0
                        ? project.Name.Substring(0, dotIndex)
                        : project.Name;

                    actualNames.Add(projectName);
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

                Assert.True(actualNames.SetEquals(expectedNames), $"Project names differ!{Environment.NewLine}Actual: {{{actualNames.Join(",")}}}{Environment.NewLine}Expected: {{{expectedNames.Join(",")}}}");

                // Verify that the projects reference the correct TFMs
                var projects = workspace.CurrentSolution.Projects.Where(p => p.FilePath.EndsWith("Project.csproj"));
                foreach (var project in projects)
                {
                    var projectReference = Assert.Single(project.ProjectReferences);

                    var referencedProject = workspace.CurrentSolution.GetProject(projectReference.ProjectId);

                    if (project.OutputFilePath.Contains("netcoreapp2"))
                    {
                        Assert.Contains("netstandard2", referencedProject.OutputFilePath);
                    }
                    else if (project.OutputFilePath.Contains("net461"))
                    {
                        Assert.Contains("net461", referencedProject.OutputFilePath);
                    }
                    else
                    {
                        Assert.True(false, "OutputFilePath with expected TFM not found.");
                    }
                }
            }
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenSolution_NetCoreMultiTFMWithProjectReferenceToFSharp()
        {
            CreateFiles(GetNetCoreMultiTFMFiles_ProjectReferenceToFSharp());

            var solutionFilePath = GetSolutionFileName("Solution.sln");

            DotNetRestore("Solution.sln");

            using (var workspace = CreateMSBuildWorkspace())
            {
                var solution = await workspace.OpenSolutionAsync(solutionFilePath);

                var projects = solution.Projects.ToArray();

                Assert.Equal(2, projects.Length);

                foreach (var project in projects)
                {
                    Assert.StartsWith("csharplib", project.Name);
                    Assert.Empty(project.ProjectReferences);
                    Assert.Single(project.AllProjectReferences);
                }
            }
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DotNetCoreSdk.IsAvailable))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public async Task TestOpenProject_ReferenceConfigurationSpecificMetadata()
        {
            var files = GetBaseFiles()
                .WithFile(@"Solution.sln", Resources.SolutionFiles.Issue30174_Solution)
                .WithFile(@"InspectedLibrary\InspectedLibrary.csproj", Resources.ProjectFiles.CSharp.Issue30174_InspectedLibrary)
                .WithFile(@"InspectedLibrary\InspectedClass.cs", Resources.SourceFiles.CSharp.Issue30174_InspectedClass)
                .WithFile(@"ReferencedLibrary\ReferencedLibrary.csproj", Resources.ProjectFiles.CSharp.Issue30174_ReferencedLibrary)
                .WithFile(@"ReferencedLibrary\SomeMetadataAttribute.cs", Resources.SourceFiles.CSharp.Issue30174_SomeMetadataAttribute);

            CreateFiles(files);

            DotNetRestore("Solution.sln");
            DotNetBuild("Solution.sln", configuration: "Release");

            var projectFilePath = GetSolutionFileName(@"InspectedLibrary\InspectedLibrary.csproj");

            using (var workspace = CreateMSBuildWorkspace(("Configuration", "Release")))
            {
                workspace.LoadMetadataForReferencedProjects = true;

                var project = await workspace.OpenProjectAsync(projectFilePath);

                Assert.Empty(project.ProjectReferences);
                Assert.Empty(workspace.Diagnostics);

                var compilation = await project.GetCompilationAsync();
            }
        }
    }
}
