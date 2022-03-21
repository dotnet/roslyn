// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;
using static Microsoft.CodeAnalysis.UnitTests.SolutionUtilities;
using static Microsoft.CodeAnalysis.UnitTests.WorkspaceTestUtilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SolutionWithSourceGeneratorTests : TestBase
    {
        // This is used to add on the preview language version which controls incremental generators being allowed.
        // TODO: remove this method entirely and the calls once incremental generators are no longer preview
        private static Project WithPreviewLanguageVersion(Project project)
        {
            return project.WithParseOptions(((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(LanguageVersion.Preview));
        }

        [Theory]
        [CombinatorialData]
        public async Task SourceGeneratorBasedOnAdditionalFileGeneratesSyntaxTrees(
            bool fetchCompilationBeforeAddingAdditionalFile,
            bool useRecoverableTrees)
        {
            // This test is just the sanity test to make sure generators work at all. There's not a special scenario being
            // tested.

            using var workspace = useRecoverableTrees ? CreateWorkspaceWithRecoverableSyntaxTreesAndWeakCompilations() : CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference);

            // Optionally fetch the compilation first, which validates that we handle both running the generator
            // when the file already exists, and when it is added incrementally.
            Compilation? originalCompilation = null;

            if (fetchCompilationBeforeAddingAdditionalFile)
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
        public async Task WithReferencesMethodCorrectlyUpdatesRunningGenerators()
        {
            using var workspace = CreateWorkspace();

            // We always have a single generator in this test, and we add or remove a second one. This is critical
            // to ensuring we correctly update our existing GeneratorDriver we may have from a prior run with the new
            // generators passed to WithAnalyzerReferences. If we only swap from zero generators to one generator,
            // we don't have a prior GeneratorDriver to update, since we don't make a GeneratorDriver if we have no generators.
            // Similarly, once we go from one back to zero, we end up getting rid of our GeneratorDriver entirely since
            // we have no need for it, as an optimization.
            var generatorReferenceToKeep = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent", hintName: "generatorReferenceToKeep"));
            var analyzerReferenceToAddAndRemove = new TestGeneratorReference(new SingleFileTestGenerator2("// More Static Content", hintName: "analyzerReferenceToAddAndRemove"));

            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(generatorReferenceToKeep);

            Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);

            // Go from one generator to two.
            project = project.WithAnalyzerReferences(new[] { generatorReferenceToKeep, analyzerReferenceToAddAndRemove });

            Assert.Equal(2, (await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees.Count());

            // And go back to one
            project = project.WithAnalyzerReferences(new[] { generatorReferenceToKeep });

            Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);
        }

        [Fact]
        public async Task IncrementalSourceGeneratorInvokedCorrectNumberOfTimes()
        {
            using var workspace = CreateWorkspace(new[] { typeof(TestCSharpCompilationFactoryServiceWithIncrementalGeneratorTracking) });
            var generator = new GenerateFileForEachAdditionalFileWithContentsCommented();
            var analyzerReference = new TestGeneratorReference(generator);
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project
                .AddAdditionalDocument("Test2.txt", "Hello, world!").Project;

            var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            var generatorDriver = project.Solution.State.GetTestAccessor().GetGeneratorDriver(project)!;

            var runResult = generatorDriver!.GetRunResult().Results[0];

            Assert.Equal(2, compilation.SyntaxTrees.Count());
            Assert.Equal(2, runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName].Length);
            Assert.All(runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });

            // Change one of the additional documents, and rerun; we should only reprocess that one change, since this
            // is an incremental generator.
            project = project.AdditionalDocuments.First().WithAdditionalDocumentText(SourceText.From("Changed text!")).Project;

            compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);
            generatorDriver = project.Solution.State.GetTestAccessor().GetGeneratorDriver(project)!;
            runResult = generatorDriver.GetRunResult().Results[0];

            Assert.Equal(2, compilation.SyntaxTrees.Count());
            Assert.Equal(2, runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName].Length);
            Assert.Contains(runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName],
                step =>
                {
                    return step.Inputs.Length == 1
                    && step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason == IncrementalStepRunReason.Modified
                    && step.Outputs.Length == 1
                    && step.Outputs[0].Reason == IncrementalStepRunReason.Modified;
                });
            Assert.Contains(runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName],
                step =>
                {
                    return step.Inputs.Length == 1
                    && step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason == IncrementalStepRunReason.Cached
                    && step.Outputs.Length == 1
                    && step.Outputs[0].Reason == IncrementalStepRunReason.Cached;
                });

            // Change one of the source documents, and rerun; we should again only reprocess that one change.
            project = project.AddDocument("Source.cs", SourceText.From("")).Project;

            compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);
            generatorDriver = project.Solution.State.GetTestAccessor().GetGeneratorDriver(project)!;
            runResult = generatorDriver.GetRunResult().Results[0];

            // We have one extra syntax tree now, but it did not require any invocations of the incremental generator.
            Assert.Equal(3, compilation.SyntaxTrees.Count());
            Assert.Equal(2, runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName].Length);
            Assert.All(runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
                });
        }

        [Fact]
        public async Task SourceGeneratorContentStillIncludedAfterSourceFileChange()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
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

        // This will make a series of changes to additional files and assert that we correctly update generated output at various times.
        // By making this a theory with a bunch of booleans, it tests that we are correctly handling the situation where we queue up multiple changes
        // to the Compilation at once.
        [Theory]
        [CombinatorialData]
        public async Task SourceGeneratorContentChangesAfterAdditionalFileChanges(
            bool assertRightAway,
            bool assertAfterAdd,
            bool assertAfterFirstChange,
            bool assertAfterSecondChange)
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference);

            if (assertRightAway)
                await AssertCompilationContainsGeneratedFilesAsync(project, expectedGeneratedContents: new string[] { });

            project = project.AddAdditionalDocument("Test.txt", "Hello, world!").Project;
            var additionalDocumentId = project.AdditionalDocumentIds.Single();

            if (assertAfterAdd)
                await AssertCompilationContainsGeneratedFilesAsync(project, "// Hello, world!");

            project = project.Solution.WithAdditionalDocumentText(additionalDocumentId, SourceText.From("Hello, everyone!")).Projects.Single();

            if (assertAfterFirstChange)
                await AssertCompilationContainsGeneratedFilesAsync(project, "// Hello, everyone!");

            project = project.Solution.WithAdditionalDocumentText(additionalDocumentId, SourceText.From("Good evening, everyone!")).Projects.Single();

            if (assertAfterSecondChange)
                await AssertCompilationContainsGeneratedFilesAsync(project, "// Good evening, everyone!");

            project = project.RemoveAdditionalDocument(additionalDocumentId);

            await AssertCompilationContainsGeneratedFilesAsync(project, expectedGeneratedContents: new string[] { });

            static async Task AssertCompilationContainsGeneratedFilesAsync(Project project, params string[] expectedGeneratedContents)
            {
                var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

                foreach (var tree in compilation.SyntaxTrees)
                    Assert.IsType<SourceGeneratedDocument>(project.GetDocument(tree));

                var texts = compilation.SyntaxTrees.Select(t => t.GetText().ToString());
                AssertEx.SetEqual(expectedGeneratedContents, texts);
            }
        }

        [Fact]
        public async Task PartialCompilationsIncludeGeneratedFilesAfterFullGeneration()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("Hello.cs", "// Source File").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var fullCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Equal(2, fullCompilation.SyntaxTrees.Count());

            var partialProject = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None).Project;
            Assert.NotSame(partialProject, project);
            var partialCompilation = await partialProject.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Same(fullCompilation, partialCompilation);
        }

        [Fact]
        public async Task DocumentIdOfGeneratedDocumentsIsStable()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var projectBeforeChange = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
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
                var project = WithPreviewLanguageVersion(AddEmptyProject(solution));
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
            var solution = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project.Solution;

            var projectIdWithGenerator = solution.ProjectIds.Single();
            var projectIdWithReference = ProjectId.CreateNewId();

            solution = solution.AddProject(projectIdWithReference, "WithReference", "WithReference", LanguageNames.CSharp)
                               .AddProjectReference(projectIdWithReference, new ProjectReference(projectIdWithGenerator));

            var compilationWithReference = await solution.GetRequiredProject(projectIdWithReference).GetRequiredCompilationAsync(CancellationToken.None);

            var compilationReference = Assert.IsAssignableFrom<CompilationReference>(Assert.Single(compilationWithReference.References));

            var compilationWithGenerator = await solution.GetRequiredProject(projectIdWithGenerator).GetRequiredCompilationAsync(CancellationToken.None);

            Assert.NotEmpty(compilationWithGenerator.SyntaxTrees);
            Assert.Same(compilationWithGenerator, compilationReference.Compilation);
        }

        [Fact]
        public async Task RequestingGeneratedDocumentsTwiceGivesSameInstance()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTreesAndWeakCompilations();

            var generatorRan = false;
            var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var generatedDocumentFirstTime = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
            var tree = await generatedDocumentFirstTime.GetSyntaxTreeAsync();

            // Fetch the compilation, and then wait for it to be GC'ed, then fetch it again. This ensures that
            // finalizing a compilation more than once doesn't recreate things incorrectly or run the generator more than once.
            generatorRan = false;
            var compilationReference = ObjectReference.CreateFromFactory(() => project.GetCompilationAsync().Result);
            compilationReference.AssertHeld();
            var secondCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            var generatedDocumentSecondTime = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());

            Assert.Same(generatedDocumentFirstTime, generatedDocumentSecondTime);
            Assert.Same(tree, secondCompilation.SyntaxTrees.Single());
            Assert.False(generatorRan);
        }

        [Fact]
        public async Task GetDocumentWithGeneratedTreeReturnsGeneratedDocument()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var syntaxTree = Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);
            var generatedDocument = Assert.IsType<SourceGeneratedDocument>(project.GetDocument(syntaxTree));
            Assert.Same(syntaxTree, await generatedDocument.GetSyntaxTreeAsync());
        }

        [Fact]
        public async Task GetDocumentWithGeneratedTreeForInProgressReturnsGeneratedDocument()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
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

        [Fact]
        public async Task TreeReusedIfGeneratedFileDoesNotChangeBetweenRuns()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent"));
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var generatedTreeBeforeChange = await Assert.Single(await project.GetSourceGeneratedDocumentsAsync()).GetSyntaxTreeAsync();

            // Mutate the regular document to produce a new compilation
            project = project.Documents.Single().WithText(SourceText.From("// Change")).Project;

            var generatedTreeAfterChange = await Assert.Single(await project.GetSourceGeneratedDocumentsAsync()).GetSyntaxTreeAsync();

            Assert.Same(generatedTreeBeforeChange, generatedTreeAfterChange);
        }

        [Fact]
        public async Task TreeNotReusedIfParseOptionsChangeChangeBetweenRuns()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent"));
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var generatedTreeBeforeChange = await Assert.Single(await project.GetSourceGeneratedDocumentsAsync()).GetSyntaxTreeAsync();

            // Mutate the parse options to produce a new compilation
            Assert.NotEqual(DocumentationMode.Diagnose, project.ParseOptions!.DocumentationMode);
            project = project.WithParseOptions(project.ParseOptions.WithDocumentationMode(DocumentationMode.Diagnose));

            var generatedTreeAfterChange = await Assert.Single(await project.GetSourceGeneratedDocumentsAsync()).GetSyntaxTreeAsync();

            Assert.NotSame(generatedTreeBeforeChange, generatedTreeAfterChange);
            Assert.Equal(DocumentationMode.Diagnose, generatedTreeAfterChange!.Options.DocumentationMode);
        }

        [Theory, CombinatorialData]
        public async Task ChangeToDocumentThatDoesNotImpactGeneratedDocumentReusesDeclarationTree(bool generatorProducesTree)
        {
            using var workspace = CreateWorkspace();

            // We'll use either a generator that produces a single tree, or no tree, to ensure we efficiently handle both cases
            ISourceGenerator generator = generatorProducesTree ? new SingleFileTestGenerator("// StaticContent")
                                                               : new CallbackGenerator(onInit: _ => { }, onExecute: _ => { });

            var analyzerReference = new TestGeneratorReference(generator);
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

            // Ensure we already have a compilation created
            _ = await project.GetCompilationAsync();

            project = await MakeChangesToDocument(project);

            var compilationAfterFirstChange = await project.GetRequiredCompilationAsync(CancellationToken.None);

            project = await MakeChangesToDocument(project);

            var compilationAfterSecondChange = await project.GetRequiredCompilationAsync(CancellationToken.None);

            // When we produced compilationAfterSecondChange, what we would ideally like is that compilation was produced by taking
            // compilationAfterFirstChange and simply updating the syntax tree that changed, since the generated documents didn't change.
            // That allows the compiler to reuse the same declaration tree for the generated file. This is hard to observe directly, but if we reflect
            // into the Compilation we can see if the declaration tree is untouched. We won't look at the original compilation, since
            // that original one was produced by adding the generated file as the final step, so it's cache won't be reusable, since the
            // compiler separates the "most recently changed tree" in the declaration table for efficiency.

            var cachedStateAfterFirstChange = GetDeclarationManagerCachedStateForUnchangingTrees(compilationAfterFirstChange);
            var cachedStateAfterSecondChange = GetDeclarationManagerCachedStateForUnchangingTrees(compilationAfterSecondChange);

            Assert.Same(cachedStateAfterFirstChange, cachedStateAfterSecondChange);

            static object GetDeclarationManagerCachedStateForUnchangingTrees(Compilation compilation)
            {
                var syntaxAndDeclarationsManager = compilation.GetFieldValue("_syntaxAndDeclarations");
                var state = syntaxAndDeclarationsManager.GetType().GetMethod("GetLazyState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(syntaxAndDeclarationsManager, null);
                var declarationTable = state.GetFieldValue("DeclarationTable");
                return declarationTable.GetFieldValue("_cache");
            }

            static async Task<Project> MakeChangesToDocument(Project project)
            {
                var existingText = await project.Documents.Single().GetTextAsync();
                var newText = existingText.WithChanges(new TextChange(new TextSpan(existingText.Length, length: 0), " With Change"));
                project = project.Documents.Single().WithText(newText).Project;
                return project;
            }
        }

        [Fact]
        public async Task CompilationNotCreatedByFetchingGeneratedFilesIfNoGeneratorsPresent()
        {
            using var workspace = CreateWorkspace();
            var project = AddEmptyProject(workspace.CurrentSolution);

            Assert.Empty(await project.GetSourceGeneratedDocumentsAsync());

            // We shouldn't have any compilation since we didn't have to run anything
            Assert.False(project.TryGetCompilation(out _));
        }

        [Fact]
        public async Task OpenSourceGeneratedUpdatedToBufferContentsWhenCallingGetOpenDocumentInCurrentContextWithChanges()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent"));
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference);

            Assert.True(workspace.SetCurrentSolution(_ => project.Solution, WorkspaceChangeKind.SolutionChanged));

            var generatedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
            var differentOpenTextContainer = SourceText.From("// Open Text").Container;

            workspace.OnSourceGeneratedDocumentOpened(differentOpenTextContainer, generatedDocument);

            generatedDocument = Assert.IsType<SourceGeneratedDocument>(differentOpenTextContainer.CurrentText.GetOpenDocumentInCurrentContextWithChanges());
            Assert.Same(differentOpenTextContainer.CurrentText, await generatedDocument.GetTextAsync());
            Assert.NotSame(workspace.CurrentSolution, generatedDocument.Project.Solution);

            var generatedTree = await generatedDocument.GetSyntaxTreeAsync();
            var compilation = await generatedDocument.Project.GetRequiredCompilationAsync(CancellationToken.None);
            Assert.Contains(generatedTree, compilation.SyntaxTrees);
        }

        [Fact]
        public async Task OpenSourceGeneratedFileDoesNotCreateNewSnapshotIfContentsKnownToMatch()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent"));
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference);

            Assert.True(workspace.SetCurrentSolution(_ => project.Solution, WorkspaceChangeKind.SolutionChanged));

            var generatedDocument = Assert.Single(await workspace.CurrentSolution.Projects.Single().GetSourceGeneratedDocumentsAsync());
            var differentOpenTextContainer = SourceText.From("// StaticContent", Encoding.UTF8).Container;

            workspace.OnSourceGeneratedDocumentOpened(differentOpenTextContainer, generatedDocument);

            generatedDocument = Assert.IsType<SourceGeneratedDocument>(differentOpenTextContainer.CurrentText.GetOpenDocumentInCurrentContextWithChanges());
            Assert.Same(workspace.CurrentSolution, generatedDocument!.Project.Solution);
        }

        [Fact]
        public async Task OpenSourceGeneratedFileMatchesBufferContentsEvenIfGeneratedFileIsMissingIsRemoved()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var originalAdditionalFile = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", SourceText.From(""));

            Assert.True(workspace.SetCurrentSolution(_ => originalAdditionalFile.Project.Solution, WorkspaceChangeKind.SolutionChanged));

            var generatedDocument = Assert.Single(await originalAdditionalFile.Project.GetSourceGeneratedDocumentsAsync());
            var differentOpenTextContainer = SourceText.From("// Open Text").Container;

            workspace.OnSourceGeneratedDocumentOpened(differentOpenTextContainer, generatedDocument);
            workspace.OnAdditionalDocumentRemoved(originalAdditionalFile.Id);

            // At this point there should be no generated documents, even though our file is still open
            Assert.Empty(await workspace.CurrentSolution.Projects.Single().GetSourceGeneratedDocumentsAsync());

            generatedDocument = Assert.IsType<SourceGeneratedDocument>(differentOpenTextContainer.CurrentText.GetOpenDocumentInCurrentContextWithChanges());
            Assert.Same(differentOpenTextContainer.CurrentText, await generatedDocument.GetTextAsync());

            var generatedTree = await generatedDocument.GetSyntaxTreeAsync();
            var compilation = await generatedDocument.Project.GetRequiredCompilationAsync(CancellationToken.None);
            Assert.Contains(generatedTree, compilation.SyntaxTrees);
        }

        [Fact]
        public async Task OpenSourceGeneratedDocumentUpdatedAndVisibleInProjectReference()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent"));
            var solution = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference).Solution;
            var projectIdWithGenerator = solution.ProjectIds.Single();

            solution = AddEmptyProject(solution).AddProjectReference(
                new ProjectReference(projectIdWithGenerator)).Solution;

            Assert.True(workspace.SetCurrentSolution(_ => solution, WorkspaceChangeKind.SolutionChanged));

            var generatedDocument = Assert.Single(await workspace.CurrentSolution.GetRequiredProject(projectIdWithGenerator).GetSourceGeneratedDocumentsAsync());
            var differentOpenTextContainer = SourceText.From("// Open Text").Container;

            workspace.OnSourceGeneratedDocumentOpened(differentOpenTextContainer, generatedDocument);

            generatedDocument = Assert.IsType<SourceGeneratedDocument>(differentOpenTextContainer.CurrentText.GetOpenDocumentInCurrentContextWithChanges());
            var generatedTree = await generatedDocument.GetSyntaxTreeAsync();

            // Fetch the compilation from the other project, it should have a compilation reference that
            // contains the generated tree
            var projectWithReference = generatedDocument.Project.Solution.Projects.Single(p => p.Id != projectIdWithGenerator);
            var compilationWithReference = await projectWithReference.GetRequiredCompilationAsync(CancellationToken.None);
            var compilationReference = Assert.Single(compilationWithReference.References.OfType<CompilationReference>());

            Assert.Contains(generatedTree, compilationReference.Compilation.SyntaxTrees);
        }

        [Fact]
        public async Task OpenSourceGeneratedDocumentsUpdateIsDocumentOpenAndCloseWorks()
        {
            using var workspace = CreateWorkspace();
            var analyzerReference = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent"));
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference);

            Assert.True(workspace.SetCurrentSolution(_ => project.Solution, WorkspaceChangeKind.SolutionChanged));

            var generatedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
            var differentOpenTextContainer = SourceText.From("// Open Text").Container;

            workspace.OnSourceGeneratedDocumentOpened(differentOpenTextContainer, generatedDocument);

            Assert.True(workspace.IsDocumentOpen(generatedDocument.Identity.DocumentId));

            var document = await workspace.CurrentSolution.GetSourceGeneratedDocumentAsync(generatedDocument.Identity.DocumentId, CancellationToken.None);
            Contract.ThrowIfNull(document);
            workspace.OnSourceGeneratedDocumentClosed(document);

            Assert.False(workspace.IsDocumentOpen(generatedDocument.Identity.DocumentId));
            Assert.Null(differentOpenTextContainer.CurrentText.GetOpenDocumentInCurrentContextWithChanges());
        }

        [Theory, CombinatorialData]
        public async Task FreezingSolutionEnsuresGeneratorsDoNotRun(bool forkBeforeFreeze)
        {
            var generatorRan = false;
            var generator = new CallbackGenerator(onInit: _ => { }, onExecute: _ => { generatorRan = true; });

            using var workspace = CreateWorkspaceWithPartialSemantics();
            var analyzerReference = new TestGeneratorReference(generator);
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

            Assert.True(workspace.SetCurrentSolution(_ => project.Solution, WorkspaceChangeKind.SolutionChanged));

            var documentToFreeze = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // The generator shouldn't have ran before any of this since we didn't do anything that would ask for a compilation
            Assert.False(generatorRan);

            if (forkBeforeFreeze)
            {
                // Forking before freezing means we'll have to do extra work to produce the final compilation, but we should still
                // not be running generators
                documentToFreeze = documentToFreeze.WithText(SourceText.From("// Changed Source File"));
            }

            var frozenDocument = documentToFreeze.WithFrozenPartialSemantics(CancellationToken.None);
            Assert.NotSame(frozenDocument, documentToFreeze);
            await frozenDocument.GetSemanticModelAsync(CancellationToken.None);

            Assert.False(generatorRan);
        }

        [Fact]
        [WorkItem(56702, "https://github.com/dotnet/roslyn/issues/56702")]
        public async Task ForkAfterFreezeNoLongerRunsGenerators()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var generatorRan = false;
            var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

            // Ensure generators are ran
            var objectReference = await project.GetCompilationAsync();

            Assert.True(generatorRan);
            generatorRan = false;

            var document = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None);

            // And fork with new contents; we'll ensure the contents of this tree are different, but the generator will still not be ran
            document = document.WithText(SourceText.From("// Something else"));

            var compilation = await document.Project.GetRequiredCompilationAsync(CancellationToken.None);
            Assert.Equal(2, compilation.SyntaxTrees.Count());
            Assert.False(generatorRan);

            Assert.Equal("// Something else", (await document.GetRequiredSyntaxRootAsync(CancellationToken.None)).ToFullString());
        }

        [Fact]
        [WorkItem(56702, "https://github.com/dotnet/roslyn/issues/56702")]
        public async Task ForkAfterFreezeNoLongerRunsGeneratorsEvenIfCompilationFallsAwayBeforeFreeze()
        {
            using var workspace = CreateWorkspaceWithPartialSemanticsAndWeakCompilations();
            var generatorRan = false;
            var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

            // Ensure generators are ran
            var compilationReference = ObjectReference.CreateFromFactory(() => project.GetCompilationAsync().Result);

            Assert.True(generatorRan);
            generatorRan = false;

            compilationReference.AssertHeld();

            var document = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None);

            // And fork with new contents; we'll ensure the contents of this tree are different, but the generator will still not be ran
            document = document.WithText(SourceText.From("// Something else"));

            var compilation = await document.Project.GetRequiredCompilationAsync(CancellationToken.None);
            Assert.Equal(2, compilation.SyntaxTrees.Count());
            Assert.False(generatorRan);

            Assert.Equal("// Something else", (await document.GetRequiredSyntaxRootAsync(CancellationToken.None)).ToFullString());
        }

        [Fact]
        public async Task ChangesToAdditionalFilesCorrectlyAppliedEvenIfCompilationFallsAway()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTreesAndWeakCompilations();
            var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
            var project = WithPreviewLanguageVersion(AddEmptyProject(workspace.CurrentSolution))
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var compilationReference = ObjectReference.CreateFromFactory(() => project.GetRequiredCompilationAsync(CancellationToken.None).Result);
            compilationReference.AssertHeld();

            var projectWithoutAdditionalFiles = project.RemoveAdditionalDocument(project.AdditionalDocumentIds.Single());
            Assert.Empty((await projectWithoutAdditionalFiles.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);
        }
    }
}
