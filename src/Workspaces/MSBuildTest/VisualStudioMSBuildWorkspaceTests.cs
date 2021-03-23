// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class VisualStudioMSBuildWorkspaceTests : MSBuildWorkspaceTestBase
    {
        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        [WorkItem(991528, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991528")]
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

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenProject_WithInvalidFilePath_Fails()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
            var projectFilePath = GetSolutionFileName(@"http://localhost/Invalid/InvalidProject.csproj");

            using var workspace = CreateMSBuildWorkspace();

            AssertEx.Throws<InvalidOperationException>(() => workspace.OpenProjectAsync(projectFilePath).Wait());
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenProject_WithInvalidProjectReference_SkipFalse_Fails()
        {
            CreateFiles(GetMultiProjectSolutionFiles()
                .WithFile(@"VisualBasicProject\VisualBasicProject.vbproj", Resources.ProjectFiles.VisualBasic.InvalidProjectReference));
            var projectFilePath = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");

            using var workspace = CreateMSBuildWorkspace();
            workspace.SkipUnrecognizedProjects = false;

            AssertEx.Throws<InvalidOperationException>(() => workspace.OpenProjectAsync(projectFilePath).Wait());
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenSolution_WithInvalidProjectPath_SkipFalse_Fails()
        {
            // when not skipped we should get an exception for the invalid project

            CreateFiles(GetSimpleCSharpSolutionFiles()
                .WithFile(@"TestSolution.sln", Resources.SolutionFiles.InvalidProjectPath));
            var solutionFilePath = GetSolutionFileName(@"TestSolution.sln");

            using var workspace = CreateMSBuildWorkspace();
            workspace.SkipUnrecognizedProjects = false;

            AssertEx.Throws<InvalidOperationException>(() => workspace.OpenSolutionAsync(solutionFilePath).Wait());
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public void TestOpenSolution_WithInvalidSolutionFile_Fails()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());
            var solutionFilePath = GetSolutionFileName(@"http://localhost/Invalid/InvalidSolution.sln");

            using var workspace = CreateMSBuildWorkspace();

            AssertEx.Throws<InvalidOperationException>(() => workspace.OpenSolutionAsync(solutionFilePath).Wait());
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public async Task TestAddRemoveMetadataReference_GAC()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());

            var projFile = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
            var projFileText = File.ReadAllText(projFile);
            Assert.False(projFileText.Contains(@"System.Xaml"));

            using var workspace = CreateMSBuildWorkspace();
            var solutionFilePath = GetSolutionFileName("TestSolution.sln");
            var solution = await workspace.OpenSolutionAsync(solutionFilePath);
            var project = solution.Projects.First();

            var mref = MetadataReference.CreateFromFile(typeof(System.Xaml.XamlObjectReader).Assembly.Location);

            // add reference to System.Xaml
            workspace.TryApplyChanges(project.AddMetadataReference(mref).Solution);
            projFileText = File.ReadAllText(projFile);
            Assert.Contains(@"<Reference Include=""System.Xaml,", projFileText);

            // remove reference to System.Xaml
            workspace.TryApplyChanges(workspace.CurrentSolution.GetProject(project.Id).RemoveMetadataReference(mref).Solution);
            projFileText = File.ReadAllText(projFile);
            Assert.DoesNotContain(@"<Reference Include=""System.Xaml,", projFileText);
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public async Task TestAddRemoveMetadataReference_NonGACorRefAssembly()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles()
                .WithFile(@"References\MyAssembly.dll", Resources.Dlls.EmptyLibrary));

            var projFile = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
            var projFileText = File.ReadAllText(projFile);
            Assert.False(projFileText.Contains(@"MyAssembly"));

            using var workspace = CreateMSBuildWorkspace();
            var solutionFilePath = GetSolutionFileName("TestSolution.sln");
            var solution = await workspace.OpenSolutionAsync(solutionFilePath);
            var project = solution.Projects.First();

            var myAssemblyPath = GetSolutionFileName(@"References\MyAssembly.dll");
            var mref = MetadataReference.CreateFromFile(myAssemblyPath);

            // add reference to MyAssembly.dll
            workspace.TryApplyChanges(project.AddMetadataReference(mref).Solution);
            projFileText = File.ReadAllText(projFile);
            Assert.Contains(@"<Reference Include=""MyAssembly""", projFileText);
            Assert.Contains(@"<HintPath>..\References\MyAssembly.dll", projFileText);

            // remove reference MyAssembly.dll
            workspace.TryApplyChanges(workspace.CurrentSolution.GetProject(project.Id).RemoveMetadataReference(mref).Solution);
            projFileText = File.ReadAllText(projFile);
            Assert.DoesNotContain(@"<Reference Include=""MyAssembly""", projFileText);
            Assert.DoesNotContain(@"<HintPath>..\References\MyAssembly.dll", projFileText);
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled))]
        [Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
        public async Task TestAddRemoveMetadataReference_ReferenceAssembly()
        {
            CreateFiles(GetMultiProjectSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.WithSystemNumerics));

            var csProjFile = GetSolutionFileName(@"CSharpProject\CSharpProject.csproj");
            var csProjFileText = File.ReadAllText(csProjFile);
            Assert.True(csProjFileText.Contains(@"<Reference Include=""System.Numerics"""));

            var vbProjFile = GetSolutionFileName(@"VisualBasicProject\VisualBasicProject.vbproj");
            var vbProjFileText = File.ReadAllText(vbProjFile);
            Assert.False(vbProjFileText.Contains(@"System.Numerics"));

            using var workspace = CreateMSBuildWorkspace();
            var solution = await workspace.OpenSolutionAsync(GetSolutionFileName("TestSolution.sln"));
            var csProject = solution.Projects.First(p => p.Language == LanguageNames.CSharp);
            var vbProject = solution.Projects.First(p => p.Language == LanguageNames.VisualBasic);

            var numericsMetadata = csProject.MetadataReferences.Single(m => m.Display.Contains("System.Numerics"));

            // add reference to System.Xaml
            workspace.TryApplyChanges(vbProject.AddMetadataReference(numericsMetadata).Solution);
            var newVbProjFileText = File.ReadAllText(vbProjFile);
            Assert.Contains(@"<Reference Include=""System.Numerics""", newVbProjFileText);

            // remove reference MyAssembly.dll
            workspace.TryApplyChanges(workspace.CurrentSolution.GetProject(vbProject.Id).RemoveMetadataReference(numericsMetadata).Solution);
            var newVbProjFileText2 = File.ReadAllText(vbProjFile);
            Assert.DoesNotContain(@"<Reference Include=""System.Numerics""", newVbProjFileText2);
        }

        [ConditionalFact(typeof(VisualStudioMSBuildInstalled)), Trait(Traits.Feature, Traits.Features.MSBuildWorkspace)]
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
            using var workspace = CreateMSBuildWorkspace(("AlwaysCompileMarkupFilesInSeparateDomain", "false"));
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
    }
}
