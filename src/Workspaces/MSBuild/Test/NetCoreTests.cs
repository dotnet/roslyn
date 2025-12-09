// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

public sealed class NetCoreTests : MSBuildWorkspaceTestBase
{
    private readonly TempDirectory _nugetCacheDir;

    public NetCoreTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nugetCacheDir = SolutionDirectory.CreateDirectory(".packages");
    }

    private void RunDotNet(string arguments)
    {
        var environmentVariables = new Dictionary<string, string>()
        {
            ["NUGET_PACKAGES"] = _nugetCacheDir.Path
        };

        var dotNetExeName = "dotnet" + (Path.DirectorySeparatorChar == '/' ? "" : ".exe");

        var restoreResult = ProcessUtilities.Run(
            dotNetExeName, arguments,
            workingDirectory: SolutionDirectory.Path,
            additionalEnvironmentVars: environmentVariables);

        Assert.True(restoreResult.ExitCode == 0, $"{dotNetExeName} failed with exit code {restoreResult.ExitCode}: {restoreResult.Output}");
    }

    private void DotNetRestore(string solutionOrProjectFileName)
    {
        RunDotNet($@"msbuild ""{solutionOrProjectFileName}"" /t:restore /bl:{Path.Combine(SolutionDirectory.Path, "restore.binlog")}");
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

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProject_NetCoreApp()
    {
        CreateFiles(GetNetCoreAppFiles());

        var projectFilePath = GetSolutionFileName("Project.csproj");
        var projectDir = Path.GetDirectoryName(projectFilePath);

        DotNetRestore("Project.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Equal(Path.Combine(projectDir, "bin", "Debug", "netcoreapp3.1", "Project.dll"), project.OutputFilePath);
        Assert.Equal(Path.Combine(projectDir, "obj", "Debug", "netcoreapp3.1", "Project.dll"), project.CompilationOutputInfo.AssemblyPath);
        Assert.Null(project.CompilationOutputInfo.GeneratedFilesOutputDirectory);

        // Assert that there is a single project loaded.
        Assert.Single(workspace.CurrentSolution.ProjectIds);

        // Assert that the project does not have any diagnostics in Program.cs
        var document = project.Documents.First(d => d.Name == "Program.cs");
        var semanticModel = await document.GetSemanticModelAsync();
        var diagnostics = semanticModel.GetDiagnostics();
        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProject_BinaryLogger()
    {
        CreateFiles(GetNetCoreAppFiles());

        var projectFilePath = GetSolutionFileName("Project.csproj");
        var projectDir = Path.GetDirectoryName(projectFilePath);
        var binLogPath = Path.Combine(projectDir, "build.binlog");

        DotNetRestore("Project.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath, new BinaryLogger() { Parameters = binLogPath });

        // The binarylog could have been given a suffix to avoid filename collisions when used by multiple buildhosts.
        var buildLogPaths = Directory.EnumerateFiles(projectDir, "build*.binlog").ToImmutableArray();
        var buildLogPath = Assert.Single(buildLogPaths);
        var buildLogInfo = new FileInfo(buildLogPath);
        Assert.True(buildLogInfo.Length > 0);
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenInMemoryProject_NetCoreApp()
    {
        CreateFiles(GetNetCoreAppFiles());

        var projectFilePath = GetSolutionFileName("Project.csproj");
        var content = File.ReadAllText(projectFilePath);
        File.Delete(projectFilePath);
        var projectDir = Path.GetDirectoryName(projectFilePath);

        await using var buildHostProcessManager = new BuildHostProcessManager(ImmutableDictionary<string, string>.Empty);

        var buildHost = await buildHostProcessManager.GetBuildHostAsync(BuildHostProcessManager.BuildHostProcessKind.NetCore, CancellationToken.None);
        var projectFile = await buildHost.LoadProjectAsync(projectFilePath, content, LanguageNames.CSharp, CancellationToken.None);
        var projectFileInfo = (await projectFile.GetProjectFileInfosAsync(CancellationToken.None)).Single();

        Assert.Equal(Path.Combine(projectDir, "bin", "Debug", "netcoreapp3.1", "Project.dll"), projectFileInfo.OutputFilePath);
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProjectTwice_NetCoreAppAndLibrary()
    {
        CreateFiles(GetNetCoreAppAndLibraryFiles());

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

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProjectTwice_NetCoreAppAndTwoLibraries()
    {
        CreateFiles(GetNetCoreAppAndTwoLibrariesFiles());

        var projectFilePath = GetSolutionFileName(@"Project\Project.csproj");
        var library1FilePath = GetSolutionFileName(@"Library1\Library1.csproj");
        var library2FilePath = GetSolutionFileName(@"Library2\Library2.csproj");

        DotNetRestore(@"Project\Project.csproj");
        DotNetRestore(@"Library2\Library2.csproj");

        // Warning: Found project reference without a matching metadata reference: Library1.csproj
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
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

        static void AssertSingleProjectReference(Project project, string projectRefFilePath)
        {
            var projectReference = Assert.Single(project.ProjectReferences);

            var projectRefId = projectReference.ProjectId;
            Assert.Equal(projectRefFilePath, project.Solution.GetProject(projectRefId).FilePath);
        }
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProject_NetCoreMultiTFM()
    {
        CreateFiles(GetNetCoreMultiTFMFiles());

        var projectFilePath = GetSolutionFileName("Project.csproj");

        DotNetRestore("Project.csproj");

        using var workspace = CreateMSBuildWorkspace();
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

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProject_NetCoreMultiTFM_ExtensionWithConditionOnTFM()
    {
        CreateFiles(GetNetCoreMultiTFMFiles_ExtensionWithConditionOnTFM());

        var projectFilePath = GetSolutionFileName("Project.csproj");

        DotNetRestore("Project.csproj");

        using var workspace = CreateMSBuildWorkspace();
        await workspace.OpenProjectAsync(projectFilePath);

        // Assert that three projects have been loaded, one for each TFM.
        Assert.Equal(3, workspace.CurrentSolution.ProjectIds.Count);

        // Assert the TFM is accessible from project extensions.
        // The test project extension sets the default namespace based on the TFM.
        foreach (var project in workspace.CurrentSolution.Projects)
        {
            switch (project.Name)
            {
                case "Project(net6)":
                    Assert.Equal("Project.NetCore", project.DefaultNamespace);
                    break;

                case "Project(netstandard2.0)":
                    Assert.Equal("Project.NetStandard", project.DefaultNamespace);
                    break;

                case "Project(net5)":
                    Assert.Equal("Project.NetFramework", project.DefaultNamespace);
                    break;

                default:
                    Assert.True(false, $"Unexpected project: {project.Name}");
                    break;
            }
        }
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
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

    private async Task AssertNetCoreMultiTFMProject(string projectFilePath)
    {
        using var workspace = CreateMSBuildWorkspace();
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
                "Project(net6)",
                "Project(net5)",
                "Library(netstandard2",
                "Library(net5)"
            };

        var actualNames = new HashSet<string>();

        foreach (var project in workspace.CurrentSolution.Projects)
        {
            var dotIndex = project.Name.IndexOf('.');
            var projectName = dotIndex >= 0
                ? project.Name[..dotIndex]
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

            if (project.OutputFilePath.Contains("net6"))
            {
                Assert.Contains("net5", referencedProject.OutputFilePath);
            }
            else if (project.OutputFilePath.Contains("net5"))
            {
                Assert.Contains("net5", referencedProject.OutputFilePath);
            }
            else
            {
                Assert.True(false, "OutputFilePath with expected TFM not found.");
            }
        }
    }

    [ConditionalTheory(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    [CombinatorialData]
    public async Task TestOpenSolution_NetCoreMultiTFMWithProjectReferenceToFSharp(bool build)
    {
        CreateFiles(GetNetCoreMultiTFMFiles_ProjectReferenceToFSharp());

        var solutionFilePath = GetSolutionFileName("Solution.sln");

        DotNetRestore("Solution.sln");

        if (build)
        {
            DotNetBuild("Solution.sln", configuration: "Debug");
        }

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false, skipUnrecognizedProjects: true);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        var projects = solution.Projects.ToArray();

        Assert.Equal(2, projects.Length);

        foreach (var project in projects)
        {
            Assert.StartsWith("csharplib", project.Name);
            Assert.Empty(project.ProjectReferences);

            if (build)
            {
                Assert.Empty(project.AllProjectReferences);
                Assert.Contains(project.MetadataReferences, m => m is PortableExecutableReference pe && pe.FilePath.EndsWith("fsharplib.dll"));
            }
            else
            {
                Assert.Single(project.AllProjectReferences);
            }
        }
    }

    [ConditionalTheory(typeof(DotNetSdkMSBuildInstalled), Reason = "https://github.com/dotnet/roslyn/issues/81589")]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/81589")]
    [CombinatorialData]
    public async Task TestOpenSolution_NetCoreMultiTFMWithProjectReferenceToFSharp_MultiTFM(bool build)
    {
        CreateFiles(GetNetCoreMultiTFMFiles_ProjectReferenceToFSharp());

        var solutionFilePath = GetSolutionFileName("Solution.sln");
        var fsharpProjectFilePath = GetSolutionFileName(@"fsharplib\fsharplib.fsproj");

        File.WriteAllText(fsharpProjectFilePath, Resources.ProjectFiles.FSharp.NetCoreMultiTFM_ProjectReferenceToFSharp_FSharpLib
            .Replace("<TargetFramework>netstandard2.0</TargetFramework>", "<TargetFrameworks>netstandard2.0;netcoreapp2.0</TargetFrameworks>"));

        DotNetRestore("Solution.sln");

        if (build)
        {
            DotNetBuild("Solution.sln", configuration: "Debug");
        }

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false, skipUnrecognizedProjects: true);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        var projects = solution.Projects.ToArray();

        Assert.Equal(2, projects.Length);

        foreach (var project in projects)
        {
            Assert.StartsWith("csharplib", project.Name);
            Assert.Empty(project.ProjectReferences);

            if (build)
            {
                Assert.Empty(project.AllProjectReferences);
                Assert.Contains(project.MetadataReferences, m => m is PortableExecutableReference pe && pe.FilePath.EndsWith("fsharplib.dll"));
            }
            else
            {
                Assert.Single(project.AllProjectReferences);
            }
        }
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProject_ReferenceConfigurationSpecificMetadata()
    {
        var files = new FileSet(
            (@"Solution.sln", Resources.SolutionFiles.Issue30174_Solution),
            (@"InspectedLibrary\InspectedLibrary.csproj", Resources.ProjectFiles.CSharp.Issue30174_InspectedLibrary),
            (@"InspectedLibrary\InspectedClass.cs", Resources.SourceFiles.CSharp.Issue30174_InspectedClass),
            (@"ReferencedLibrary\ReferencedLibrary.csproj", Resources.ProjectFiles.CSharp.Issue30174_ReferencedLibrary),
            (@"ReferencedLibrary\SomeMetadataAttribute.cs", Resources.SourceFiles.CSharp.Issue30174_SomeMetadataAttribute));

        CreateFiles(files);

        DotNetRestore("Solution.sln");
        DotNetBuild("Solution.sln", configuration: "Release");

        var projectFilePath = GetSolutionFileName(@"InspectedLibrary\InspectedLibrary.csproj");

        using var workspace = CreateMSBuildWorkspace(("Configuration", "Release"));
        workspace.LoadMetadataForReferencedProjects = true;

        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Empty(project.ProjectReferences);
        Assert.Empty(workspace.Diagnostics);

        var compilation = await project.GetCompilationAsync();
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProject_OverrideTFM()
    {
        CreateFiles(GetNetCoreAppAndLibraryFiles());

        var projectFilePath = GetSolutionFileName(@"Library\Library.csproj");

        DotNetRestore(@"Library\Library.csproj");

        // Override the TFM properties defined in the file
        using var workspace = CreateMSBuildWorkspace(("TargetFramework", ""), ("TargetFrameworks", "net6;net5"));
        await workspace.OpenProjectAsync(projectFilePath);

        // Assert that two projects have been loaded, one for each TFM.
        Assert.Equal(2, workspace.CurrentSolution.ProjectIds.Count);

        Assert.Contains(workspace.CurrentSolution.Projects, p => p.Name == "Library(net6)");
        Assert.Contains(workspace.CurrentSolution.Projects, p => p.Name == "Library(net5)");
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    [UseCulture("en-EN", "en-EN")]
    public Task TestBuildHostLocale_EN()
        => AssertInvalidTfmDiagnosticMessageContains("The TargetFramework value 'Invalid' was not recognized. It may be misspelled.");

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    [UseCulture("de-DE", "de-DE")]
    public Task TestBuildHostLocale_DE()
        => AssertInvalidTfmDiagnosticMessageContains("Der TargetFramework-Wert \"Invalid\" wurde nicht erkannt. Unter Umständen ist die Schreibweise nicht korrekt.");

    private async Task AssertInvalidTfmDiagnosticMessageContains(string expected)
    {
        var projectPath = @"Project\Project.csproj";
        var files = new FileSet((projectPath, Resources.ProjectFiles.CSharp.InvalidTFM));

        CreateFiles(files);

        var fullProjectPath = GetSolutionFileName(projectPath);

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(fullProjectPath);

        var diagnostic = Assert.Single(workspace.Diagnostics);
        Assert.Contains(expected, diagnostic.Message);
    }

    [ConditionalFact(typeof(DotNetSdkMSBuildInstalled))]
    [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
    [Trait(Traits.Feature, Traits.Features.NetCore)]
    public async Task TestOpenProject_VBNetCoreAppWithGlobalImportAndLibrary()
    {
        CreateFiles(GetVBNetCoreAppWithGlobalImportAndLibraryFiles());

        var vbProjectFilePath = GetSolutionFileName(@"VBProject\VBProject.vbproj");
        var libraryFilePath = GetSolutionFileName(@"Library\Library.csproj");

        DotNetRestore(@"Library\Library.csproj");
        DotNetRestore(@"VBProject\VBProject.vbproj");

        // Warning:Found project reference without a matching metadata reference: Library.csproj
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(vbProjectFilePath);

        // Assert that there is are two projects loaded (VBProject.vbproj references Library.csproj).
        Assert.Equal(2, workspace.CurrentSolution.ProjectIds.Count);

        // Assert that there is a project reference between VBProject.vbproj and Library.csproj
        AssertSingleProjectReference(project, libraryFilePath);

        var document = project.Documents.First(d => d.Name == "Program.vb");
        Assert.Empty(document.Folders);

        // Assert that the project does not have any diagnostics in Program.vb
        var semanticModel = await document.GetSemanticModelAsync();
        var diagnostics = semanticModel.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));

        var compilation = await project.GetCompilationAsync();
        var option = compilation.Options as VisualBasicCompilationOptions;
        Assert.Contains("LibraryHelperClass = Library.MyHelperClass", option.GlobalImports.Select(i => i.Name));

        static void AssertSingleProjectReference(Project project, string projectRefFilePath)
        {
            var projectReference = Assert.Single(project.ProjectReferences);

            var projectRefId = projectReference.ProjectId;
            Assert.Equal(projectRefFilePath, project.Solution.GetProject(projectRefId).FilePath);
        }
    }

    [Fact]
    public void BuildHostShipsDepsJsonFile()
    {
        var depsJsonFile = Path.ChangeExtension(BuildHostProcessManager.GetNetCoreBuildHostPath(), "deps.json");
        Assert.True(File.Exists(depsJsonFile), $"{depsJsonFile} should exist, or it won't load on some runtimes.");
    }
}
