// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.CodeAnalysis.CSharp.LanguageVersionFacts;
using static Microsoft.CodeAnalysis.MSBuild.UnitTests.SolutionGeneration;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

[Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
public sealed class VisualStudioMSBuildWorkspaceTests : MSBuildWorkspaceTestBase
{
    public VisualStudioMSBuildWorkspaceTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public void TestCreateMSBuildWorkspace()
    {
        using var workspace = CreateMSBuildWorkspace();
        Assert.NotNull(workspace);
        Assert.NotNull(workspace.Services);
        Assert.NotNull(workspace.Services.Workspace);
        Assert.Equal(workspace, workspace.Services.Workspace);
        Assert.NotNull(workspace.Services.HostServices);
        Assert.NotNull(workspace.Services.TextFactory);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_SingleProjectSolution()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        var document = project.Documents.First();
        Assert.Empty(document.Folders);
        var tree = await document.GetSyntaxTreeAsync();
        var type = tree.GetRoot().DescendantTokens().First(t => t.ToString() == "class").Parent;
        Assert.NotNull(type);
        Assert.StartsWith("public class CSharpClass", type.ToString(), StringComparison.Ordinal);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_MultiProjectSolution()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var vbProject = solution.Projects.First(p => p.Language == LanguageNames.VisualBasic);

        // verify the dependent project has the correct metadata references (and does not include the output for the project references)
        var references = vbProject.MetadataReferences.ToList();
        Assert.Equal(4, references.Count);
        var fileNames = new HashSet<string>(references.Select(r => Path.GetFileName(((PortableExecutableReference)r).FilePath)));
        Assert.Contains("System.Core.dll", fileNames);
        Assert.Contains("System.dll", fileNames);
        Assert.Contains("Microsoft.VisualBasic.dll", fileNames);
        Assert.Contains("mscorlib.dll", fileNames);

        // the compilation references should have the metadata reference to the csharp project skeleton assembly
        var compilation = await vbProject.GetCompilationAsync();
        var compReferences = compilation.References.ToList();
        Assert.Equal(5, compReferences.Count);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestDirectUseOfMSBuildProjectLoader()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var msbuildProjectLoader = new MSBuildProjectLoader(workspace);
        var solutionInfo = await msbuildProjectLoader.LoadSolutionInfoAsync(solutionFilePath);
        var projectInfo = Assert.Single(solutionInfo.Projects);

        Assert.Single(projectInfo.Documents, d => d.Name == "CSharpClass.cs");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task Test_SharedMetadataReferences()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var p0 = solution.Projects.ElementAt(0);
        var p1 = solution.Projects.ElementAt(1);

        Assert.NotSame(p0, p1);

        var p0mscorlib = GetMetadataReference(p0, "mscorlib");
        var p1mscorlib = GetMetadataReference(p1, "mscorlib");

        Assert.NotNull(p0mscorlib);
        Assert.NotNull(p1mscorlib);

        // metadata references to mscorlib in both projects are the same
        Assert.Same(p0mscorlib, p1mscorlib);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546171")]
    public async Task Test_SharedMetadataReferencesWithAliases()
    {
        var projPath1 = @"CSharpProject\CSharpProject_ExternAlias.csproj";
        var projPath2 = @"CSharpProject\CSharpProject_ExternAlias2.csproj";

        var files = new FileSet(
            (projPath1, Resources.ProjectFiles.CSharp.ExternAlias),
            (projPath2, Resources.ProjectFiles.CSharp.ExternAlias2),
            (@"CSharpProject\CSharpExternAlias.cs", Resources.SourceFiles.CSharp.CSharpExternAlias));

        CreateFiles(files);

        var fullPath1 = GetSolutionFileName(projPath1);
        var fullPath2 = GetSolutionFileName(projPath2);
        using var workspace = CreateMSBuildWorkspace();
        var proj1 = await workspace.OpenProjectAsync(fullPath1);
        var proj2 = await workspace.OpenProjectAsync(fullPath2);

        var p1Sys1 = GetMetadataReferenceByAlias(proj1, "Sys1");
        var p1Sys2 = GetMetadataReferenceByAlias(proj1, "Sys2");
        var p2Sys1 = GetMetadataReferenceByAlias(proj2, "Sys1");
        var p2Sys3 = GetMetadataReferenceByAlias(proj2, "Sys3");

        Assert.NotNull(p1Sys1);
        Assert.NotNull(p1Sys2);
        Assert.NotNull(p2Sys1);
        Assert.NotNull(p2Sys3);

        // same filepath but different alias so they are not the same instance
        Assert.NotSame(p1Sys1, p1Sys2);
        Assert.NotSame(p2Sys1, p2Sys3);

        // same filepath and alias so they are the same instance
        Assert.Same(p1Sys1, p2Sys1);

        var mdp1Sys1 = GetMetadata(p1Sys1);
        var mdp1Sys2 = GetMetadata(p1Sys2);
        var mdp2Sys1 = GetMetadata(p2Sys1);
        var mdp2Sys3 = GetMetadata(p2Sys1);

        Assert.NotNull(mdp1Sys1);
        Assert.NotNull(mdp1Sys2);
        Assert.NotNull(mdp2Sys1);
        Assert.NotNull(mdp2Sys3);

        // all references to System.dll share the same metadata bytes
        Assert.Same(mdp1Sys1.Id, mdp1Sys2.Id);
        Assert.Same(mdp1Sys1.Id, mdp2Sys1.Id);
        Assert.Same(mdp1Sys1.Id, mdp2Sys3.Id);
    }

    private static MetadataReference GetMetadataReference(Project project, string name)
        => project.MetadataReferences
            .OfType<PortableExecutableReference>()
            .SingleOrDefault(mr => mr.FilePath.Contains(name));

    private static MetadataReference GetMetadataReferenceByAlias(Project project, string aliasName)
        => project.MetadataReferences
            .OfType<PortableExecutableReference>()
            .SingleOrDefault(mr =>
                !mr.Properties.Aliases.IsDefault &&
                mr.Properties.Aliases.Contains(aliasName));

    private static Metadata GetMetadata(MetadataReference metadataReference)
        => ((PortableExecutableReference)metadataReference).GetMetadata();

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), AlwaysSkip = "https://github.com/microsoft/vs-solutionpersistence/issues/95")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552981")]
    public async Task TestOpenSolution_DuplicateProjectGuids()
    {
        CreateFiles(GetSolutionWithDuplicatedGuidFiles());
        var solutionFilePath = GetSolutionFileName("DuplicatedGuids.sln");

        // Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/831379")]
    public async Task GetCompilationWithCircularProjectReferences()
    {
        CreateFiles(GetSolutionWithCircularProjectReferences());
        var solutionFilePath = GetSolutionFileName("CircularSolution.sln");

        //  Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        // Verify we can get compilations for both projects
        var projects = solution.Projects.ToArray();

        // Exactly one of them should have a reference to the other. Which one it is, is unspecced
        Assert.True(projects[0].ProjectReferences.Any(r => r.ProjectId == projects[1].Id) ||
                    projects[1].ProjectReferences.Any(r => r.ProjectId == projects[0].Id));

        var compilation1 = await projects[0].GetCompilationAsync();
        var compilation2 = await projects[1].GetCompilationAsync();

        // Exactly one of them should have a compilation to the other. Which one it is, is unspecced
        Assert.True(compilation1.References.OfType<CompilationReference>().Any(c => c.Compilation == compilation2) ||
                    compilation2.References.OfType<CompilationReference>().Any(c => c.Compilation == compilation1));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOutputFilePaths()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var p1 = sol.Projects.First(p => p.Language == LanguageNames.CSharp);
        var p2 = sol.Projects.First(p => p.Language == LanguageNames.VisualBasic);

        Assert.Equal("CSharpProject.dll", Path.GetFileName(p1.OutputFilePath));
        Assert.Equal("VisualBasicProject.dll", Path.GetFileName(p2.OutputFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOutputInfo()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var p1 = sol.Projects.First(p => p.Language == LanguageNames.CSharp);
        var p2 = sol.Projects.First(p => p.Language == LanguageNames.VisualBasic);

        Assert.Equal("CSharpProject.dll", Path.GetFileName(p1.CompilationOutputInfo.AssemblyPath));
        Assert.Equal("VisualBasicProject.dll", Path.GetFileName(p2.CompilationOutputInfo.AssemblyPath));
    }

    [ConditionalTheory(typeof(VisualStudioMSBuildInstalled))]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task TestChecksumAlgorithm_NonDefault(string language)
    {
        var files = language == LanguageNames.CSharp
            ? GetSimpleCSharpSolutionFiles().WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithChecksumAlgorithm)
            : GetSimpleVisualBasicSolutionFiles().WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithChecksumAlgorithm);

        CreateFiles(files);
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.Single();

        Assert.Equal(SourceHashAlgorithm.Sha1, project.State.ChecksumAlgorithm);

        Assert.All(project.Documents, d => Assert.Equal(SourceHashAlgorithm.Sha1, d.GetTextSynchronously(default).ChecksumAlgorithm));
        Assert.All(project.AdditionalDocuments, d => Assert.Equal(SourceHashAlgorithm.Sha1, d.GetTextSynchronously(default).ChecksumAlgorithm));
    }

    [ConditionalTheory(typeof(VisualStudioMSBuildInstalled))]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task TestChecksumAlgorithm_Default(string language)
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = sol.Projects.First(p => p.Language == language);

        Assert.Equal(SourceHashAlgorithms.Default, project.State.ChecksumAlgorithm);

        Assert.All(project.Documents, d => Assert.Equal(SourceHashAlgorithms.Default, d.GetTextSynchronously(default).ChecksumAlgorithm));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCrossLanguageReferencesUsesInMemoryGeneratedMetadata()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var p1 = sol.Projects.First(p => p.Language == LanguageNames.CSharp);
        var p2 = sol.Projects.First(p => p.Language == LanguageNames.VisualBasic);

        // prove there is no existing metadata on disk for this project
        Assert.Equal("CSharpProject.dll", Path.GetFileName(p1.OutputFilePath));
        Assert.False(File.Exists(p1.OutputFilePath));

        // prove that vb project refers to csharp project via generated metadata (skeleton) assembly.
        // it should be a MetadataImageReference
        var c2 = await p2.GetCompilationAsync();
        var pref = c2.References.OfType<PortableExecutableReference>().FirstOrDefault(r => r.Display == "CSharpProject");
        Assert.NotNull(pref);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCrossLanguageReferencesWithOutOfDateMetadataOnDiskUsesInMemoryGeneratedMetadata()
    {
        await PrepareCrossLanguageProjectWithEmittedMetadataAsync();
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        // recreate the solution so it will reload from disk
        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var p1 = sol.Projects.First(p => p.Language == LanguageNames.CSharp);

        // update project with top level change that should now invalidate use of metadata from disk
        var d1 = p1.Documents.First();
        var root = await d1.GetSyntaxRootAsync();
        var decl = root.DescendantNodes().OfType<CS.Syntax.ClassDeclarationSyntax>().First();
        var newDecl = decl.WithIdentifier(CS.SyntaxFactory.Identifier("Pogrom").WithLeadingTrivia(decl.Identifier.LeadingTrivia).WithTrailingTrivia(decl.Identifier.TrailingTrivia));
        var newRoot = root.ReplaceNode(decl, newDecl);
        var newDoc = d1.WithSyntaxRoot(newRoot);
        p1 = newDoc.Project;
        var p2 = p1.Solution.Projects.First(p => p.Language == LanguageNames.VisualBasic);

        // we should now find a MetadataImageReference that was generated instead of a MetadataFileReference
        var c2 = await p2.GetCompilationAsync();
        var pref = c2.References.OfType<PortableExecutableReference>().FirstOrDefault(r => r.Display == "EmittedCSharpProject");
        Assert.NotNull(pref);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), AlwaysSkip = "https://github.com/dotnet/roslyn/issues/54818")]
    public async Task TestInternalsVisibleToSigned()
    {
        var solution = await SolutionAsync(
            Project(
                ProjectName("Project1"),
                Sign,
                Document(string.Format(
                    """
                    using System.Runtime.CompilerServices;
                    [assembly:InternalsVisibleTo("Project2, PublicKey={0}")]
                    class C1
                    {{
                    }}
                    """, PublicKey))),
            Project(
                ProjectName("Project2"),
                Sign,
                ProjectReference("Project1"),
                Document(@"class C2 : C1 { }")));

        var project2 = solution.GetProjectsByName("Project2").First();
        var compilation = await project2.GetCompilationAsync();
        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToArray();

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestVersions()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var sversion = solution.Version;
        var latestPV = solution.GetLatestProjectVersion();
        var project = solution.Projects.First();
        var pversion = project.Version;
        var document = project.Documents.First();
        var dversion = await document.GetTextVersionAsync();
        var latestDV = await project.GetLatestDocumentVersionAsync();

        // update document
        var solution1 = solution.WithDocumentText(document.Id, SourceText.From("using test;"));
        var document1 = solution1.GetDocument(document.Id);
        var dversion1 = await document1.GetTextVersionAsync();
        Assert.NotEqual(dversion, dversion1); // new document version
        Assert.True(dversion1.GetTestAccessor().IsNewerThan(dversion));
        Assert.Equal(solution.Version, solution1.Version); // updating document should not have changed solution version
        Assert.Equal(project.Version, document1.Project.Version); // updating doc should not have changed project version
        var latestDV1 = await document1.Project.GetLatestDocumentVersionAsync();
        Assert.NotEqual(latestDV, latestDV1);
        Assert.True(latestDV1.GetTestAccessor().IsNewerThan(latestDV));
        Assert.Equal(latestDV1, await document1.GetTextVersionAsync()); // projects latest doc version should be this doc's version

        // update project
        var solution2 = solution1.WithProjectCompilationOptions(project.Id, project.CompilationOptions.WithOutputKind(OutputKind.NetModule));
        var document2 = solution2.GetDocument(document.Id);
        var dversion2 = await document2.GetTextVersionAsync();
        Assert.Equal(dversion1, dversion2); // document didn't change, so version should be the same.
        Assert.NotEqual(document1.Project.Version, document2.Project.Version); // project did change, so project versions should be different
        Assert.True(document2.Project.Version.GetTestAccessor().IsNewerThan(document1.Project.Version));
        Assert.Equal(solution1.Version, solution2.Version); // solution didn't change, just individual project.

        // update solution
        var pid2 = ProjectId.CreateNewId();
        var solution3 = solution2.AddProject(pid2, "foo", "foo", LanguageNames.CSharp);
        Assert.NotEqual(solution2.Version, solution3.Version); // solution changed, added project.
        Assert.True(solution3.Version.GetTestAccessor().IsNewerThan(solution2.Version));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_LoadMetadataForReferencedProjects()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        workspace.LoadMetadataForReferencedProjects = true;

        var project = await workspace.OpenProjectAsync(projectFilePath);
        var document = project.Documents.First();
        var tree = await document.GetSyntaxTreeAsync();
        var expectedFileName = GetSolutionFileName(@"CSharpProject\CSharpClass.cs");
        Assert.Equal(expectedFileName, tree.FilePath);
        var type = tree.GetRoot().DescendantTokens().First(t => t.ToString() == "class").Parent;
        Assert.NotNull(type);
        Assert.StartsWith("public class CSharpClass", type.ToString(), StringComparison.Ordinal);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33047")]
    public async Task TestOpenProject_CSharp_GlobalPropertyShouldUnsetParentConfigurationAndPlatformDefault()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.ShouldUnsetParentConfigurationAndPlatform)
            .WithFile(@"CSharpProject\ShouldUnsetParentConfigurationAndPlatformConditional.cs", Resources.SourceFiles.CSharp.CSharpClass));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var document = project.Documents.First();
        var tree = await document.GetSyntaxTreeAsync();
        var expectedFileName = GetSolutionFileName(@"CSharpProject\CSharpClass.cs");
        Assert.Equal(expectedFileName, tree.FilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33047")]
    public async Task TestOpenProject_CSharp_GlobalPropertyShouldUnsetParentConfigurationAndPlatformTrue()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.ShouldUnsetParentConfigurationAndPlatform)
            .WithFile(@"CSharpProject\ShouldUnsetParentConfigurationAndPlatformConditional.cs", Resources.SourceFiles.CSharp.CSharpClass));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace(("ShouldUnsetParentConfigurationAndPlatform", bool.TrueString));
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var document = project.Documents.First();
        var tree = await document.GetSyntaxTreeAsync();
        var expectedFileName = GetSolutionFileName(@"CSharpProject\ShouldUnsetParentConfigurationAndPlatformConditional.cs");
        Assert.Equal(expectedFileName, tree.FilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_CSharp_WithoutPrefer32BitAndConsoleApplication()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithoutPrefer32Bit));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_CSharp_WithoutPrefer32BitAndLibrary()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithoutPrefer32Bit)
            .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", "OutputType", "Library"));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_CSharp_WithPrefer32BitAndConsoleApplication()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithPrefer32Bit));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu32BitPreferred, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_CSharp_WithPrefer32BitAndLibrary()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithPrefer32Bit)
            .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", "OutputType", "Library"));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_CSharp_WithPrefer32BitAndWinMDObj()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithPrefer32Bit)
            .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", "OutputType", "winmdobj"));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_CSharp_WithoutOutputPath()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", "OutputPath", ""));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.NotEmpty(project.OutputFilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_CSharp_WithoutAssemblyName()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", "AssemblyName", ""));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.NotEmpty(project.OutputFilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_CSharp_WithoutCSharpTargetsImported_DocumentsArePickedUp()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithoutCSharpTargetsImported));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        //Msbuild failed when processing the file with message: Project does not contain 'Compile' target.
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        Assert.NotEmpty(project.Documents);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_VisualBasic_WithoutVBTargetsImported_DocumentsArePickedUp()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithoutVBTargetsImported));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        //Msbuild failed when processing the file with message: Project does not contain 'Compile' target.
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.NotEmpty(project.Documents);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_VisualBasic_WithoutPrefer32BitAndConsoleApplication()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithoutPrefer32Bit));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_VisualBasic_WithoutPrefer32BitAndLibrary()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithoutPrefer32Bit)
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "OutputType", "Library"));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_VisualBasic_WithPrefer32BitAndConsoleApplication()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithPrefer32Bit));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu32BitPreferred, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_VisualBasic_WithPrefer32BitAndLibrary()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithPrefer32Bit)
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "OutputType", "Library"));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739043")]
    public async Task TestOpenProject_VisualBasic_WithPrefer32BitAndWinMDObj()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithPrefer32Bit)
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "OutputType", "winmdobj"));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        Assert.Equal(Platform.AnyCpu, compilation.Options.Platform);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_VisualBasic_WithoutOutputPath()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithPrefer32Bit)
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "OutputPath", ""));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.NotEmpty(project.OutputFilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_VisualBasic_WithLanguageVersion15_3()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "LangVersion", "15.3"));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        // Warning:Found project reference without a matching metadata reference:
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.Equal(VB.LanguageVersion.VisualBasic15_3, ((VB.VisualBasicParseOptions)project.ParseOptions).LanguageVersion);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_VisualBasic_WithLatestLanguageVersion()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "LangVersion", "Latest"));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        //  Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.Equal(VB.LanguageVersionFacts.MapSpecifiedToEffectiveVersion(VB.LanguageVersion.Latest), ((VB.VisualBasicParseOptions)project.ParseOptions).LanguageVersion);
        Assert.Equal(VB.LanguageVersion.Latest, ((VB.VisualBasicParseOptions)project.ParseOptions).SpecifiedLanguageVersion);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_VisualBasic_WithoutAssemblyName()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.WithPrefer32Bit)
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "AssemblyName", ""));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.Empty(workspace.Diagnostics);
        Assert.NotEmpty(project.OutputFilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task Test_Respect_ReferenceOutputassembly_Flag()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"VisualBasicProject_Circular_Top.vbproj", Resources.ProjectFiles.VisualBasic.Circular_Top)
            .WithFile(@"VisualBasicProject_Circular_Target.vbproj", Resources.ProjectFiles.VisualBasic.Circular_Target));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject_Circular_Top.vbproj");

        // The referenced project 'VisualBasic_Circular_Target.vbproj' does not exist
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);
        Assert.Empty(project.ProjectReferences);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithXaml()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithXaml)
            .WithFile(@"CSharpProject\App.xaml", Resources.SourceFiles.Xaml.App)
            .WithFile(@"CSharpProject\App.xaml.cs", Resources.SourceFiles.CSharp.App)
            .WithFile(@"CSharpProject\MainWindow.xaml", Resources.SourceFiles.Xaml.MainWindow)
            .WithFile(@"CSharpProject\MainWindow.xaml.cs", Resources.SourceFiles.CSharp.MainWindow));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        // Ensure the Xaml compiler does not run in a separate appdomain. It appears that this won't work within xUnit.
        using var workspace = CreateMSBuildWorkspace(
            ("AlwaysCompileMarkupFilesInSeparateDomain", "false"));
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var documents = project.Documents.ToList();

        // AssemblyInfo.cs, App.xaml.cs, MainWindow.xaml.cs, App.g.cs, MainWindow.g.cs, + unusual AssemblyAttributes.cs
        Assert.Equal(6, documents.Count);

        // both xaml code behind files are documents
        Assert.Contains(documents, d => d.Name == "App.xaml.cs");
        Assert.Contains(documents, d => d.Name == "MainWindow.xaml.cs");

        // prove no xaml files are documents
        Assert.DoesNotContain(documents, d => d.Name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));

        // prove that generated source files for xaml files are included in documents list
        Assert.Contains(documents, d => d.Name == "App.g.cs");
        Assert.Contains(documents, d => d.Name == "MainWindow.g.cs");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestMetadataReferenceHasBadHintPath()
    {
        // prove that even with bad hint path for metadata reference the workspace can succeed at finding the correct metadata reference.
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.BadHintPath));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        var refs = project.MetadataReferences.ToList();
        var csharpLib = refs.OfType<PortableExecutableReference>().FirstOrDefault(r => r.FilePath.Contains("Microsoft.CSharp"));
        Assert.NotNull(csharpLib);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531631")]
    public async Task TestOpenProject_AssemblyNameIsPath()
    {
        // prove that even if assembly name is specified as a path instead of just a name, workspace still succeeds at opening project.
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.AssemblyNameIsPath));

        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        var comp = await project.GetCompilationAsync();
        Assert.Equal("ReproApp", comp.AssemblyName);
        var expectedOutputPath = Path.GetDirectoryName(project.FilePath);
        Assert.Equal(expectedOutputPath, Path.GetDirectoryName(project.OutputFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531631")]
    public async Task TestOpenProject_AssemblyNameIsPath2()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.AssemblyNameIsPath2));

        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        var comp = await project.GetCompilationAsync();
        Assert.Equal("ReproApp", comp.AssemblyName);
        var expectedOutputPath = Path.Combine(Path.GetDirectoryName(project.FilePath), @"bin");
        Assert.Equal(expectedOutputPath, Path.GetDirectoryName(Path.GetFullPath(project.OutputFilePath)));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithDuplicateFile()
    {
        // Verify that we don't throw in this case
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.DuplicateFile));

        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        var documents = project.Documents.Where(d => d.Name == "CSharpClass.cs").ToList();
        Assert.Equal(2, documents.Count);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithInvalidFileExtensionAsync()
    {
        // make sure the file does in fact exist, but with an unrecognized extension
        const string ProjFileName = @"CSharpProject\CSharpProject.csproj.nyi";
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(ProjFileName, Resources.ProjectFiles.CSharp.CSharpProject));

        var e = await Assert.ThrowsAsync<InvalidOperationException>(async delegate
        {
            await MSBuildWorkspace.Create().OpenProjectAsync(GetSolutionFileName(ProjFileName));
        });

        var expected = string.Format(WorkspacesResources.Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language, GetSolutionFileName(ProjFileName), ".nyi");
        Assert.Equal(expected, e.Message);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_ProjectFileExtensionAssociatedWithUnknownLanguageAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var projFileName = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
        var language = "lingo";
        var e = await Assert.ThrowsAsync<InvalidOperationException>(async delegate
        {
            var ws = MSBuildWorkspace.Create();
            ws.AssociateFileExtensionWithLanguage("csproj", language); // non-existent language
            await ws.OpenProjectAsync(projFileName);
        });

        // the exception should tell us something about the language being unrecognized.
        var expected = string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projFileName, language);
        Assert.Equal(expected, e.Message);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithAssociatedLanguageExtension1()
    {
        // make a CSharp solution with a project file having the incorrect extension 'vbproj', and then load it using the overload the lets us
        // specify the language directly, instead of inferring from the extension
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.vbproj", Resources.ProjectFiles.CSharp.CSharpProject));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        workspace.AssociateFileExtensionWithLanguage("vbproj", LanguageNames.CSharp);
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var document = project.Documents.First();
        var tree = await document.GetSyntaxTreeAsync();
        var diagnostics = tree.GetDiagnostics();
        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithAssociatedLanguageExtension2_IgnoreCase()
    {
        // make a CSharp solution with a project file having the incorrect extension 'anyproj', and then load it using the overload the lets us
        // specify the language directly, instead of inferring from the extension
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.anyproj", Resources.ProjectFiles.CSharp.CSharpProject));
        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.anyproj");

        using var workspace = CreateMSBuildWorkspace();
        // prove that the association works even if the case is different
        workspace.AssociateFileExtensionWithLanguage("ANYPROJ", LanguageNames.CSharp);
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var document = project.Documents.First();
        var tree = await document.GetSyntaxTreeAsync();
        var diagnostics = tree.GetDiagnostics();
        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithNonExistentSolutionFile_FailsAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("NonExistentSolution.sln");

        using var workspace = CreateMSBuildWorkspace();

        await Assert.ThrowsAsync<FileNotFoundException>(() => workspace.OpenSolutionAsync(solutionFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithInvalidSolutionFile_FailsAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName(@"http://localhost/Invalid/InvalidSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);

        await AssertThrowsExceptionForInvalidPath(() => workspace.OpenSolutionAsync(solutionFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithTemporaryLockedFile_SucceedsWithoutFailureEvent()
    {
        // when skipped we should see a diagnostic for the invalid project

        CreateFiles(GetSimpleCSharpSolutionFiles());

        using var ws = CreateMSBuildWorkspace();
        // open source file so it cannot be read by workspace;
        var sourceFile = GetSolutionFileName(@"CSharpProject\CSharpClass.cs");
        var file = File.Open(sourceFile, FileMode.Open, FileAccess.Write, FileShare.None);
        try
        {
            var solution = await ws.OpenSolutionAsync(GetSolutionFileName(@"TestSolution.sln"));
            var doc = solution.Projects.First().Documents.First(d => d.FilePath == sourceFile);

            // start reading text
            var getTextTask = doc.GetTextAsync();

            // wait 1 unit of retry delay then close file
            var delay = TextLoader.RetryDelay;
            await Task.Delay(delay).ContinueWith(t => file.Close(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            // finish reading text
            var text = await getTextTask;
            Assert.NotEmpty(text.ToString());
        }
        finally
        {
            file.Close();
        }

        Assert.Empty(ws.Diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithLockedFile_LoadsWithEmptyText()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);

        // open source file so it cannot be read by workspace;
        var sourceFile = GetSolutionFileName(@"CSharpProject\CSharpClass.cs");
        using var file = File.Open(sourceFile, FileMode.Open, FileAccess.Write, FileShare.None);

        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var doc = solution.Projects.First().Documents.First(d => d.FilePath == sourceFile);
        var text = await doc.GetTextAsync();
        Assert.Empty(text.ToString());
        Assert.NotNull(await doc.State.GetFailedToLoadExceptionMessageAsync(CancellationToken.None));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithInvalidProjectPath_SkipTrue_SucceedsWithFailureEvent()
    {
        // when skipped we should see a diagnostic for the invalid project
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");
        CreateFiles(GetSimpleCSharpProjectFiles()
            .WithFile(solutionFilePath, Resources.SolutionFiles.InvalidProjectPath));

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false, skipUnrecognizedProjects: true);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        Assert.Single(workspace.Diagnostics);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/985906")]
    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task HandleSolutionProjectTypeSolutionFolder()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.SolutionFolder));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        Assert.Empty(workspace.Diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithInvalidProjectPath_SkipFalse_Fails()
    {
        // when not skipped we should get an exception for the invalid project

        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.InvalidProjectPath));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        workspace.SkipUnrecognizedProjects = false;

        await AssertThrowsExceptionForInvalidPath(() => workspace.OpenSolutionAsync(solutionFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithNonExistentProject_SkipTrue_SucceedsWithFailureEvent()
    {
        // when skipped we should see a diagnostic for the non-existent project

        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.NonExistentProject));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false, skipUnrecognizedProjects: true);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        Assert.Single(workspace.Diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithNonExistentProject_SkipFalse_Fails()
    {
        // when skipped we should see an exception for the non-existent project

        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.NonExistentProject));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        workspace.SkipUnrecognizedProjects = false;

        await Assert.ThrowsAsync<FileNotFoundException>(() => workspace.OpenSolutionAsync(solutionFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithUnrecognizedProjectFileExtension_Fails()
    {
        // proves that for solution open, project type guid and extension are both necessary
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.CSharp_UnknownProjectExtension)
            .WithFile(@"CSharpProject\CSharpProject.noproj", Resources.ProjectFiles.CSharp.CSharpProject));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        Assert.Empty(solution.ProjectIds);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithUnrecognizedProjectTypeGuidButRecognizedExtension_Succeeds()
    {
        // proves that if project type guid is not recognized, a known project file extension is all we need.
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.CSharp_UnknownProjectTypeGuid));
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        Assert.Single(solution.ProjectIds);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithUnrecognizedProjectTypeGuidAndUnrecognizedExtension_WithSkipTrue_SucceedsWithFailureEvent()
    {
        // proves that if both project type guid and file extension are unrecognized, then project is skipped.
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.CSharp_UnknownProjectTypeGuidAndUnknownExtension)
            .WithFile(@"CSharpProject\CSharpProject.noproj", Resources.ProjectFiles.CSharp.CSharpProject));

        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        Assert.Single(workspace.Diagnostics);
        Assert.Empty(solution.ProjectIds);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithUnrecognizedProjectTypeGuidAndUnrecognizedExtension_WithSkipFalse_FailsAsync()
    {
        // proves that if both project type guid and file extension are unrecognized, then open project fails.
        const string NoProjFileName = @"CSharpProject\CSharpProject.noproj";
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"TestSolution.sln", Resources.SolutionFiles.CSharp_UnknownProjectTypeGuidAndUnknownExtension)
            .WithFile(NoProjFileName, Resources.ProjectFiles.CSharp.CSharpProject));

        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        var e = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var workspace = CreateMSBuildWorkspace();
            workspace.SkipUnrecognizedProjects = false;
            await workspace.OpenSolutionAsync(solutionFilePath);
        });

        var noProjFullFileName = GetSolutionFileName(NoProjFileName);
        var expected = string.Format(WorkspacesResources.Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language, noProjFullFileName, ".noproj");
        Assert.Equal(expected, e.Message);
    }

    private static readonly IEnumerable<Assembly> _defaultAssembliesWithoutCSharp = MefHostServices.DefaultAssemblies.Where(a => !a.FullName.Contains("CSharp"));

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3931")]
    public async Task TestOpenSolution_WithMissingLanguageLibraries_WithSkipFalse_ThrowsAsync()
    {
        // proves that if the language libraries are missing then the appropriate error occurs
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        var e = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var workspace = CreateMSBuildWorkspace(MefHostServices.Create(_defaultAssembliesWithoutCSharp));
            workspace.SkipUnrecognizedProjects = false;
            await workspace.OpenSolutionAsync(solutionFilePath);
        });

        var projFileName = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
        var expected = string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projFileName, LanguageNames.CSharp);
        Assert.Equal(expected, e.Message);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3931")]
    public async Task TestOpenSolution_WithMissingLanguageLibraries_WithSkipTrue_SucceedsWithDiagnostic()
    {
        // proves that if the language libraries are missing then the appropriate error occurs
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(MefHostServices.Create(_defaultAssembliesWithoutCSharp));
        workspace.SkipUnrecognizedProjects = true;

        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        var projFileName = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
        var expected = string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projFileName, LanguageNames.CSharp);
        Assert.Equal(expected, workspace.Diagnostics.Single().Message);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/3931")]
    public async Task TestOpenProject_WithMissingLanguageLibraries_Throws()
    {
        // proves that if the language libraries are missing then the appropriate error occurs
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var projectName = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = MSBuildWorkspace.Create(MefHostServices.Create(_defaultAssembliesWithoutCSharp));
        var e = await Assert.ThrowsAsync<InvalidOperationException>(() => workspace.OpenProjectAsync(projectName));

        var expected = string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projectName, LanguageNames.CSharp);
        Assert.Equal(expected, e.Message);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithInvalidFilePath_Fails()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var projectFilePath = GetSolutionFileName(@"http://localhost/Invalid/InvalidProject.csproj");

        using var workspace = CreateMSBuildWorkspace();

        await AssertThrowsExceptionForInvalidPath(() => workspace.OpenProjectAsync(projectFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithNonExistentProjectFile_FailsAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var projectFilePath = GetSolutionFileName(@"CSharpProject\NonExistentProject.csproj");

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            using var workspace = CreateMSBuildWorkspace();
            await workspace.OpenProjectAsync(projectFilePath);
        });
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithInvalidProjectReference_SkipTrue_SucceedsWithEvent()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.InvalidProjectReference));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Single(project.Solution.ProjectIds); // didn't really open referenced project due to invalid file path.
        Assert.Empty(project.ProjectReferences); // no resolved project references
        Assert.Single(project.AllProjectReferences); // dangling project reference

        Assert.NotEmpty(workspace.Diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithInvalidProjectReference_SkipFalse_Fails()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.InvalidProjectReference));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        workspace.SkipUnrecognizedProjects = false;
        await AssertThrowsExceptionForInvalidPath(() => workspace.OpenProjectAsync(projectFilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithNonExistentProjectReference_SkipTrue_SucceedsWithEvent()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.NonExistentProjectReference));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Single(project.Solution.ProjectIds); // didn't really open referenced project due to invalid file path.
        Assert.Empty(project.ProjectReferences); // no resolved project references
        Assert.Single(project.AllProjectReferences); // dangling project reference

        Assert.NotEmpty(workspace.Diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithNonExistentProjectReference_SkipFalse_FailsAsync()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.NonExistentProjectReference));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
            workspace.SkipUnrecognizedProjects = false;
            await workspace.OpenProjectAsync(projectFilePath);
        });
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithUnrecognizedProjectReferenceFileExtension_SkipTrue_SucceedsWithEvent()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.UnknownProjectExtension)
            .WithFile(@"CSharpProject\CSharpProject.noproj", Resources.ProjectFiles.CSharp.CSharpProject));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Single(project.Solution.ProjectIds); // didn't really open referenced project due to unrecognized extension.
        Assert.Empty(project.ProjectReferences); // no resolved project references
        Assert.Single(project.AllProjectReferences); // dangling project reference

        Assert.NotEmpty(workspace.Diagnostics);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithUnrecognizedProjectReferenceFileExtension_SkipFalse_Fails()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.UnknownProjectExtension)
            .WithFile(@"CSharpProject\CSharpProject.noproj", Resources.ProjectFiles.CSharp.CSharpProject));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var workspace = CreateMSBuildWorkspace();
            workspace.SkipUnrecognizedProjects = false;
            await workspace.OpenProjectAsync(projectFilePath);
        });
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithUnrecognizedProjectReferenceFileExtension_WithMetadata_SkipTrue_SucceedsByLoadingMetadata()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.UnknownProjectExtension)
            .WithFile(@"CSharpProject\CSharpProject.noproj", Resources.ProjectFiles.CSharp.CSharpProject)
            .WithFile(@"CSharpProject\bin\Debug\CSharpProject.dll", Resources.Dlls.CSharpProject));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Single(project.Solution.ProjectIds);
        Assert.Empty(project.ProjectReferences);
        Assert.Empty(project.AllProjectReferences);

        var metaRefs = project.MetadataReferences.ToList();
        Assert.Contains(metaRefs, r => r is PortableExecutableReference reference && reference.Display.Contains("CSharpProject.dll"));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithUnrecognizedProjectReferenceFileExtension_WithMetadata_SkipFalse_SucceedsByLoadingMetadata()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.UnknownProjectExtension)
            .WithFile(@"CSharpProject\CSharpProject.noproj", Resources.ProjectFiles.CSharp.CSharpProject)
            .WithFile(@"CSharpProject\bin\Debug\CSharpProject.dll", Resources.Dlls.CSharpProject));
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        workspace.SkipUnrecognizedProjects = false;
        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Single(project.Solution.ProjectIds);
        Assert.Empty(project.ProjectReferences);
        Assert.Empty(project.AllProjectReferences);
        Assert.Contains(project.MetadataReferences, r => r is PortableExecutableReference reference && reference.Display.Contains("CSharpProject.dll"));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithUnrecognizedProjectReferenceFileExtension_BadMsbuildProject_SkipTrue_SucceedsWithDanglingProjectReference()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.UnknownProjectExtension)
            .WithFile(@"CSharpProject\CSharpProject.noproj", Resources.Dlls.CSharpProject)); // use metadata file as stand-in for bad project file

        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        workspace.SkipUnrecognizedProjects = true;

        var project = await workspace.OpenProjectAsync(projectFilePath);

        Assert.Single(project.Solution.ProjectIds);
        Assert.Empty(project.ProjectReferences);
        Assert.Single(project.AllProjectReferences);

        Assert.InRange(workspace.Diagnostics.Count, 2, 3);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithReferencedProject_LoadMetadata_ExistingMetadata_Succeeds()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"CSharpProject\bin\Debug\CSharpProject.dll", Resources.Dlls.CSharpProject));

        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        using var workspace = CreateMSBuildWorkspace();
        workspace.LoadMetadataForReferencedProjects = true;
        var project = await workspace.OpenProjectAsync(projectFilePath);

        // referenced project got converted to a metadata reference
        var projRefs = project.ProjectReferences.ToList();
        var metaRefs = project.MetadataReferences.ToList();
        Assert.Empty(projRefs);
        Assert.Contains(metaRefs, r => r is PortableExecutableReference reference && reference.Display.Contains("CSharpProject.dll"));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithReferencedProject_LoadMetadata_NonExistentMetadata_LoadsProjectInstead()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

        // Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        workspace.LoadMetadataForReferencedProjects = true;
        var project = await workspace.OpenProjectAsync(projectFilePath);

        // referenced project is still a project ref, did not get converted to metadata ref
        var projRefs = project.ProjectReferences.ToList();
        var metaRefs = project.MetadataReferences.ToList();
        Assert.Single(projRefs);
        Assert.DoesNotContain(metaRefs, r => r.Properties.Aliases.Contains("CSharpProject"));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_UpdateExistingReferences()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"CSharpProject\bin\Debug\CSharpProject.dll", Resources.Dlls.CSharpProject));
        var vbProjectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");
        var csProjectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        // first open vb project that references c# project, but only reference the c# project's built metadata
        using var workspace = CreateMSBuildWorkspace();
        workspace.LoadMetadataForReferencedProjects = true;
        var vbProject = await workspace.OpenProjectAsync(vbProjectFilePath);

        // prove vb project references c# project as a metadata reference
        Assert.Empty(vbProject.ProjectReferences);
        Assert.Contains(vbProject.MetadataReferences, r => r is PortableExecutableReference reference && reference.Display.Contains("CSharpProject.dll"));

        // now explicitly open the c# project that got referenced as metadata
        var csProject = await workspace.OpenProjectAsync(csProjectFilePath);

        // show that the vb project now references the c# project directly (not as metadata)
        vbProject = workspace.CurrentSolution.GetProject(vbProject.Id);
        Assert.Single(vbProject.ProjectReferences);
        Assert.DoesNotContain(vbProject.MetadataReferences, r => r.Properties.Aliases.Contains("CSharpProject"));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(Framework35Installed))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528984")]
    public async Task TestOpenProject_AddVBDefaultReferences()
    {
        var files = new FileSet(
            ("VisualBasicProject_3_5.vbproj", Resources.ProjectFiles.VisualBasic.VisualBasicProject_3_5),
            ("VisualBasicProject_VisualBasicClass.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass));

        CreateFiles(files);

        var projectFilePath = GetSolutionFileName("VisualBasicProject_3_5.vbproj");

        // The reference assemblies for .NETFramework,Version=v4.0 were not found.
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);
        var compilation = await project.GetCompilationAsync();
        var diagnostics = compilation.GetDiagnostics();
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_DebugType_Full()
    {
        CreateCSharpFilesWith("DebugType", "full");
        await AssertCSParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_DebugType_None()
    {
        CreateCSharpFilesWith("DebugType", "none");
        await AssertCSParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_DebugType_PDBOnly()
    {
        CreateCSharpFilesWith("DebugType", "pdbonly");
        await AssertCSParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_DebugType_Portable()
    {
        CreateCSharpFilesWith("DebugType", "portable");
        await AssertCSParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_DebugType_Embedded()
    {
        CreateCSharpFilesWith("DebugType", "embedded");
        await AssertCSParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_OutputKind_DynamicallyLinkedLibrary()
    {
        CreateCSharpFilesWith("OutputType", "Library");
        await AssertCSCompilationOptionsAsync(OutputKind.DynamicallyLinkedLibrary, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_OutputKind_ConsoleApplication()
    {
        CreateCSharpFilesWith("OutputType", "Exe");
        await AssertCSCompilationOptionsAsync(OutputKind.ConsoleApplication, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_OutputKind_WindowsApplication()
    {
        CreateCSharpFilesWith("OutputType", "WinExe");
        await AssertCSCompilationOptionsAsync(OutputKind.WindowsApplication, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_OutputKind_NetModule()
    {
        CreateCSharpFilesWith("OutputType", "Module");
        await AssertCSCompilationOptionsAsync(OutputKind.NetModule, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_OptimizationLevel_Release()
    {
        CreateCSharpFilesWith("Optimize", "True");
        await AssertCSCompilationOptionsAsync(OptimizationLevel.Release, options => options.OptimizationLevel);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_OptimizationLevel_Debug()
    {
        CreateCSharpFilesWith("Optimize", "False");
        await AssertCSCompilationOptionsAsync(OptimizationLevel.Debug, options => options.OptimizationLevel);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_MainFileName()
    {
        CreateCSharpFilesWith("StartupObject", "Foo");
        await AssertCSCompilationOptionsAsync("Foo", options => options.MainTypeName);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_AssemblyOriginatorKeyFile_SignAssembly_Missing()
    {
        CreateCSharpFiles();
        await AssertCSCompilationOptionsAsync(null, options => options.CryptoKeyFile);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_AssemblyOriginatorKeyFile_SignAssembly_False()
    {
        CreateCSharpFilesWith("SignAssembly", "false");
        await AssertCSCompilationOptionsAsync(null, options => options.CryptoKeyFile);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_AssemblyOriginatorKeyFile_SignAssembly_True()
    {
        CreateCSharpFilesWith("SignAssembly", "true");
        await AssertCSCompilationOptionsAsync("snKey.snk", options => Path.GetFileName(options.CryptoKeyFile));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_AssemblyOriginatorKeyFile_DelaySign_False()
    {
        CreateCSharpFilesWith("DelaySign", "false");
        await AssertCSCompilationOptionsAsync(null, options => options.DelaySign);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_AssemblyOriginatorKeyFile_DelaySign_True()
    {
        CreateCSharpFilesWith("DelaySign", "true");
        await AssertCSCompilationOptionsAsync(true, options => options.DelaySign);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_CheckOverflow_True()
    {
        CreateCSharpFilesWith("CheckForOverflowUnderflow", "true");
        await AssertCSCompilationOptionsAsync(true, options => options.CheckOverflow);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_CSharp_CheckOverflow_False()
    {
        CreateCSharpFilesWith("CheckForOverflowUnderflow", "false");
        await AssertCSCompilationOptionsAsync(false, options => options.CheckOverflow);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestParseOptions_CSharp_Compatibility_ECMA1()
    {
        CreateCSharpFilesWith("LangVersion", "ISO-1");
        await AssertCSParseOptionsAsync(CS.LanguageVersion.CSharp1, options => options.LanguageVersion);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestParseOptions_CSharp_Compatibility_ECMA2()
    {
        CreateCSharpFilesWith("LangVersion", "ISO-2");
        await AssertCSParseOptionsAsync(CS.LanguageVersion.CSharp2, options => options.LanguageVersion);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestParseOptions_CSharp_Compatibility_None()
    {
        CreateCSharpFilesWith("LangVersion", "3");
        await AssertCSParseOptionsAsync(CS.LanguageVersion.CSharp3, options => options.LanguageVersion);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestParseOptions_CSharp_LanguageVersion_Default()
    {
        CreateCSharpFiles();
        await AssertCSParseOptionsAsync(CS.LanguageVersion.CSharp7_3.MapSpecifiedToEffectiveVersion(), options => options.LanguageVersion);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestParseOptions_CSharp_PreprocessorSymbols()
    {
        CreateCSharpFilesWith("DefineConstants", "DEBUG;TRACE;X;Y");
        await AssertCSParseOptionsAsync("DEBUG,TRACE,X,Y", options => string.Join(",", options.PreprocessorSymbolNames));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestConfigurationDebug()
    {
        CreateCSharpFiles();
        await AssertCSParseOptionsAsync("DEBUG,TRACE", options => string.Join(",", options.PreprocessorSymbolNames));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestConfigurationRelease()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(("Configuration", "Release"));
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = sol.Projects.First();
        var options = project.ParseOptions;

        Assert.DoesNotContain("DEBUG", options.PreprocessorSymbolNames);
        Assert.Contains("TRACE", options.PreprocessorSymbolNames);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_DebugType_Full()
    {
        CreateVBFilesWith("DebugType", "full");
        await AssertVBParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_DebugType_None()
    {
        CreateVBFilesWith("DebugType", "none");
        await AssertVBParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_DebugType_PDBOnly()
    {
        CreateVBFilesWith("DebugType", "pdbonly");
        await AssertVBParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_DebugType_Portable()
    {
        CreateVBFilesWith("DebugType", "portable");
        await AssertVBParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_DebugType_Embedded()
    {
        CreateVBFilesWith("DebugType", "embedded");
        await AssertVBParseOptionsAsync(0, options => options.Errors.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_VBRuntime_Embed()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.Embed));
        await AssertVBCompilationOptionsAsync(true, options => options.EmbedVbCoreRuntime);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OutputKind_DynamicallyLinkedLibrary()
    {
        CreateVBFilesWith("OutputType", "Library");
        await AssertVBCompilationOptionsAsync(OutputKind.DynamicallyLinkedLibrary, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OutputKind_ConsoleApplication()
    {
        CreateVBFilesWith("OutputType", "Exe");
        await AssertVBCompilationOptionsAsync(OutputKind.ConsoleApplication, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OutputKind_WindowsApplication()
    {
        CreateVBFilesWith("OutputType", "WinExe");
        await AssertVBCompilationOptionsAsync(OutputKind.WindowsApplication, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OutputKind_NetModule()
    {
        CreateVBFilesWith("OutputType", "Module");
        await AssertVBCompilationOptionsAsync(OutputKind.NetModule, options => options.OutputKind);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_RootNamespace()
    {
        CreateVBFilesWith("RootNamespace", "Foo.Bar");
        await AssertVBCompilationOptionsAsync("Foo.Bar", options => options.RootNamespace);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionStrict_On()
    {
        CreateVBFilesWith("OptionStrict", "On");
        await AssertVBCompilationOptionsAsync(VB.OptionStrict.On, options => options.OptionStrict);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionStrict_Off()
    {
        CreateVBFilesWith("OptionStrict", "Off");

        // The VBC MSBuild task specifies '/optionstrict:custom' rather than '/optionstrict-'
        // See https://github.com/dotnet/roslyn/blob/58f44c39048032c6b823ddeedddd20fa589912f5/src/Compilers/Core/MSBuildTask/Vbc.cs#L390-L418 for details.

        await AssertVBCompilationOptionsAsync(VB.OptionStrict.Custom, options => options.OptionStrict);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionStrict_Custom()
    {
        CreateVBFilesWith("OptionStrictType", "Custom");
        await AssertVBCompilationOptionsAsync(VB.OptionStrict.Custom, options => options.OptionStrict);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionInfer_True()
    {
        CreateVBFilesWith("OptionInfer", "On");
        await AssertVBCompilationOptionsAsync(true, options => options.OptionInfer);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionInfer_False()
    {
        CreateVBFilesWith("OptionInfer", "Off");
        await AssertVBCompilationOptionsAsync(false, options => options.OptionInfer);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionExplicit_True()
    {
        CreateVBFilesWith("OptionExplicit", "On");
        await AssertVBCompilationOptionsAsync(true, options => options.OptionExplicit);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionExplicit_False()
    {
        CreateVBFilesWith("OptionExplicit", "Off");
        await AssertVBCompilationOptionsAsync(false, options => options.OptionExplicit);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionCompareText_True()
    {
        CreateVBFilesWith("OptionCompare", "Text");
        await AssertVBCompilationOptionsAsync(true, options => options.OptionCompareText);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionCompareText_False()
    {
        CreateVBFilesWith("OptionCompare", "Binary");
        await AssertVBCompilationOptionsAsync(false, options => options.OptionCompareText);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionRemoveIntegerOverflowChecks_True()
    {
        CreateVBFilesWith("RemoveIntegerChecks", "true");
        await AssertVBCompilationOptionsAsync(false, options => options.CheckOverflow);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionRemoveIntegerOverflowChecks_False()
    {
        CreateVBFilesWith("RemoveIntegerChecks", "false");
        await AssertVBCompilationOptionsAsync(true, options => options.CheckOverflow);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_OptionAssemblyOriginatorKeyFile_SignAssemblyFalse()
    {
        CreateVBFilesWith("SignAssembly", "false");
        await AssertVBCompilationOptionsAsync(null, options => options.CryptoKeyFile);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestCompilationOptions_VisualBasic_GlobalImports()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.GetProjectsByName("VisualBasicProject").FirstOrDefault();
        var options = (VB.VisualBasicCompilationOptions)project.CompilationOptions;
        var imports = options.GlobalImports;

        AssertEx.Equal(
            expected:
            [
                "Microsoft.VisualBasic",
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Diagnostics",
                "System.Linq",
            ],
            actual: imports.Select(i => i.Name));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestParseOptions_VisualBasic_PreprocessorSymbols()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "DefineConstants", "X=1,Y=2,Z,T=-1,VBC_VER=123,F=false"));
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.GetProjectsByName("VisualBasicProject").FirstOrDefault();
        var options = (VB.VisualBasicParseOptions)project.ParseOptions;
        var defines = new List<KeyValuePair<string, object>>(options.PreprocessorSymbols);
        defines.Sort((x, y) => x.Key.CompareTo(y.Key));

        AssertEx.Equal(
            expected:
            [
                new KeyValuePair<string, object>("_MyType", "Windows"),
                new KeyValuePair<string, object>("CONFIG", "Debug"),
                new KeyValuePair<string, object>("DEBUG", -1),
                new KeyValuePair<string, object>("F", false),
                new KeyValuePair<string, object>("PLATFORM", "AnyCPU"),
                new KeyValuePair<string, object>("T", -1),
                new KeyValuePair<string, object>("TARGET", "library"),
                new KeyValuePair<string, object>("TRACE", -1),
                new KeyValuePair<string, object>("VBC_VER", 123),
                new KeyValuePair<string, object>("X", 1),
                new KeyValuePair<string, object>("Y", 2),
                new KeyValuePair<string, object>("Z", true),
            ],
            actual: defines);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task Test_VisualBasic_ConditionalAttributeEmitted()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicClass.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass_WithConditionalAttributes)
            .ReplaceFileElement(@"VisualBasicProject\VisualBasicProject.vbproj", "DefineConstants", "EnableMyAttribute"));
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
        var options = (VB.VisualBasicParseOptions)project.ParseOptions;
        Assert.True(options.PreprocessorSymbolNames.Contains("EnableMyAttribute"));

        var compilation = await project.GetCompilationAsync();
        var metadataBytes = compilation.EmitToArray();
        var mtref = MetadataReference.CreateFromImage(metadataBytes);
        var mtcomp = CS.CSharpCompilation.Create("MT", references: [mtref]);
        var sym = (IAssemblySymbol)mtcomp.GetAssemblyOrModuleSymbol(mtref);
        var attrs = sym.GetAttributes();

        Assert.Contains(attrs, ad => ad.AttributeClass.Name == "MyAttribute");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task Test_VisualBasic_ConditionalAttributeNotEmitted()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"VisualBasicProject\VisualBasicClass.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass_WithConditionalAttributes));
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
        var options = (VB.VisualBasicParseOptions)project.ParseOptions;
        Assert.False(options.PreprocessorSymbolNames.Contains("EnableMyAttribute"));

        var compilation = await project.GetCompilationAsync();
        var metadataBytes = compilation.EmitToArray();
        var mtref = MetadataReference.CreateFromImage(metadataBytes);
        var mtcomp = CS.CSharpCompilation.Create("MT", references: [mtref]);
        var sym = (IAssemblySymbol)mtcomp.GetAssemblyOrModuleSymbol(mtref);
        var attrs = sym.GetAttributes();

        Assert.DoesNotContain(attrs, ad => ad.AttributeClass.Name == "MyAttribute");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task Test_CSharp_ConditionalAttributeEmitted()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass_WithConditionalAttributes)
            .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", "DefineConstants", "EnableMyAttribute"));
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = sol.GetProjectsByName("CSharpProject").FirstOrDefault();
        var options = project.ParseOptions;
        Assert.Contains("EnableMyAttribute", options.PreprocessorSymbolNames);

        var compilation = await project.GetCompilationAsync();
        var metadataBytes = compilation.EmitToArray();
        var mtref = MetadataReference.CreateFromImage(metadataBytes);
        var mtcomp = CS.CSharpCompilation.Create("MT", references: [mtref]);
        var sym = (IAssemblySymbol)mtcomp.GetAssemblyOrModuleSymbol(mtref);
        var attrs = sym.GetAttributes();

        Assert.Contains(attrs, ad => ad.AttributeClass.Name == "MyAttr");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task Test_CSharp_ConditionalAttributeNotEmitted()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass_WithConditionalAttributes));
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = sol.GetProjectsByName("CSharpProject").FirstOrDefault();
        var options = project.ParseOptions;
        Assert.DoesNotContain("EnableMyAttribute", options.PreprocessorSymbolNames);

        var compilation = await project.GetCompilationAsync();
        var metadataBytes = compilation.EmitToArray();
        var mtref = MetadataReference.CreateFromImage(metadataBytes);
        var mtcomp = CS.CSharpCompilation.Create("MT", references: [mtref]);
        var sym = (IAssemblySymbol)mtcomp.GetAssemblyOrModuleSymbol(mtref);
        var attrs = sym.GetAttributes();

        Assert.DoesNotContain(attrs, ad => ad.AttributeClass.Name == "MyAttr");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_CSharp_WithLinkedDocument()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithLink)
            .WithFile(@"OtherStuff\Foo.cs", Resources.SourceFiles.CSharp.OtherStuff_Foo));

        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.GetProjectsByName("CSharpProject").FirstOrDefault();
        var documents = project.Documents.ToList();
        var fooDoc = documents.Single(d => d.Name == "Foo.cs");
        var folder = Assert.Single(fooDoc.Folders);
        Assert.Equal("Blah", folder);

        // prove that the file path is the correct full path to the actual file
        Assert.Contains("OtherStuff", fooDoc.FilePath);
        Assert.True(File.Exists(fooDoc.FilePath));
        var text = File.ReadAllText(fooDoc.FilePath);
        Assert.Equal(Resources.SourceFiles.CSharp.OtherStuff_Foo, text);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestAddDocumentAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.GetProjectsByName("CSharpProject").FirstOrDefault();

        var newText = SourceText.From("public class Bar { }");
        workspace.AddDocument(project.Id, ["NewFolder"], "Bar.cs", newText);

        // check workspace current solution
        var solution2 = workspace.CurrentSolution;
        var project2 = solution2.GetProjectsByName("CSharpProject").FirstOrDefault();
        var documents = project2.Documents.ToList();
        Assert.Equal(4, documents.Count);
        var document2 = documents.Single(d => d.Name == "Bar.cs");
        var text2 = await document2.GetTextAsync();
        Assert.Equal(newText.ToString(), text2.ToString());
        Assert.Single(document2.Folders);

        // check actual file on disk...
        var textOnDisk = File.ReadAllText(document2.FilePath);
        Assert.Equal(newText.ToString(), textOnDisk);

        // check project file on disk
        var projectFileText = File.ReadAllText(project2.FilePath);
        Assert.Contains(@"NewFolder\Bar.cs", projectFileText);

        // reload project & solution to prove project file change was good
        using var workspaceB = CreateMSBuildWorkspace();
        var solutionB = await workspaceB.OpenSolutionAsync(solutionFilePath);
        var projectB = workspaceB.CurrentSolution.GetProjectsByName("CSharpProject").FirstOrDefault();
        var documentsB = projectB.Documents.ToList();
        Assert.Equal(4, documentsB.Count);
        var documentB = documentsB.Single(d => d.Name == "Bar.cs");
        Assert.Single(documentB.Folders);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestUpdateDocumentAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.GetProjectsByName("CSharpProject").FirstOrDefault();
        var document = project.Documents.Single(d => d.Name == "CSharpClass.cs");
        var originalText = await document.GetTextAsync();

        var newText = SourceText.From("public class Bar { }");
        workspace.TryApplyChanges(solution.WithDocumentText(document.Id, newText, PreservationMode.PreserveIdentity));

        // check workspace current solution
        var solution2 = workspace.CurrentSolution;
        var project2 = solution2.GetProjectsByName("CSharpProject").FirstOrDefault();
        var documents = project2.Documents.ToList();
        Assert.Equal(3, documents.Count);
        var document2 = documents.Single(d => d.Name == "CSharpClass.cs");
        var text2 = await document2.GetTextAsync();
        Assert.Equal(newText.ToString(), text2.ToString());

        // check actual file on disk...
        var textOnDisk = File.ReadAllText(document2.FilePath);
        Assert.Equal(newText.ToString(), textOnDisk);

        // check original text in original solution did not change
        var text = await document.GetTextAsync();

        Assert.Equal(originalText.ToString(), text.ToString());
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestRemoveDocumentAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.GetProjectsByName("CSharpProject").FirstOrDefault();
        var document = project.Documents.Single(d => d.Name == "CSharpClass.cs");
        var originalText = await document.GetTextAsync();

        workspace.RemoveDocument(document.Id);

        // check workspace current solution
        var solution2 = workspace.CurrentSolution;
        var project2 = solution2.GetProjectsByName("CSharpProject").FirstOrDefault();
        Assert.DoesNotContain(project2.Documents, d => d.Name == "CSharpClass.cs");

        // check actual file on disk...
        Assert.False(File.Exists(document.FilePath));

        // check original text in original solution did not change
        var text = await document.GetTextAsync();
        Assert.Equal(originalText.ToString(), text.ToString());
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestApplyChanges_UpdateDocumentText()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var documents = solution.GetProjectsByName("CSharpProject").FirstOrDefault().Documents.ToList();
        var document = documents.Single(d => d.Name.Contains("CSharpClass"));
        var text = await document.GetTextAsync();
        var newText = SourceText.From("using System.Diagnostics;\r\n" + text.ToString());
        var newSolution = solution.WithDocumentText(document.Id, newText);

        workspace.TryApplyChanges(newSolution);

        // check workspace current solution
        var solution2 = workspace.CurrentSolution;
        var document2 = solution2.GetDocument(document.Id);
        var text2 = await document2.GetTextAsync();
        Assert.Equal(newText.ToString(), text2.ToString());

        // check actual file on disk...
        var textOnDisk = File.ReadAllText(document.FilePath);
        Assert.Equal(newText.ToString(), textOnDisk);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestApplyChanges_UpdateAdditionalDocumentText()
    {
        CreateFiles(GetSimpleCSharpSolutionWithAdditionaFile());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");
        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        var documents = solution.GetProjectsByName("CSharpProject").FirstOrDefault().AdditionalDocuments.ToList();
        var document = documents.Single(d => d.Name.Contains("ValidAdditionalFile"));
        var text = await document.GetTextAsync();
        var newText = SourceText.From("New Text In Additional File.\r\n" + text.ToString());
        var newSolution = solution.WithAdditionalDocumentText(document.Id, newText);

        workspace.TryApplyChanges(newSolution);

        // check workspace current solution
        var solution2 = workspace.CurrentSolution;
        var document2 = solution2.GetAdditionalDocument(document.Id);
        var text2 = await document2.GetTextAsync();
        Assert.Equal(newText.ToString(), text2.ToString());

        // check actual file on disk...
        var textOnDisk = File.ReadAllText(document.FilePath);
        Assert.Equal(newText.ToString(), textOnDisk);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestApplyChanges_AddDocument()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.GetProjectsByName("CSharpProject").FirstOrDefault();
        var newDocId = DocumentId.CreateNewId(project.Id);
        var newText = SourceText.From("public class Bar { }");
        var newSolution = solution.AddDocument(newDocId, "Bar.cs", newText);

        workspace.TryApplyChanges(newSolution);

        // check workspace current solution
        var solution2 = workspace.CurrentSolution;
        var document2 = solution2.GetDocument(newDocId);
        var text2 = await document2.GetTextAsync();
        Assert.Equal(newText.ToString(), text2.ToString());

        // check actual file on disk...
        var textOnDisk = File.ReadAllText(document2.FilePath);
        Assert.Equal(newText.ToString(), textOnDisk);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestApplyChanges_NotSupportedChangesFail()
    {
        var csharpProjPath = @"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj";
        var vbProjPath = @"AnalyzerSolution\VisualBasicProject_AnalyzerReference.vbproj";
        CreateFiles(GetAnalyzerReferenceSolutionFiles());

        using var workspace = CreateMSBuildWorkspace();
        var csProjectFilePath = GetSolutionFileName(csharpProjPath);
        var csProject = await workspace.OpenProjectAsync(csProjectFilePath);
        var csProjectId = csProject.Id;

        var vbProjectFilePath = GetSolutionFileName(vbProjPath);
        var vbProject = await workspace.OpenProjectAsync(vbProjectFilePath);
        var vbProjectId = vbProject.Id;

        // adding additional documents not supported.
        Assert.False(workspace.CanApplyChange(ApplyChangesKind.AddAdditionalDocument));
        Assert.Throws<NotSupportedException>(delegate
        {
            workspace.TryApplyChanges(workspace.CurrentSolution.AddAdditionalDocument(DocumentId.CreateNewId(csProjectId), "foo.xaml", SourceText.From("<foo></foo>")));
        });

        var xaml = workspace.CurrentSolution.GetProject(csProjectId).AdditionalDocuments.FirstOrDefault(d => d.Name == "XamlFile.xaml");
        Assert.NotNull(xaml);

        // removing additional documents not supported
        Assert.False(workspace.CanApplyChange(ApplyChangesKind.RemoveAdditionalDocument));
        Assert.Throws<NotSupportedException>(delegate
        {
            workspace.TryApplyChanges(workspace.CurrentSolution.RemoveAdditionalDocument(xaml.Id));
        });
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestWorkspaceChangedEvent()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        await workspace.OpenSolutionAsync(solutionFilePath);
        var expectedEventKind = WorkspaceChangeKind.DocumentChanged;
        var originalSolution = workspace.CurrentSolution;

        using var eventWaiter = workspace.VerifyWorkspaceChangedEvent(args =>
        {
            Assert.Equal(expectedEventKind, args.Kind);
            Assert.NotNull(args.NewSolution);
            Assert.NotSame(originalSolution, args.NewSolution);
        });
        // change document text (should fire SolutionChanged event)
        var doc = workspace.CurrentSolution.Projects.First().Documents.First();
        var text = await doc.GetTextAsync();
        var newText = "/* new text */\r\n" + text.ToString();

        workspace.TryApplyChanges(workspace.CurrentSolution.WithDocumentText(doc.Id, SourceText.From(newText), PreservationMode.PreserveIdentity));

        Assert.True(eventWaiter.WaitForEventToFire(AsyncEventTimeout),
            string.Format("event {0} was not fired within {1}",
            Enum.GetName(typeof(WorkspaceChangeKind), expectedEventKind),
            AsyncEventTimeout));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestWorkspaceChangedWeakEvent()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        await workspace.OpenSolutionAsync(solutionFilePath);
        var expectedEventKind = WorkspaceChangeKind.DocumentChanged;
        var originalSolution = workspace.CurrentSolution;

        using var eventWanter = workspace.VerifyWorkspaceChangedEvent(args =>
        {
            Assert.Equal(expectedEventKind, args.Kind);
            Assert.NotNull(args.NewSolution);
            Assert.NotSame(originalSolution, args.NewSolution);
        });
        // change document text (should fire SolutionChanged event)
        var doc = workspace.CurrentSolution.Projects.First().Documents.First();
        var text = await doc.GetTextAsync();
        var newText = "/* new text */\r\n" + text.ToString();

        workspace.TryApplyChanges(
            workspace
            .CurrentSolution
            .WithDocumentText(
                doc.Id,
                SourceText.From(newText),
                PreservationMode.PreserveIdentity));

        Assert.True(eventWanter.WaitForEventToFire(AsyncEventTimeout),
            string.Format("event {0} was not fired within {1}",
            Enum.GetName(typeof(WorkspaceChangeKind), expectedEventKind),
            AsyncEventTimeout));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529276"), WorkItem(12086, "DevDiv_Projects/Roslyn")]
    public async Task TestOpenProject_LoadMetadataForReferenceProjects_NoMetadata()
    {
        var projPath = @"CSharpProject\CSharpProject_ProjectReference.csproj";
        var files = GetProjectReferenceSolutionFiles();

        CreateFiles(files);

        var projectFullPath = GetSolutionFileName(projPath);

        // Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        workspace.LoadMetadataForReferencedProjects = true;

        var proj = await workspace.OpenProjectAsync(projectFullPath);

        // prove that project gets opened instead.
        Assert.Equal(2, workspace.CurrentSolution.Projects.Count());

        // and all is well
        var comp = await proj.GetCompilationAsync();
        var errs = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(errs);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/918072")]
    public async Task TestAnalyzerReferenceLoadStandalone()
    {
        var projPaths = new[] { @"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj", @"AnalyzerSolution\VisualBasicProject_AnalyzerReference.vbproj" };
        var files = GetAnalyzerReferenceSolutionFiles();

        CreateFiles(files);

        using var workspace = CreateMSBuildWorkspace();
        foreach (var projectPath in projPaths)
        {
            var projectFullPath = GetSolutionFileName(projectPath);

            var proj = await workspace.OpenProjectAsync(projectFullPath);
            Assert.Equal(1, proj.AnalyzerReferences.Count);
            var analyzerReference = proj.AnalyzerReferences[0] as AnalyzerFileReference;
            Assert.NotNull(analyzerReference);
            Assert.True(analyzerReference.FullPath.EndsWith("CSharpProject.dll", StringComparison.OrdinalIgnoreCase));
        }

        // prove that project gets opened instead.
        Assert.Equal(2, workspace.CurrentSolution.Projects.Count());
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestAdditionalFilesStandalone()
    {
        var projPaths = new[] { @"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj", @"AnalyzerSolution\VisualBasicProject_AnalyzerReference.vbproj" };
        var files = GetAnalyzerReferenceSolutionFiles();

        CreateFiles(files);

        using var workspace = CreateMSBuildWorkspace();
        foreach (var projectPath in projPaths)
        {
            var projectFullPath = GetSolutionFileName(projectPath);

            var proj = await workspace.OpenProjectAsync(projectFullPath);
            var doc = Assert.Single(proj.AdditionalDocuments);
            Assert.Equal("XamlFile.xaml", doc.Name);
            var text = await doc.GetTextAsync();
            Assert.Contains("Window", text.ToString(), StringComparison.Ordinal);
        }
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestLoadTextSync()
    {
        var files = GetAnalyzerReferenceSolutionFiles();

        CreateFiles(files);

        using var workspace = CreateMSBuildWorkspace();
        var projectFullPath = GetSolutionFileName(@"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj");

        var project = await workspace.OpenProjectAsync(projectFullPath);

        var document = project.Documents.Single(d => d.Name == "CSharpClass.cs");
        var documentText = document.GetTextSynchronously(CancellationToken.None);
        Assert.Contains("public class CSharpClass", documentText.ToString(), StringComparison.Ordinal);

        var additionalDocument = project.AdditionalDocuments.Single(a => a.Name == "XamlFile.xaml");
        var additionalDocumentText = additionalDocument.GetTextSynchronously(CancellationToken.None);
        Assert.Contains("Window", additionalDocumentText.ToString(), StringComparison.Ordinal);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestGetTextSynchronously()
    {
        var files = GetAnalyzerReferenceSolutionFiles();

        CreateFiles(files);

        using var workspace = CreateMSBuildWorkspace();
        var projectFullPath = GetSolutionFileName(@"AnalyzerSolution\CSharpProject_AnalyzerReference.csproj");
        var proj = await workspace.OpenProjectAsync(projectFullPath);

        var doc = proj.Documents.First();
        var text = doc.State.GetTextSynchronously(CancellationToken.None);

        var adoc = proj.AdditionalDocuments.First(a => a.Name == "XamlFile.xaml");
        var atext = adoc.State.GetTextSynchronously(CancellationToken.None);
        Assert.Contains("Window", atext.ToString(), StringComparison.Ordinal);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546171")]
    public async Task TestCSharpExternAlias()
    {
        var projPath = @"CSharpProject\CSharpProject_ExternAlias.csproj";
        var files = new FileSet(
            (projPath, Resources.ProjectFiles.CSharp.ExternAlias),
            (@"CSharpProject\CSharpExternAlias.cs", Resources.SourceFiles.CSharp.CSharpExternAlias));

        CreateFiles(files);

        var fullPath = GetSolutionFileName(projPath);
        using var workspace = CreateMSBuildWorkspace();
        var proj = await workspace.OpenProjectAsync(fullPath);
        var comp = await proj.GetCompilationAsync();
        comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530337")]
    public async Task TestProjectReferenceWithExternAlias()
    {
        var files = GetProjectReferenceSolutionFiles();
        CreateFiles(files);

        var fullPath = GetSolutionFileName(@"CSharpProjectReference.sln");

        // Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var sol = await workspace.OpenSolutionAsync(fullPath);
        var proj = sol.Projects.First();
        var comp = await proj.GetCompilationAsync();
        comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestProjectReferenceWithReferenceOutputAssemblyFalse_SolutionRoot()
    {
        var files = GetProjectReferenceSolutionFiles();
        files = VisitProjectReferences(
            files,
            r => r.Add(new XElement(XName.Get("ReferenceOutputAssembly", MSBuildNamespace), "false")));

        CreateFiles(files);

        var fullPath = GetSolutionFileName(@"CSharpProjectReference.sln");

        using var workspace = CreateMSBuildWorkspace();
        var sol = await workspace.OpenSolutionAsync(fullPath);
        foreach (var project in sol.Projects)
        {
            Assert.Empty(project.ProjectReferences);
        }
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestProjectReferenceWithReferenceOutputAssemblyFalse_ProjectRoot()
    {
        var files = GetProjectReferenceSolutionFiles();
        files = VisitProjectReferences(
            files,
            r => r.Add(new XElement(XName.Get("ReferenceOutputAssembly", MSBuildNamespace), "false")));

        CreateFiles(files);

        var referencingProjectPath = GetSolutionFileName(@"CSharpProject\CSharpProject_ProjectReference.csproj");
        var referencedProjectPath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(referencingProjectPath);

        Assert.Empty(project.ProjectReferences);

        // Project referenced through ProjectReference with ReferenceOutputAssembly=false
        // should be present in the solution.
        Assert.NotNull(project.Solution.GetProjectsByName("CSharpProject").SingleOrDefault());
    }

    private static FileSet VisitProjectReferences(FileSet files, Action<XElement> visitProjectReference)
    {
        var result = new List<(string, object)>();
        foreach (var (fileName, fileContent) in files)
        {
            var text = fileContent.ToString();
            if (fileName.EndsWith("proj", StringComparison.OrdinalIgnoreCase))
            {
                text = VisitProjectReferences(text, visitProjectReference);
            }

            result.Add((fileName, text));
        }

        return new FileSet([.. result]);
    }

    private static string VisitProjectReferences(string projectFileText, Action<XElement> visitProjectReference)
    {
        var document = XDocument.Parse(projectFileText);
        var projectReferenceItems = document.Descendants(XName.Get("ProjectReference", MSBuildNamespace));
        foreach (var projectReferenceItem in projectReferenceItems)
        {
            visitProjectReference(projectReferenceItem);
        }

        return document.ToString();
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestProjectReferenceWithNoGuid()
    {
        var files = GetProjectReferenceSolutionFiles();
        files = VisitProjectReferences(
            files,
            r => r.Elements(XName.Get("Project", MSBuildNamespace)).Remove());

        CreateFiles(files);

        var fullPath = GetSolutionFileName(@"CSharpProjectReference.sln");

        // Workspace failure Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var sol = await workspace.OpenSolutionAsync(fullPath);
        foreach (var project in sol.Projects)
        {
            Assert.InRange(project.ProjectReferences.Count(), 0, 1);
        }
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/5668")]
    public async Task TestOpenProject_MetadataReferenceHasDocComments()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        var comp = await project.GetCompilationAsync();
        var symbol = comp.GetTypeByMetadataName("System.Console");
        var docComment = symbol.GetDocumentationCommentXml();
        Assert.NotNull(docComment);
        Assert.NotEmpty(docComment);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_CSharp_HasSourceDocComments()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
        var parseOptions = (CS.CSharpParseOptions)project.ParseOptions;
        Assert.Equal(DocumentationMode.Parse, parseOptions.DocumentationMode);
        var comp = await project.GetCompilationAsync();
        var symbol = comp.GetTypeByMetadataName("CSharpProject.CSharpClass");
        var docComment = symbol.GetDocumentationCommentXml();
        Assert.NotNull(docComment);
        Assert.NotEmpty(docComment);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_VisualBasic_HasSourceDocComments()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First(p => p.Language == LanguageNames.VisualBasic);
        var parseOptions = (VB.VisualBasicParseOptions)project.ParseOptions;
        Assert.Equal(DocumentationMode.Diagnose, parseOptions.DocumentationMode);
        var comp = await project.GetCompilationAsync();
        var symbol = comp.GetTypeByMetadataName("VisualBasicProject.VisualBasicClass");
        var docComment = symbol.GetDocumentationCommentXml();
        Assert.NotNull(docComment);
        Assert.NotEmpty(docComment);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_CrossLanguageSkeletonReferenceHasDocComments()
    {
        CreateFiles(GetMultiProjectSolutionFiles());
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var csproject = workspace.CurrentSolution.Projects.First(p => p.Language == LanguageNames.CSharp);
        var csoptions = (CS.CSharpParseOptions)csproject.ParseOptions;
        Assert.Equal(DocumentationMode.Parse, csoptions.DocumentationMode);
        var cscomp = await csproject.GetCompilationAsync();
        var cssymbol = cscomp.GetTypeByMetadataName("CSharpProject.CSharpClass");
        var cscomment = cssymbol.GetDocumentationCommentXml();
        Assert.NotNull(cscomment);

        var vbproject = workspace.CurrentSolution.Projects.First(p => p.Language == LanguageNames.VisualBasic);
        var vboptions = (VB.VisualBasicParseOptions)vbproject.ParseOptions;
        Assert.Equal(DocumentationMode.Diagnose, vboptions.DocumentationMode);
        var vbcomp = await vbproject.GetCompilationAsync();
        var vbsymbol = vbcomp.GetTypeByMetadataName("VisualBasicProject.VisualBasicClass");
        var parent = vbsymbol.BaseType; // this is the vb imported version of the csharp symbol
        var vbcomment = parent.GetDocumentationCommentXml();

        Assert.Equal(cscomment, vbcomment);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(IsEnglishLocal))]
    public async Task TestOpenProject_WithProjectFileLockedAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());

        // open for read-write so no-one else can read
        var projectFile = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
        using (File.Open(projectFile, FileMode.Open, FileAccess.ReadWrite))
        {
            using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
            await workspace.OpenProjectAsync(projectFile);
            var diagnostic = Assert.Single(workspace.Diagnostics);
            Assert.Contains("The process cannot access the file", diagnostic.Message);
        }
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WithNonExistentProjectFileAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());

        // open for read-write so no-one else can read
        var projectFile = GetSolutionFileName(@"CSharpProject\NoProject.csproj");
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                using var workspace = CreateMSBuildWorkspace();
                await workspace.OpenProjectAsync(projectFile);
            });
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_WithNonExistentSolutionFileAsync()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());

        // open for read-write so no-one else can read
        var solutionFile = GetSolutionFileName(@"NoSolution.sln");
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                using var workspace = CreateMSBuildWorkspace();
                await workspace.OpenSolutionAsync(solutionFile);
            });
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_SolutionFileHasEmptyLinesAndWhitespaceOnlyLines()
    {
        var files = new FileSet(
            (@"TestSolution.sln", Resources.SolutionFiles.CSharp_EmptyLines),
            (@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.CSharpProject),
            (@"CSharpProject\CSharpClass.cs", Resources.SourceFiles.CSharp.CSharpClass),
            (@"CSharpProject\Properties\AssemblyInfo.cs", Resources.SourceFiles.CSharp.AssemblyInfo));

        CreateFiles(files);
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531543")]
    public async Task TestOpenSolution_SolutionFileHasEmptyLineBetweenProjectBlock()
    {
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");
        var files = GetSimpleCSharpProjectFiles()
            .Concat(GetSimpleVisualBasicProjectFiles())
            .Concat([(solutionFilePath, Resources.SolutionFiles.EmptyLineBetweenProjectBlock)]);

        CreateFiles(files);

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), AlwaysSkip = "MSBuild parsing API throws InvalidProjectFileException")]
    [WorkItem(531283, "DevDiv")]
    public async Task TestOpenSolution_SolutionFileHasMissingEndProject()
    {
        var files = new FileSet(
            (@"TestSolution1.sln", Resources.SolutionFiles.MissingEndProject1),
            (@"TestSolution2.sln", Resources.SolutionFiles.MissingEndProject2),
            (@"TestSolution3.sln", Resources.SolutionFiles.MissingEndProject3));

        CreateFiles(files);

        using (var workspace = CreateMSBuildWorkspace())
        {
            var solutionFilePath = GetSolutionFileName("TestSolution1.sln");
            var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        }

        using (var workspace = CreateMSBuildWorkspace())
        {
            var solutionFilePath = GetSolutionFileName("TestSolution2.sln");
            var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        }

        using (var workspace = CreateMSBuildWorkspace())
        {
            var solutionFilePath = GetSolutionFileName("TestSolution3.sln");
            var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        }
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), AlwaysSkip = "https://github.com/microsoft/vs-solutionpersistence/issues/95")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792912")]
    public async Task TestOpenSolution_WithDuplicatedGuidsBecomeSelfReferential()
    {
        var files = new FileSet(
            (@"DuplicatedGuids.sln", Resources.SolutionFiles.DuplicatedGuidsBecomeSelfReferential),
            (@"ReferenceTest\ReferenceTest.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidsBecomeSelfReferential),
            (@"Library1\Library1.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidLibrary1));

        CreateFiles(files);
        var solutionFilePath = GetSolutionFileName("DuplicatedGuids.sln");

        // Workspace failure Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        Assert.Equal(2, solution.ProjectIds.Count);

        var testProject = solution.Projects.FirstOrDefault(p => p.Name == "ReferenceTest");
        Assert.NotNull(testProject);
        Assert.Single(testProject.AllProjectReferences);

        var libraryProject = solution.Projects.FirstOrDefault(p => p.Name == "Library1");
        Assert.NotNull(libraryProject);
        Assert.Empty(libraryProject.AllProjectReferences);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), AlwaysSkip = "https://github.com/microsoft/vs-solutionpersistence/issues/95")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792912")]
    public async Task TestOpenSolution_WithDuplicatedGuidsBecomeCircularReferential()
    {
        var files = new FileSet(
            (@"DuplicatedGuids.sln", Resources.SolutionFiles.DuplicatedGuidsBecomeCircularReferential),
            (@"ReferenceTest\ReferenceTest.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidsBecomeCircularReferential),
            (@"Library1\Library1.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidLibrary3),
            (@"Library2\Library2.csproj", Resources.ProjectFiles.CSharp.DuplicatedGuidLibrary4));

        CreateFiles(files);
        var solutionFilePath = GetSolutionFileName("DuplicatedGuids.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        Assert.Equal(3, solution.ProjectIds.Count);

        var testProject = solution.Projects.FirstOrDefault(p => p.Name == "ReferenceTest");
        Assert.NotNull(testProject);
        Assert.Single(testProject.AllProjectReferences);

        var library1Project = solution.Projects.FirstOrDefault(p => p.Name == "Library1");
        Assert.NotNull(library1Project);
        Assert.Single(library1Project.AllProjectReferences);

        var library2Project = solution.Projects.FirstOrDefault(p => p.Name == "Library2");
        Assert.NotNull(library2Project);
        Assert.Empty(library2Project.AllProjectReferences);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_CSharp_WithMissingDebugType()
    {
        CreateFiles(new FileSet(
            (@"ProjectLoadErrorOnMissingDebugType.sln", Resources.SolutionFiles.ProjectLoadErrorOnMissingDebugType),
            (@"ProjectLoadErrorOnMissingDebugType\ProjectLoadErrorOnMissingDebugType.csproj", Resources.ProjectFiles.CSharp.ProjectLoadErrorOnMissingDebugType)));
        var solutionFilePath = GetSolutionFileName(@"ProjectLoadErrorOnMissingDebugType.sln");

        using var workspace = CreateMSBuildWorkspace();
        await workspace.OpenSolutionAsync(solutionFilePath);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991528")]
    public async Task MSBuildProjectShouldHandleInvalidCodePageProperty()
    {
        var files = new FileSet(
            ("Encoding.csproj", Resources.ProjectFiles.CSharp.Encoding.Replace("<CodePage>ReplaceMe</CodePage>", "<CodePage>-1</CodePage>")),
            ("class1.cs", "//\u201C"));

        CreateFiles(files);

        var projPath = GetSolutionFileName("Encoding.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projPath);
        var document = project.Documents.First(d => d.Name == "class1.cs");
        var text = await document.GetTextAsync();
        Assert.Equal(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), text.Encoding);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991528")]
    public async Task MSBuildProjectShouldHandleInvalidCodePageProperty2()
    {
        var files = new FileSet(
            ("Encoding.csproj", Resources.ProjectFiles.CSharp.Encoding.Replace("<CodePage>ReplaceMe</CodePage>", "<CodePage>Broken</CodePage>")),
            ("class1.cs", "//\u201C"));

        CreateFiles(files);

        var projPath = GetSolutionFileName("Encoding.csproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projPath);
        var document = project.Documents.First(d => d.Name == "class1.cs");
        var text = await document.GetTextAsync();
        Assert.Equal(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), text.Encoding);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991528")]
    public async Task MSBuildProjectShouldHandleDefaultCodePageProperty()
    {
        var files = new FileSet(
            ("Encoding.csproj", Resources.ProjectFiles.CSharp.Encoding.Replace("<CodePage>ReplaceMe</CodePage>", string.Empty)),
            ("class1.cs", "//\u201C"));

        CreateFiles(files);

        var projPath = GetSolutionFileName("Encoding.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projPath);
        var document = project.Documents.First(d => d.Name == "class1.cs");
        var text = await document.GetTextAsync();
        Assert.Equal(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), text.Encoding);
        Assert.Equal("//\u201C", text.ToString());
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981208")]
    public void DisposeMSBuildWorkspaceAndServicesCollected()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());

        var sol = ObjectReference.CreateFromFactory(() => MSBuildWorkspace.Create().OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result);
        var workspace = sol.GetObjectReference(static s => s.Workspace);
        var project = sol.GetObjectReference(static s => s.Projects.First());
        var document = project.GetObjectReference(static p => p.Documents.First());
        var tree = document.UseReference(static d => d.GetSyntaxTreeAsync().Result);
        var type = tree.GetRoot().DescendantTokens().First(t => t.ToString() == "class").Parent;
        Assert.NotNull(type);
        Assert.StartsWith("public class CSharpClass", type.ToString(), StringComparison.Ordinal);

        var compilation = document.GetObjectReference(static d => d.GetSemanticModelAsync(CancellationToken.None).Result);
        Assert.NotNull(compilation);

        document.ReleaseStrongReference();
        project.ReleaseStrongReference();
        workspace.UseReference(static w => w.Dispose());

        compilation.AssertReleased();
        sol.AssertReleased();
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1088127")]
    public async Task MSBuildWorkspacePreservesEncoding()
    {
        var encoding = Encoding.BigEndianUnicode;
        var fileContent = """
            //“
            class C { }
            """;
        var files = new FileSet(
            ("Encoding.csproj", Resources.ProjectFiles.CSharp.Encoding.Replace("<CodePage>ReplaceMe</CodePage>", string.Empty)),
            ("class1.cs", encoding.GetBytesWithPreamble(fileContent)));

        CreateFiles(files);
        var projPath = GetSolutionFileName("Encoding.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projPath);

        var document = project.Documents.First(d => d.Name == "class1.cs");

        // update root without first looking at text (no encoding is known)
        var gen = Editing.SyntaxGenerator.GetGenerator(document);
        var doc2 = document.WithSyntaxRoot(gen.CompilationUnit()); // empty CU
        var doc2text = await doc2.GetTextAsync();
        Assert.Null(doc2text.Encoding);
        var doc2tree = await doc2.GetSyntaxTreeAsync();
        Assert.Null(doc2tree.Encoding);
        Assert.Null(doc2tree.GetText().Encoding);

        // observe original text to discover encoding
        var text = await document.GetTextAsync();
        Assert.Equal(encoding.EncodingName, text.Encoding.EncodingName);
        Assert.Equal(fileContent, text.ToString());

        // update root blindly again, after observing encoding, see that encoding is preserved
        // 🐉 Tools rely on encoding preservation; see https://github.com/dotnet/sdk/issues/46780
        var doc3 = document.WithSyntaxRoot(gen.CompilationUnit()); // empty CU
        var doc3text = await doc3.GetTextAsync();
        Assert.Same(text.Encoding, doc3text.Encoding);
        var doc3tree = await doc3.GetSyntaxTreeAsync();
        Assert.Same(text.Encoding, doc3tree.Encoding);
        Assert.Same(text.Encoding, doc3tree.GetText().Encoding);

        // change doc to have no encoding, still succeeds at writing to disk with old encoding
        var root = await document.GetSyntaxRootAsync();
        var noEncodingDoc = document.WithText(SourceText.From(text.ToString(), encoding: null));
        var noEncodingDocText = await noEncodingDoc.GetTextAsync();
        Assert.Null(noEncodingDocText.Encoding);

        // apply changes (this writes the changed document)
        var noEncodingSolution = noEncodingDoc.Project.Solution;
        Assert.True(noEncodingSolution.Workspace.TryApplyChanges(noEncodingSolution));

        // prove the written document still has the same encoding
        var filePath = GetSolutionFileName("Class1.cs");
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var reloadedText = EncodedStringText.Create(stream);
        Assert.Equal(encoding.EncodingName, reloadedText.Encoding.EncodingName);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestAddRemoveAnalyzerReference()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"Analyzers\MyAnalyzer.dll", Resources.Dlls.EmptyLibrary));

        var projFile = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
        var projFileText = File.ReadAllText(projFile);
        Assert.False(projFileText.Contains(@"<Analyzer Include=""..\Analyzers\MyAnalyzer.dll"));

        using var workspace = CreateMSBuildWorkspace();
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First();

        var myAnalyzerPath = GetSolutionFileName(@"Analyzers\MyAnalyzer.dll");
        var aref = new AnalyzerFileReference(myAnalyzerPath, new InMemoryAssemblyLoader());

        // add reference to MyAnalyzer.dll
        workspace.TryApplyChanges(project.AddAnalyzerReference(aref).Solution);
        projFileText = File.ReadAllText(projFile);
        Assert.Contains(@"<Analyzer Include=""..\Analyzers\MyAnalyzer.dll", projFileText);

        // remove reference MyAnalyzer.dll
        workspace.TryApplyChanges(workspace.CurrentSolution.GetProject(project.Id).RemoveAnalyzerReference(aref).Solution);
        projFileText = File.ReadAllText(projFile);
        Assert.DoesNotContain(@"<Analyzer Include=""..\Analyzers\MyAnalyzer.dll", projFileText);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestAddRemoveProjectReference()
    {
        CreateFiles(GetMultiProjectSolutionFiles());

        var projFile = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");
        var projFileText = File.ReadAllText(projFile);
        Assert.True(projFileText.Contains(@"<ProjectReference Include=""..\CSharpProject\CSharpProject.csproj"">"));

        using var workspace = CreateMSBuildWorkspace();
        var solutionFilePath = GetSolutionFileName("TestSolution.sln");
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var project = solution.Projects.First(p => p.Language == LanguageNames.VisualBasic);
        var pref = project.ProjectReferences.First();

        // remove project reference
        workspace.TryApplyChanges(workspace.CurrentSolution.GetProject(project.Id).RemoveProjectReference(pref).Solution);
        Assert.Empty(workspace.CurrentSolution.GetProject(project.Id).ProjectReferences);

        projFileText = File.ReadAllText(projFile);
        Assert.DoesNotContain(@"<ProjectReference Include=""..\CSharpProject\CSharpProject.csproj"">", projFileText);

        // add it back
        workspace.TryApplyChanges(workspace.CurrentSolution.GetProject(project.Id).AddProjectReference(pref).Solution);
        Assert.Single(workspace.CurrentSolution.GetProject(project.Id).ProjectReferences);

        projFileText = File.ReadAllText(projFile);
        Assert.Contains(@"<ProjectReference Include=""..\CSharpProject\CSharpProject.csproj"">", projFileText);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1101040")]
    public async Task TestOpenProject_BadLink()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.BadLink));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var proj = await workspace.OpenProjectAsync(projectFilePath);
        var docs = proj.Documents.ToList();
        Assert.Equal(3, docs.Count);
    }

    [ConditionalFact(typeof(IsEnglishLocal), typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_BadElement()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.BadElement));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var proj = await workspace.OpenProjectAsync(projectFilePath);

        var diagnostic = Assert.Single(workspace.Diagnostics);
        Assert.StartsWith("Msbuild failed", diagnostic.Message);

        Assert.Empty(proj.DocumentIds);
    }

    [ConditionalFact(typeof(IsEnglishLocal), typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_BadTaskImport()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.BadTasks));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var proj = await workspace.OpenProjectAsync(projectFilePath);

        var diagnostic = Assert.Single(workspace.Diagnostics);
        Assert.StartsWith("Msbuild failed", diagnostic.Message);

        Assert.Equal(2, proj.DocumentIds.Count);
    }

    [ConditionalFact(typeof(IsEnglishLocal), typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenSolution_BadTaskImport()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.BadTasks));

        var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        var diagnostic = Assert.Single(workspace.Diagnostics);
        Assert.StartsWith("Msbuild failed", diagnostic.Message);

        var project = Assert.Single(solution.Projects);
        Assert.Equal(2, project.DocumentIds.Count);
    }

    [ConditionalFact(typeof(IsEnglishLocal), typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_MSBuildExecutionError()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.MSBuildExecutionError));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var proj = await workspace.OpenProjectAsync(projectFilePath);

        var diagnostic = Assert.Single(workspace.Diagnostics);
        Assert.StartsWith("Msbuild failed", diagnostic.Message);
    }

    [ConditionalFact(typeof(IsEnglishLocal), typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_MSBuildEvaluationErrorWithExpressionInputs()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.MSBuildEvaluationErrorWithExpressionInputs));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);

        var diagnostic = Assert.Single(workspace.Diagnostics);

        // Assert we get an error about the broken expression so we know this is not some unrelated error making the test pass
        Assert.Contains("[MSBuild]::VersionEquals('', 6.0)", diagnostic.Message);
    }

    [ConditionalFact(typeof(IsEnglishLocal), typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_MSBuildEvaluationErrorWithSyntax()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.MSBuildEvaluationErrorWithSyntax));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var project = await workspace.OpenProjectAsync(projectFilePath);

        var diagnostic = Assert.Single(workspace.Diagnostics);

        // Assert we get an error about the broken expression so we know this is not some unrelated error making the test pass
        Assert.Contains("'$(Configuration)|$(Platform)' == 'Debug|AnyCPU", diagnostic.Message);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_WildcardsWithLink()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.Wildcards));

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var proj = await workspace.OpenProjectAsync(projectFilePath);

        // prove that the file identified with a wildcard and remapped to a computed link is named correctly.
        Assert.Contains(proj.Documents, d => d.Name == "AssemblyInfo.cs");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestOpenProject_CommandLineArgsHaveNoErrors()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles());

        using var workspace = CreateMSBuildWorkspace();

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        await using var buildHostProcessManager = new BuildHostProcessManager(ImmutableDictionary<string, string>.Empty);

        var buildHost = await buildHostProcessManager.GetBuildHostWithFallbackAsync(projectFilePath, CancellationToken.None);
        var projectFile = await buildHost.LoadProjectFileAsync(projectFilePath, LanguageNames.CSharp, CancellationToken.None);
        var projectFileInfo = (await projectFile.GetProjectFileInfosAsync(CancellationToken.None)).Single();

        var commandLineParser = workspace.Services
            .GetLanguageServices(LanguageNames.CSharp)
            .GetRequiredService<ICommandLineParserService>();

        var projectDirectory = Path.GetDirectoryName(projectFilePath);
        var commandLineArgs = commandLineParser.Parse(
            arguments: projectFileInfo.CommandLineArgs,
            baseDirectory: projectDirectory,
            isInteractive: false,
            sdkDirectory: System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());

        Assert.Empty(commandLineArgs.Errors);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/29122")]
    public async Task TestOpenSolution_ProjectReferencesWithUnconventionalOutputPaths()
    {
        CreateFiles(new FileSet(
            (@"TestVB2.sln", Resources.SolutionFiles.Issue29122_Solution),
            (@"Proj1\ClassLibrary1.vbproj", Resources.ProjectFiles.VisualBasic.Issue29122_ClassLibrary1),
            (@"Proj1\Class1.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass),
            (@"Proj1\My Project\Application.Designer.vb", Resources.SourceFiles.VisualBasic.Application_Designer),
            (@"Proj1\My Project\Application.myapp", Resources.SourceFiles.VisualBasic.Application),
            (@"Proj1\My Project\AssemblyInfo.vb", Resources.SourceFiles.VisualBasic.AssemblyInfo),
            (@"Proj1\My Project\Resources.Designer.vb", Resources.SourceFiles.VisualBasic.Resources_Designer),
            (@"Proj1\My Project\Resources.resx", Resources.SourceFiles.VisualBasic.Resources),
            (@"Proj1\My Project\Settings.Designer.vb", Resources.SourceFiles.VisualBasic.Settings_Designer),
            (@"Proj1\My Project\Settings.settings", Resources.SourceFiles.VisualBasic.Settings),
            (@"Proj2\ClassLibrary2.vbproj", Resources.ProjectFiles.VisualBasic.Issue29122_ClassLibrary2),
            (@"Proj2\Class1.vb", Resources.SourceFiles.VisualBasic.VisualBasicClass),
            (@"Proj2\My Project\Application.Designer.vb", Resources.SourceFiles.VisualBasic.Application_Designer),
            (@"Proj2\My Project\Application.myapp", Resources.SourceFiles.VisualBasic.Application),
            (@"Proj2\My Project\AssemblyInfo.vb", Resources.SourceFiles.VisualBasic.AssemblyInfo),
            (@"Proj2\My Project\Resources.Designer.vb", Resources.SourceFiles.VisualBasic.Resources_Designer),
            (@"Proj2\My Project\Resources.resx", Resources.SourceFiles.VisualBasic.Resources),
            (@"Proj2\My Project\Settings.Designer.vb", Resources.SourceFiles.VisualBasic.Settings_Designer),
            (@"Proj2\My Project\Settings.settings", Resources.SourceFiles.VisualBasic.Settings)));

        var solutionFilePath = GetSolutionFileName(@"TestVB2.sln");

        // The reference assemblies for .NETFramework,Version=v3.5 were not found To resolve this, install the Developer Pack
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);

        // Neither project should contain any unresolved metadata references
        foreach (var project in solution.Projects)
        {
            Assert.DoesNotContain(project.MetadataReferences, mr => mr is UnresolvedMetadataReference);
        }
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/29494")]
    public async Task TestOpenProjectAsync_MalformedAdditionalFilePath()
    {
        var files = GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.MallformedAdditionalFilePath)
            .WithFile(@"CSharpProject\ValidAdditionalFile.txt", Resources.SourceFiles.Text.ValidAdditionalFile);

        CreateFiles(files);

        var projectFilePath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projectFilePath);

        // Project should open without an exception being thrown.
        Assert.NotNull(project);

        Assert.Contains(project.AdditionalDocuments, doc => doc.Name == "COM1");
        Assert.Contains(project.AdditionalDocuments, doc => doc.Name == "TEST::");
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/31390")]
    public async Task TestDuplicateProjectAndMetadataReferences()
    {
        var files = GetDuplicateProjectReferenceSolutionFiles();
        CreateFiles(files);

        var fullPath = GetSolutionFileName(@"CSharpProjectReference.sln");

        // Warning:Found project reference without a matching metadata reference
        using var workspace = CreateMSBuildWorkspace(throwOnWorkspaceFailed: false);
        var solution = await workspace.OpenSolutionAsync(fullPath);
        var project = solution.Projects.Single(p => p.FilePath.EndsWith("CSharpProject_ProjectReference.csproj"));

        Assert.Single(project.ProjectReferences);

        AssertEx.Equal(
            ["EmptyLibrary.dll", "System.Core.dll", "mscorlib.dll"],
            project.MetadataReferences.Select(r => Path.GetFileName(((PortableExecutableReference)r).FilePath)).OrderBy(StringComparer.Ordinal));

        var compilation = await project.GetCompilationAsync();

        Assert.Single(compilation.References.OfType<CompilationReference>());
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestEditorConfigDiscovery()
    {
        var files = GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithDiscoverEditorConfigFiles)
            .WithFile(".editorconfig", "root = true");

        CreateFiles(files);

        var expectedEditorConfigPath = SolutionDirectory.CreateOrOpenFile(".editorconfig").Path;

        using var workspace = CreateMSBuildWorkspace();
        var projectFullPath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        var project = await workspace.OpenProjectAsync(projectFullPath);

        // We should have exactly one .editorconfig corresponding to the file we had. We may also
        // have other files if there is a .editorconfig floating around somewhere higher on the disk.
        var analyzerConfigDocument = Assert.Single(project.AnalyzerConfigDocuments, d => d.FilePath == expectedEditorConfigPath);
        Assert.Equal(".editorconfig", analyzerConfigDocument.Name);
        var text = await analyzerConfigDocument.GetTextAsync();
        Assert.Equal("root = true", text.ToString());
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestEditorConfigDiscoveryDisabled()
    {
        var files = GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithDiscoverEditorConfigFiles)
            .ReplaceFileElement(@"CSharpProject\CSharpProject.csproj", "DiscoverEditorConfigFiles", "false")
            .WithFile(".editorconfig", "root = true");

        CreateFiles(files);

        using var workspace = CreateMSBuildWorkspace();
        var projectFullPath = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");

        var project = await workspace.OpenProjectAsync(projectFullPath);
        Assert.Empty(project.AnalyzerConfigDocuments);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestSolutionFilterSupport()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"CSharpSolutionFilter.slnf", Resources.SolutionFilters.CSharp));
        var solutionFilePath = GetSolutionFileName(@"CSharpSolutionFilter.slnf");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var csharpProject = solution.Projects.Single();

        Assert.Equal(LanguageNames.CSharp, csharpProject.Language);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestInvalidSolutionFilterDoesNotLoad()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"InvalidSolutionFilter.slnf", Resources.SolutionFilters.Invalid));
        var solutionFilePath = GetSolutionFileName(@"InvalidSolutionFilter.slnf");

        using var workspace = CreateMSBuildWorkspace();
        var exception = await Assert.ThrowsAsync<Exception>(() => workspace.OpenSolutionAsync(solutionFilePath));

        Assert.Equal(0, workspace.CurrentSolution.ProjectIds.Count);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestValidXmlSolutionSupport()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"CSharpXmlSolution.slnx", Resources.XmlSolutionFiles.CSharp));
        var solutionFilePath = GetSolutionFileName(@"CSharpXmlSolution.slnx");

        using var workspace = CreateMSBuildWorkspace();
        var solution = await workspace.OpenSolutionAsync(solutionFilePath);
        var csharpProject = Assert.Single(solution.Projects);

        Assert.Equal(LanguageNames.CSharp, csharpProject.Language);
        Assert.Equal("CSharpProject.csproj", Path.GetFileName(csharpProject.FilePath));
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task TestInvalidXmlSolutionSupport()
    {
        CreateFiles(GetMultiProjectSolutionFiles()
            .WithFile(@"InvalidXmlSolution.slnx", Resources.XmlSolutionFiles.Invalid));
        var solutionFilePath = GetSolutionFileName(@"InvalidXmlSolution.slnx");

        using var workspace = CreateMSBuildWorkspace();
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => workspace.OpenSolutionAsync(solutionFilePath));

        Assert.Equal(0, workspace.CurrentSolution.ProjectIds.Count);
    }

    // On .NET Core this tests fails with "CodePage Not Found"
    [ConditionalFact(typeof(VisualStudioMSBuildInstalled), typeof(DesktopClrOnly))]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991528")]
    public async Task MSBuildProjectShouldHandleCodePageProperty()
    {
        var files = new FileSet(
            ("Encoding.csproj", Resources.ProjectFiles.CSharp.Encoding.Replace("<CodePage>ReplaceMe</CodePage>", "<CodePage>1254</CodePage>")),
            ("class1.cs", "//\u201C"));

        CreateFiles(files);

        var projPath = GetSolutionFileName("Encoding.csproj");
        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(projPath);
        var document = project.Documents.First(d => d.Name == "class1.cs");
        var text = await document.GetTextAsync();
        Assert.Equal(Encoding.GetEncoding(1254), text.Encoding);

        // The smart quote (“) in class1.cs shows up as "â€œ" in codepage 1254. Do a sanity
        // check here to make sure this file hasn't been corrupted in a way that would
        // impact subsequent asserts.
        Assert.Equal(5, "//\u00E2\u20AC\u0153".Length);
        Assert.Equal("//\u00E2\u20AC\u0153".Length, text.Length);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task MSBuildWorkspaceDocumentsFoldersProperty()
    {
        CreateFiles(GetNetCoreAppFiles()
            .WithFile(@"dir1\dir2\dir3\MyClass.cs", Resources.SourceFiles.CSharp.CSharpClass));

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(GetSolutionFileName("Project.csproj"));
        var document = project.Documents.Single(d => d.Name == "MyClass.cs");
        Assert.Equal(["dir1", "dir2", "dir3"], document.Folders);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task MSBuildWorkspaceLinkedDocumentHasFolders()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithLink)
            .WithFile(@"OtherStuff\Foo.cs", Resources.SourceFiles.CSharp.OtherStuff_Foo));

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(GetSolutionFileName(@"CSharpProject\CSharpProject.csproj"));
        var linkedDocument = project.Documents.Single(d => d.Name == "Foo.cs");
        Assert.Equal(["Blah"], linkedDocument.Folders);
    }

    [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
    public async Task MSBuildWorkspaceWithDocumentInParentFolders()
    {
        CreateFiles(GetSimpleCSharpSolutionFiles()
            .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithClassNotInProjectFolder)
            .WithFile(@"MyDir\MyClass.cs", Resources.SourceFiles.CSharp.CSharpClass));

        using var workspace = CreateMSBuildWorkspace();
        var project = await workspace.OpenProjectAsync(GetSolutionFileName(@"CSharpProject\CSharpProject.csproj"));
        var linkedDocument = project.Documents.Single(d => d.Name == "MyClass.cs");
        Assert.Equal(["..", "MyDir"], linkedDocument.Folders);
    }

    private sealed class InMemoryAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
        {
            var bytes = File.ReadAllBytes(fullPath);
            return Assembly.Load(bytes);
        }
    }
}
