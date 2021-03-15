// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SolutionWithSourceGeneratorTests : TestBase
    {
        [Theory]
        [CombinatorialData]
        public async Task SourceGeneratorBasedOnAdditionalFileGeneratesSyntaxTreesOnce(
            bool fetchCompilationBeforeAddingGenerator,
            bool useRecoverableTrees)
        {
            using var workspace = useRecoverableTrees ? CreateWorkspaceWithRecoverableSyntaxTreesAndWeakCompilations() : CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented() { });
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference);

            // Optionally fetch the compilation first, which validates that we handle both running the generator
            // when the file already exists, and when it is added incrementally.
            Compilation? originalCompilation = null;

            if (fetchCompilationBeforeAddingGenerator)
            {
                originalCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);
            }

            project = project.AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var newCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.NotSame(originalCompilation, newCompilation);
            var generatedTree = Assert.Single(newCompilation.SyntaxTrees);
            var generatorType = typeof(GenerateFileForEachAdditionalFileWithContentsCommented);

            Assert.Equal($"{generatorType.Assembly.GetName().Name}\\{generatorType.FullName}\\Test.generated.cs", generatedTree.FilePath);

            var generatedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
            Assert.Same(generatedTree, await generatedDocument.GetSyntaxTreeAsync());
        }

        [Fact]
        public async Task SourceGeneratorContentStillIncludedAfterSourceFileChange()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented() { });
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("Hello.cs", "// Source File").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var documentId = project.DocumentIds.Single();

            await AssertCompilationContainsOneRegularAndOneGeneratedFile(project, documentId, "// Hello, world!");

            project = project.Solution.WithDocumentText(documentId, SourceText.From("// Changed Source File")).Projects.Single();

            await AssertCompilationContainsOneRegularAndOneGeneratedFile(project, documentId, "// Hello, world!");

            static async Task AssertCompilationContainsOneRegularAndOneGeneratedFile(Project project, DocumentId documentId, string expectedGeneratedContents)
            {
                var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

                var regularDocumentSyntaxTree = await project.GetRequiredDocument(documentId).GetRequiredSyntaxTreeAsync(CancellationToken.None);
                Assert.Contains(regularDocumentSyntaxTree, compilation.SyntaxTrees);

                var generatedSyntaxTree = Assert.Single(compilation.SyntaxTrees.Where(t => t != regularDocumentSyntaxTree));
                Assert.IsType<SourceGeneratedDocument>(project.GetDocument(generatedSyntaxTree));

                Assert.Equal(expectedGeneratedContents, generatedSyntaxTree.GetText().ToString());
            }
        }

        [Fact]
        public async Task SourceGeneratorContentChangesAfterAdditionalFileChanges()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented() { });
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var additionalDocumentId = project.AdditionalDocumentIds.Single();

            await AssertCompilationContainsGeneratedFile(project, "// Hello, world!");

            project = project.Solution.WithAdditionalDocumentText(additionalDocumentId, SourceText.From("Hello, everyone!")).Projects.Single();

            await AssertCompilationContainsGeneratedFile(project, "// Hello, everyone!");

            static async Task AssertCompilationContainsGeneratedFile(Project project, string expectedGeneratedContents)
            {
                var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

                var generatedSyntaxTree = Assert.Single(compilation.SyntaxTrees);
                Assert.IsType<SourceGeneratedDocument>(project.GetDocument(generatedSyntaxTree));

                Assert.Equal(expectedGeneratedContents, generatedSyntaxTree.GetText().ToString());
            }
        }

        [Fact]
        public async Task PartialCompilationsIncludeGeneratedFilesAfterFullGeneration()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("Hello.cs", "// Source File").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var fullCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Equal(2, fullCompilation.SyntaxTrees.Count());

            var partialProject = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None).Project;
            var partialCompilation = await partialProject.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Same(fullCompilation, partialCompilation);
        }

        [Fact]
        public async Task DocumentIdOfGeneratedDocumentsIsStable()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var projectBeforeChange = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var generatedDocumentBeforeChange = Assert.Single(await projectBeforeChange.GetSourceGeneratedDocumentsAsync());

            var projectAfterChange =
                projectBeforeChange.Solution.WithAdditionalDocumentText(
                    projectBeforeChange.AdditionalDocumentIds.Single(),
                    SourceText.From("Hello, world!!!!")).Projects.Single();

            var generatedDocumentAfterChange = Assert.Single(await projectAfterChange.GetSourceGeneratedDocumentsAsync());

            Assert.NotSame(generatedDocumentBeforeChange, generatedDocumentAfterChange);
            Assert.Equal(generatedDocumentBeforeChange.Id, generatedDocumentAfterChange.Id);
        }

        [Fact]
        public async Task DocumentIdGuidInDifferentProjectsIsDifferent()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());

            var solutionWithProjects = AddProjectWithReference(workspace.CurrentSolution, analyzerReference);
            solutionWithProjects = AddProjectWithReference(solutionWithProjects, analyzerReference);

            var projectIds = solutionWithProjects.ProjectIds.ToList();

            var generatedDocumentsInFirstProject = await solutionWithProjects.GetRequiredProject(projectIds[0]).GetSourceGeneratedDocumentsAsync();
            var generatedDocumentsInSecondProject = await solutionWithProjects.GetRequiredProject(projectIds[1]).GetSourceGeneratedDocumentsAsync();

            // A DocumentId consists of a GUID and then the ProjectId it's within. Even if these two documents have the same GUID,
            // they'll still be not equal because of the different ProjectIds. However, we'll also assert the GUIDs should be different as well,
            // because otherwise things can get confusing. If nothing else, the DocumentId debugger display string shows only the GUID, so you could
            // easily confuse them as being the same.
            Assert.NotEqual(generatedDocumentsInFirstProject.Single().Id.Id, generatedDocumentsInSecondProject.Single().Id.Id);

            static Solution AddProjectWithReference(Solution solution, TestGeneratorReference analyzerReference)
            {
                var project = AddEmptyProject(solution);
                project = project.AddAnalyzerReference(analyzerReference);
                project = project.AddAdditionalDocument("Test.txt", "Hello, world!").Project;

                return project.Solution;
            }
        }

        [Fact]
        public async Task CompilationsInCompilationReferencesIncludeGeneratedSourceFiles()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var solution = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project.Solution;

            var projectIdWithGenerator = solution.ProjectIds.Single();
            var projectIdWithReference = ProjectId.CreateNewId();

            solution = solution.AddProject(projectIdWithReference, "WithReference", "WithReference", LanguageNames.CSharp)
                               .AddProjectReference(projectIdWithReference, new ProjectReference(projectIdWithGenerator));

            var compilationWithReference = await solution.GetRequiredProject(projectIdWithReference).GetRequiredCompilationAsync(CancellationToken.None);

            var compilationReference = Assert.IsAssignableFrom<CompilationReference>(Assert.Single(compilationWithReference.References));

            var compilationWithGenerator = await solution.GetRequiredProject(projectIdWithGenerator).GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Same(compilationWithGenerator, compilationReference.Compilation);
        }

        [Fact]
        public async Task RequestingGeneratedDocumentsTwiceGivesSameInstance()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTreesAndWeakCompilations();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var generatedDocumentFirstTime = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
            var tree = await generatedDocumentFirstTime.GetSyntaxTreeAsync();

            // Fetch the compilation, and then wait for it to be GC'ed, then fetch it again. This ensures that
            // finalizing a compilation more than once doesn't recreate things incorrectly.
            var compilationReference = ObjectReference.CreateFromFactory(() => project.GetCompilationAsync().Result);
            compilationReference.AssertReleased();
            var secondCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            var generatedDocumentSecondTime = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());

            Assert.Same(generatedDocumentFirstTime, generatedDocumentSecondTime);
            Assert.Same(tree, secondCompilation.SyntaxTrees.Single());
        }

        [Fact]
        public async Task GetDocumentWithGeneratedTreeReturnsGeneratedDocument()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var syntaxTree = Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);
            var generatedDocument = Assert.IsType<SourceGeneratedDocument>(project.GetDocument(syntaxTree));
            Assert.Same(syntaxTree, await generatedDocument.GetSyntaxTreeAsync());
        }

        [Fact]
        public async Task GetDocumentWithGeneratedTreeForInProgressReturnsGeneratedDocument()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            // Ensure we've ran generators at least once
            await project.GetCompilationAsync();

            // Produce an in-progress snapshot
            project = project.Documents.Single(d => d.Name == "RegularDocument.cs").WithFrozenPartialSemantics(CancellationToken.None).Project;

            // The generated tree should still be there; even if the regular compilation fell away we've now cached the 
            // generated trees.
            var syntaxTree = Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees, t => t.FilePath != "RegularDocument.cs");
            var generatedDocument = Assert.IsType<SourceGeneratedDocument>(project.GetDocument(syntaxTree));
            Assert.Same(syntaxTree, await generatedDocument.GetSyntaxTreeAsync());
        }
    }
}
