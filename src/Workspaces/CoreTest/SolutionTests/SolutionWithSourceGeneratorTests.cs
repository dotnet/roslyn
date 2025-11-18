// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;
using static Microsoft.CodeAnalysis.UnitTests.SolutionUtilities;
using static Microsoft.CodeAnalysis.UnitTests.WorkspaceTestUtilities;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class SolutionWithSourceGeneratorTests : TestBase
{
    [Theory, CombinatorialData]
    public async Task SourceGeneratorBasedOnAdditionalFileGeneratesSyntaxTrees(
        bool fetchCompilationBeforeAddingAdditionalFile, TestHost testHost)
    {
        // This test is just the sanity test to make sure generators work at all. There's not a special scenario being
        // tested.

        var generatedFilesOutputDir = Path.Combine(TempRoot.Root, "gendir");
        var assemblyPath = Path.Combine(TempRoot.Root, "assemblyDir", "assembly.dll");

        using var workspace = CreateWorkspace(testHost: testHost);
        var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
        var project = AddEmptyProject(workspace.CurrentSolution)
            .WithCompilationOutputInfo(new CompilationOutputInfo(assemblyPath, generatedFilesOutputDir))
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

        Assert.Equal(Path.Combine(generatedFilesOutputDir, generatorType.Assembly.GetName().Name!, generatorType.FullName!, "Test.generated.cs"), generatedTree.FilePath);

        var generatedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
        Assert.Same(generatedTree, await generatedDocument.GetSyntaxTreeAsync());
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1655835")]
    public async Task WithReferencesMethodCorrectlyUpdatesWithEqualReferences(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        // AnalyzerReferences may implement equality (AnalyezrFileReference does), and we want to make sure if we substitute out one
        // reference with another reference that's equal, we correctly update generators. We'll have the underlying generators
        // be different since two AnalyzerFileReferences that are value equal but different instances would have their own generators as well.
        const string SharedPath = "Z:\\Generator.dll";
        static ISourceGenerator CreateGenerator() => new SingleFileTestGenerator("// StaticContent", hintName: "generated");

        var analyzerReference1 = new TestGeneratorReferenceWithFilePathEquality(CreateGenerator(), SharedPath);
        var analyzerReference2 = new TestGeneratorReferenceWithFilePathEquality(CreateGenerator(), SharedPath);

        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference1);

        Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);

        // Go from one analyzer reference to the other
        project = project.WithAnalyzerReferences([analyzerReference2]);

        Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);

        // Now remove and confirm that we don't have any files
        project = project.WithAnalyzerReferences([]);

        Assert.Empty((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);
    }

    private sealed class TestGeneratorReferenceWithFilePathEquality : TestGeneratorReference, IEquatable<AnalyzerReference>
    {
        public TestGeneratorReferenceWithFilePathEquality(ISourceGenerator generator, string analyzerFilePath)
            : base(generator, analyzerFilePath)
        {
        }

        public override bool Equals(object? obj) => Equals(obj as AnalyzerReference);
        public override string FullPath => base.FullPath!; // This derived class always has this non-null
        public override int GetHashCode() => this.FullPath.GetHashCode();

        public bool Equals(AnalyzerReference? other)
        {
            return other is TestGeneratorReferenceWithFilePathEquality otherReference &&
                this.FullPath == otherReference.FullPath;
        }
    }

    [Theory, CombinatorialData]
    public async Task WithReferencesMethodCorrectlyAddsAndRemovesRunningGenerators(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        // We always have a single generator in this test, and we add or remove a second one. This is critical
        // to ensuring we correctly update our existing GeneratorDriver we may have from a prior run with the new
        // generators passed to WithAnalyzerReferences. If we only swap from zero generators to one generator,
        // we don't have a prior GeneratorDriver to update, since we don't make a GeneratorDriver if we have no generators.
        // Similarly, once we go from one back to zero, we end up getting rid of our GeneratorDriver entirely since
        // we have no need for it, as an optimization.
        var generatorReferenceToKeep = new TestGeneratorReference(new SingleFileTestGenerator("// StaticContent", hintName: "generatorReferenceToKeep"));
        var analyzerReferenceToAddAndRemove = new TestGeneratorReference(new SingleFileTestGenerator2("// More Static Content", hintName: "analyzerReferenceToAddAndRemove"));

        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(generatorReferenceToKeep);

        Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);

        // Go from one generator to two.
        project = project.WithAnalyzerReferences([generatorReferenceToKeep, analyzerReferenceToAddAndRemove]);

        Assert.Equal(2, (await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees.Count());

        // And go back to one
        project = project.WithAnalyzerReferences([generatorReferenceToKeep]);

        Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);
    }

    // We only run this test on Release, as the compiler has asserts that trigger in Debug that the type names probably shouldn't be the same.
    [ConditionalTheory(typeof(IsRelease)), CombinatorialData]
    public async Task GeneratorAddedWithDifferentFilePathsProducesDistinctDocumentIds(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        // Produce two generator references with different paths, but the same generator by assembly/type. We will still give them separate
        // generator instances, because in the "real" analyzer reference case each analyzer reference produces it's own generator objects.
        var generatorReference1 = new TestGeneratorReference(new SingleFileTestGenerator("", hintName: "DuplicateFile"), analyzerFilePath: "Z:\\A.dll");
        var generatorReference2 = new TestGeneratorReference(new SingleFileTestGenerator("", hintName: "DuplicateFile"), analyzerFilePath: "Z:\\B.dll");

        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReferences([generatorReference1, generatorReference2]);

        Assert.Equal(2, (await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees.Count());

        var generatedDocuments = (await project.GetSourceGeneratedDocumentsAsync()).ToList();
        Assert.Equal(2, generatedDocuments.Count);

        Assert.NotEqual(generatedDocuments[0].Id, generatedDocuments[1].Id);
    }

    [Fact]
    public async Task IncrementalSourceGeneratorInvokedCorrectNumberOfTimes()
    {
        using var workspace = CreateWorkspace([typeof(TestCSharpCompilationFactoryServiceWithIncrementalGeneratorTracking)]);
        var generator = new GenerateFileForEachAdditionalFileWithContentsCommented();
        var analyzerReference = new TestGeneratorReference(generator);
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddAdditionalDocument("Test.txt", "Hello, world!").Project
            .AddAdditionalDocument("Test2.txt", "Hello, world!").Project;

        var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

        var generatorDriver = project.Solution.CompilationState.GetTestAccessor().GetGeneratorDriver(project)!;

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
        generatorDriver = project.Solution.CompilationState.GetTestAccessor().GetGeneratorDriver(project)!;
        runResult = generatorDriver.GetRunResult().Results[0];

        Assert.Equal(2, compilation.SyntaxTrees.Count());
        Assert.Equal(2, runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName].Length);
        Assert.Contains(runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName],
            step =>
            {
                return step.Inputs.Length == 1
                && step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason == IncrementalStepRunReason.Modified
                && step.Outputs is [{ Reason: IncrementalStepRunReason.Modified }];
            });
        Assert.Contains(runResult.TrackedSteps[GenerateFileForEachAdditionalFileWithContentsCommented.StepName],
            step =>
            {
                return step.Inputs.Length == 1
                && step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason == IncrementalStepRunReason.Cached
                && step.Outputs is [{ Reason: IncrementalStepRunReason.Cached }];
            });

        // Change one of the source documents, and rerun; we should again only reprocess that one change.
        project = project.AddDocument("Source.cs", SourceText.From("")).Project;

        compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);
        generatorDriver = project.Solution.CompilationState.GetTestAccessor().GetGeneratorDriver(project)!;
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

    [Theory, CombinatorialData]
    public async Task SourceGeneratorContentStillIncludedAfterSourceFileChange(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
        var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
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

            var generatedSyntaxTree = Assert.Single(compilation.SyntaxTrees, t => t != regularDocumentSyntaxTree);
            Assert.IsType<SourceGeneratedDocument>(project.GetDocument(generatedSyntaxTree));

            Assert.Equal(expectedGeneratedContents, generatedSyntaxTree.GetText().ToString());
        }
    }

    // This will make a series of changes to additional files and assert that we correctly update generated output at various times.
    // By making this a theory with a bunch of booleans, it tests that we are correctly handling the situation where we queue up multiple changes
    // to the Compilation at once.
    [Theory, CombinatorialData]
    public async Task SourceGeneratorContentChangesAfterAdditionalFileChanges(
        bool assertRightAway,
        bool assertAfterAdd,
        bool assertAfterFirstChange,
        bool assertAfterSecondChange,
        TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
        var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference);

        if (assertRightAway)
            await AssertCompilationContainsGeneratedFilesAsync(project, expectedGeneratedContents: []);

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

        await AssertCompilationContainsGeneratedFilesAsync(project, expectedGeneratedContents: []);

        static async Task AssertCompilationContainsGeneratedFilesAsync(Project project, params string[] expectedGeneratedContents)
        {
            var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            foreach (var tree in compilation.SyntaxTrees)
                Assert.IsType<SourceGeneratedDocument>(project.GetDocument(tree));

            var texts = compilation.SyntaxTrees.Select(t => t.GetText().ToString());
            AssertEx.SetEqual(expectedGeneratedContents, texts);
        }
    }

    [Theory, CombinatorialData]
    public async Task PartialCompilationsIncludeGeneratedFilesAfterFullGeneration(
        TestHost testHost, bool forceFreeze)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("Hello.cs", "// Source File").Project
            .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

        var fullCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

        Assert.Equal(2, fullCompilation.SyntaxTrees.Count());

        var partialProject = project.Documents.Single().WithFrozenPartialSemantics(forceFreeze, CancellationToken.None).Project;

        // If we're forcing the freeze, we must get a different project instance.  If we're not, we'll get the same
        // project since the compilation was already available.
        if (forceFreeze)
            Assert.NotSame(partialProject, project);
        else
            Assert.Same(partialProject, project);

        var partialCompilation = await partialProject.GetRequiredCompilationAsync(CancellationToken.None);

        Assert.Same(fullCompilation, partialCompilation);
    }

    [Theory, CombinatorialData]
    public async Task DocumentIdOfGeneratedDocumentsIsStable(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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

    [Theory, CombinatorialData]
    public async Task DocumentIdGuidInDifferentProjectsIsDifferent(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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

    [Theory, CombinatorialData]
    public async Task CompilationsInCompilationReferencesIncludeGeneratedSourceFiles(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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

        Assert.NotEmpty(compilationWithGenerator.SyntaxTrees);
        Assert.Same(compilationWithGenerator, compilationReference.Compilation);
    }

    [Theory, CombinatorialData]
    public async Task GetDocumentWithGeneratedTreeReturnsGeneratedDocument(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
        var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

        var syntaxTree = Assert.Single((await project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees);
        var generatedDocument = Assert.IsType<SourceGeneratedDocument>(project.GetDocument(syntaxTree));
        Assert.Same(syntaxTree, await generatedDocument.GetSyntaxTreeAsync());
    }

    [Theory, CombinatorialData]
    public async Task GetDocumentWithGeneratedTreeForInProgressReturnsGeneratedDocument(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
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

    [Theory, CombinatorialData]
    public async Task TreeReusedIfGeneratedFileDoesNotChangeBetweenRuns(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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

    [Theory, CombinatorialData]
    public async Task TreeNotReusedIfParseOptionsChangeChangeBetweenRuns(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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
    public async Task ChangeToDocumentThatDoesNotImpactGeneratedDocumentReusesDeclarationTree(bool generatorProducesTree, TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

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

    [Theory, CombinatorialData]
    public async Task CompilationNotCreatedByFetchingGeneratedFilesIfNoGeneratorsPresent(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
        var project = AddEmptyProject(workspace.CurrentSolution);

        Assert.Empty(await project.GetSourceGeneratedDocumentsAsync());

        // We shouldn't have any compilation since we didn't have to run anything
        Assert.False(project.TryGetCompilation(out _));
    }

    [Theory, CombinatorialData]
    public async Task OpenSourceGeneratedUpdatedToBufferContentsWhenCallingGetOpenDocumentInCurrentContextWithChanges(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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

    [Theory, CombinatorialData]
    public async Task OpenSourceGeneratedFileDoesNotCreateNewSnapshotIfContentsKnownToMatch(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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

    [Theory, CombinatorialData]
    public async Task OpenSourceGeneratedFileMatchesBufferContentsEvenIfGeneratedFileIsMissingIsRemoved(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
        var analyzerReference = new TestGeneratorReference(new GenerateFileForEachAdditionalFileWithContentsCommented());
        var originalAdditionalFile = AddEmptyProject(workspace.CurrentSolution)
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

    [Theory, CombinatorialData]
    public async Task OpenSourceGeneratedDocumentUpdatedAndVisibleInProjectReference(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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

    [Theory, CombinatorialData]
    public async Task OpenSourceGeneratedDocumentsUpdateIsDocumentOpenAndCloseWorks(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);
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
    public async Task FreezingSolutionEnsuresGeneratorsDoNotRun(bool forkBeforeFreeze, TestHost testHost)
    {
        var generatorRan = false;
        var generator = new CallbackGenerator(onInit: _ => { }, onExecute: _ => { generatorRan = true; });

        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
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

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/56702")]
    public async Task ForkAfterForceFreezeNoLongerRunsGenerators(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);
        generatorRan = false;

        var document = project.Documents.Single().WithFrozenPartialSemantics(forceFreeze: true, CancellationToken.None);

        // And fork with new contents; we'll ensure the contents of this tree are different, but the generator will not
        // run since we explicitly force froze.
        document = document.WithText(SourceText.From("// Something else"));

        var compilation = await document.Project.GetRequiredCompilationAsync(CancellationToken.None);
        Assert.Equal(2, compilation.SyntaxTrees.Count());
        Assert.False(generatorRan);

        Assert.Equal("// Something else", (await document.GetRequiredSyntaxRootAsync(CancellationToken.None)).ToFullString());
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/56702")]
    public async Task ForkAfterFreezeOfCompletedDocumentStillRunsGenerators(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
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

        // And fork with new contents; we'll ensure the contents of this tree are different, but the generator will run
        // since we didn't force freeze, and we got the frozen document after its compilation was already computed.
        document = document.WithText(SourceText.From("// Something else"));

        var compilation = await document.Project.GetRequiredCompilationAsync(CancellationToken.None);
        Assert.Equal(2, compilation.SyntaxTrees.Count());
        Assert.True(generatorRan);

        Assert.Equal("// Something else", (await document.GetRequiredSyntaxRootAsync(CancellationToken.None)).ToFullString());
    }

    [Theory, CombinatorialData]
    public async Task LinkedDocumentOfFrozenShouldNotRunSourceGenerator(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));

        var originalDocument1 = AddEmptyProject(workspace.CurrentSolution, name: "Project1")
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs");

        // this is a linked document of document1 above
        var originalDocument2 = AddEmptyProject(originalDocument1.Project.Solution, name: "Project2")
            .AddAnalyzerReference(analyzerReference)
            .AddDocument(originalDocument1.Name, await originalDocument1.GetTextAsync().ConfigureAwait(false), filePath: originalDocument1.FilePath);

        var frozenSolution = originalDocument2.WithFrozenPartialSemantics(CancellationToken.None).Project.Solution;
        var documentIdsToTest = new[] { originalDocument1.Id, originalDocument2.Id };

        foreach (var documentIdToTest in documentIdsToTest)
        {
            var document = frozenSolution.GetRequiredDocument(documentIdToTest);
            Assert.Single(document.GetLinkedDocumentIds());

            Assert.Equal(document.GetLinkedDocumentIds().Single(), documentIdsToTest.Except([documentIdToTest]).Single());
            document = document.WithText(SourceText.From("// Something else"));

            var compilation = await document.Project.GetRequiredCompilationAsync(CancellationToken.None);
            Assert.Single(compilation.SyntaxTrees);
            Assert.False(generatorRan);
        }
    }

    [Theory, CombinatorialData]
    public async Task DynamicFilesNotPassedToSourceGenerators(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        bool? noTreesPassed = null;

        var analyzerReference = new TestGeneratorReference(
            new CallbackGenerator(
                onInit: _ => { },
                onExecute: context => noTreesPassed = context.Compilation.SyntaxTrees.Any()));

        var project = AddEmptyProject(workspace.CurrentSolution);
        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id),
            name: "Test.cs",
            isGenerated: true).WithDesignTimeOnly(true);

        project = project.Solution.AddDocument(documentInfo).Projects.Single()
            .AddAnalyzerReference(analyzerReference);

        _ = await project.GetCompilationAsync();

        // We should have ran the generator, and it should not have had any trees
        Assert.True(noTreesPassed.HasValue);
        Assert.False(noTreesPassed!.Value);
    }

    [Theory, CombinatorialData]
    public async Task FreezingSourceGeneratedDocumentsWorks(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World"));

        var project = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference);

        var sourceGeneratedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocument.Identity;

        // Do some assertions with freezing that document
        await AssertFrozen(project, sourceGeneratedDocumentIdentity);

        // Now remove the generator, and ensure we can freeze it even if it's not there. This scenario exists for IDEs where 
        // a text buffer might still be wired up to the workspace and we're invoking a feature on it. The generated document might have gone
        // away, but we don't know that synchronously.
        project = project.RemoveAnalyzerReference(analyzerReference);
        await AssertFrozen(project, sourceGeneratedDocumentIdentity);

        static async Task AssertFrozen(Project project, SourceGeneratedDocumentIdentity identity)
        {
            var frozenWithSingleDocument = project.Solution.WithFrozenSourceGeneratedDocument(
                identity, DateTime.Now, SourceText.From("// Frozen Document"));
            Assert.Equal("// Frozen Document", (await frozenWithSingleDocument.GetTextAsync()).ToString());
            var syntaxTrees = (await frozenWithSingleDocument.Project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees;
            var frozenTree = Assert.Single(syntaxTrees);
            Assert.Equal("// Frozen Document", frozenTree.ToString());
        }
    }

    [Theory, CombinatorialData]
    public async Task FreezingSourceGeneratedDocumentsInTwoProjectsWorks(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World"));

        var solution = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference).Solution;
        var projectId1 = solution.ProjectIds.Single();
        var project2 = AddEmptyProject(solution, name: "TestProject2").AddAnalyzerReference(analyzerReference);
        solution = project2.Solution;
        var projectId2 = project2.Id;

        var sourceGeneratedDocument1 = Assert.Single(await solution.GetRequiredProject(projectId1).GetSourceGeneratedDocumentsAsync());
        var sourceGeneratedDocument2 = Assert.Single(await solution.GetRequiredProject(projectId2).GetSourceGeneratedDocumentsAsync());

        // And now freeze both of them at once
        var solutionWithFrozenDocuments = solution.WithFrozenSourceGeneratedDocuments(
            [(sourceGeneratedDocument1.Identity, DateTime.Now, SourceText.From("// Frozen 1")), (sourceGeneratedDocument2.Identity, DateTime.Now, SourceText.From("// Frozen 2"))]);

        Assert.Equal("// Frozen 1", (await (await solutionWithFrozenDocuments.GetRequiredProject(projectId1).GetSourceGeneratedDocumentsAsync()).Single().GetTextAsync()).ToString());
        Assert.Equal("// Frozen 2", (await (await solutionWithFrozenDocuments.GetRequiredProject(projectId2).GetSourceGeneratedDocumentsAsync()).Single().GetTextAsync()).ToString());
    }

    [Theory, CombinatorialData]
    public async Task FreezingWithSameContentDoesNotFork(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World"));

        var project = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference);

        var sourceGeneratedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocument.Identity;

        var frozenSolution = project.Solution.WithFrozenSourceGeneratedDocument(
            sourceGeneratedDocumentIdentity, sourceGeneratedDocument.GenerationDateTime, SourceText.From("// Hello, World"));
        Assert.Same(project.Solution, frozenSolution.Project.Solution);
    }

    [Theory, CombinatorialData]
    public async Task TestChangingGeneratorChangesChecksum(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference1 = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World 1"));
        var analyzerReference2 = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World 2"));

        var project0 = AddEmptyProject(workspace.CurrentSolution);
        var checksum0 = await project0.Solution.SolutionState.GetChecksumAsync(CancellationToken.None);

        var project1 = project0.AddAnalyzerReference(analyzerReference1);
        var checksum1 = await project1.Solution.SolutionState.GetChecksumAsync(CancellationToken.None);

        Assert.NotEqual(project0, project1);
        Assert.NotEqual(checksum0, checksum1);

        var project2 = project1.RemoveAnalyzerReference(analyzerReference1);
        var checksum2 = await project2.Solution.SolutionState.GetChecksumAsync(CancellationToken.None);

        Assert.NotEqual(project0, project2);
        Assert.NotEqual(project1, project2);

        // Should still have the same checksum that we started with, even though we have different project instances.
        Assert.Equal(checksum0, checksum2);
        Assert.NotEqual(checksum1, checksum2);

        var project3 = project2.AddAnalyzerReference(analyzerReference2);
        var checksum3 = await project3.Solution.SolutionState.GetChecksumAsync(CancellationToken.None);

        Assert.NotEqual(project0, project3);
        Assert.NotEqual(project1, project3);
        Assert.NotEqual(project2, project3);
        Assert.NotEqual(checksum0, checksum3);
        Assert.NotEqual(checksum1, checksum3);
        Assert.NotEqual(checksum2, checksum3);
    }

    [Theory, CombinatorialData]
    public async Task WithDocumentTexts_OrdinaryAndSourceGeneratedDocuments(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument = generatedDocuments.First();
        var ordinaryDocument = project.Documents.First();

        var solution = project.Solution.WithDocumentTexts(
            [(ordinaryDocument.Id, SourceText.From("// Regular modified")),
            (sourceGeneratedDocument.Id, SourceText.From("// Source gen modified"))]);

        generatedDocuments = await solution.GetRequiredProject(project.Id).GetSourceGeneratedDocumentsAsync();
        var updatedDocument = Assert.Single(generatedDocuments);
        var sourceText = await updatedDocument.GetTextAsync();
        Assert.Equal("// Source gen modified", sourceText.ToString());

        sourceText = await solution.GetRequiredDocument(ordinaryDocument.Id).GetTextAsync();
        Assert.Equal("// Regular modified", sourceText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task WithSyntaxRootWorksOnSourceGeneratedDocument(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument = generatedDocuments.First();
        var root = await sourceGeneratedDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var modifiedRoot = root.WithTrailingTrivia(root.GetLeadingTrivia());

        sourceGeneratedDocument = sourceGeneratedDocument.WithSyntaxRoot(modifiedRoot);
        var sourceText = await sourceGeneratedDocument.GetTextAsync();
        Assert.Equal("// Hello World!// Hello World!", sourceText.ToString());

        generatedDocuments = await sourceGeneratedDocument.Project.GetSourceGeneratedDocumentsAsync();
        var updatedDocument = Assert.Single(generatedDocuments);
        sourceText = await updatedDocument.GetTextAsync();
        Assert.Equal("// Hello World!// Hello World!", sourceText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task WithSyntaxRootWorksOnSourceGeneratedDocument_OldCSharpVersion(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7))
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument = generatedDocuments.First();
        var root = await sourceGeneratedDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);

        var modifiedRoot = SyntaxFactory.ParseCompilationUnit("// Changed document");
        // Default tree is the default language version
        Assert.NotEqual(LanguageVersion.CSharp7, modifiedRoot.SyntaxTree.Options.LanguageVersion());

        sourceGeneratedDocument = sourceGeneratedDocument.WithSyntaxRoot(modifiedRoot);
        var sourceText = await sourceGeneratedDocument.GetTextAsync();
        Assert.Equal("// Changed document", sourceText.ToString());

        var newTree = await sourceGeneratedDocument.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Equal(LanguageVersion.CSharp7, newTree.Options.LanguageVersion());

        generatedDocuments = await sourceGeneratedDocument.Project.GetSourceGeneratedDocumentsAsync();
        var updatedDocument = Assert.Single(generatedDocuments);
        sourceText = await updatedDocument.GetTextAsync();
        Assert.Equal("// Changed document", sourceText.ToString());

        newTree = await updatedDocument.GetRequiredSyntaxTreeAsync(CancellationToken.None);
        Assert.Equal(LanguageVersion.CSharp7, newTree.Options.LanguageVersion());
    }

    [Theory, CombinatorialData]
    public async Task WithSyntaxRootWorksOnSourceGeneratedDocument_SameRoot_Noop(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument = generatedDocuments.First();
        var root = await sourceGeneratedDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);

        sourceGeneratedDocument = sourceGeneratedDocument.WithSyntaxRoot(root);
        var sourceText = await sourceGeneratedDocument.GetTextAsync();
        Assert.Same(root, await sourceGeneratedDocument.GetSyntaxRootAsync());

        generatedDocuments = await sourceGeneratedDocument.Project.GetSourceGeneratedDocumentsAsync();
        var updatedDocument = Assert.Single(generatedDocuments);
        Assert.Same(root, await updatedDocument.GetSyntaxRootAsync());
    }

    [Theory, CombinatorialData]
    public async Task WithSyntaxRootOnSourceGeneratedDocument_AnnotationsPreserved(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var annotation = new SyntaxAnnotation("yellow");

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument = generatedDocuments.First();
        var root = await sourceGeneratedDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var modifiedRoot = root.WithAdditionalAnnotations(annotation);

        sourceGeneratedDocument = sourceGeneratedDocument.WithSyntaxRoot(modifiedRoot);
        var newRoot = await sourceGeneratedDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        Assert.True(newRoot.HasAnnotations("yellow"));
    }

    [Theory, CombinatorialData]
    public async Task WithTextWorksOnSourceGeneratedDocument(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument = generatedDocuments.First();

        sourceGeneratedDocument = sourceGeneratedDocument.WithText(SourceText.From("// Something else"));
        var sourceText = await sourceGeneratedDocument.GetTextAsync();
        Assert.Equal("// Something else", sourceText.ToString());

        generatedDocuments = await sourceGeneratedDocument.Project.GetSourceGeneratedDocumentsAsync();
        var updatedDocument = Assert.Single(generatedDocuments);
        sourceText = await updatedDocument.GetTextAsync();
        Assert.Equal("// Something else", sourceText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task WithTextWorksOnSourceGeneratedDocument_Multiple(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Hello World!"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument = generatedDocuments.First();

        sourceGeneratedDocument = sourceGeneratedDocument.WithText(SourceText.From("// Something else"));
        var sourceText = await sourceGeneratedDocument.GetTextAsync();
        Assert.Equal("// Something else", sourceText.ToString());

        sourceGeneratedDocument = sourceGeneratedDocument.WithText(SourceText.From("// Thrice is nice"));
        sourceText = await sourceGeneratedDocument.GetTextAsync();
        Assert.Equal("// Thrice is nice", sourceText.ToString());

        generatedDocuments = await sourceGeneratedDocument.Project.GetSourceGeneratedDocumentsAsync();
        var updatedDocument = Assert.Single(generatedDocuments);
        sourceText = await updatedDocument.GetTextAsync();
        Assert.Equal("// Thrice is nice", sourceText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task MultipleWithTextUnfreezesFully(TestHost testHost)
    {
        using var workspace = CreateWorkspaceWithPartialSemantics(testHost);
        var generatorRan = false;
        var analyzerReference = new TestGeneratorReference(new CallbackGenerator(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Generated document 1"));
        var analyzerReference2 = new TestGeneratorReference(new CallbackGenerator2(_ => { }, onExecute: _ => { generatorRan = true; }, source: "// Generated document 2"));
        var project = AddEmptyProject(workspace.CurrentSolution)
            .AddAnalyzerReference(analyzerReference)
            .AddAnalyzerReference(analyzerReference2)
            .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

        // Ensure generators are ran
        var objectReference = await project.GetCompilationAsync();

        Assert.True(generatorRan);

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocument1 = generatedDocuments.Single(d => d.Identity.Generator.TypeName.EndsWith("CallbackGenerator"));
        var sourceGeneratedDocument2 = generatedDocuments.Single(d => d.Identity.Generator.TypeName.EndsWith("CallbackGenerator2"));

        // Change doc 1 and make sure it worked
        var solution = sourceGeneratedDocument1.WithText(SourceText.From("// Change doc 1")).Project.Solution;
        sourceGeneratedDocument1 = await solution.GetRequiredProject(project.Id).GetSourceGeneratedDocumentAsync(sourceGeneratedDocument1.Id);
        var sourceText = await sourceGeneratedDocument1!.GetTextAsync();
        Assert.Equal("// Change doc 1", sourceText.ToString());

        // Change doc 2
        sourceGeneratedDocument2 = await solution.GetRequiredProject(project.Id).GetSourceGeneratedDocumentAsync(sourceGeneratedDocument2.Id);
        solution = sourceGeneratedDocument2!.WithText(SourceText.From("// Change doc 2")).Project.Solution;

        // Doc 1 should still be our modified version
        sourceGeneratedDocument1 = await solution.GetRequiredProject(project.Id).GetSourceGeneratedDocumentAsync(sourceGeneratedDocument1.Id);
        sourceText = await sourceGeneratedDocument1!.GetTextAsync();
        Assert.Equal("// Change doc 1", sourceText.ToString());

        // Doc 2 should have changed too
        sourceGeneratedDocument2 = await solution.GetRequiredProject(project.Id).GetSourceGeneratedDocumentAsync(sourceGeneratedDocument2.Id);
        sourceText = await sourceGeneratedDocument2!.GetTextAsync();
        Assert.Equal("// Change doc 2", sourceText.ToString());

        solution = solution.WithoutFrozenSourceGeneratedDocuments();

        // Doc 1 should be back to the original
        sourceGeneratedDocument1 = await solution.GetRequiredProject(project.Id).GetSourceGeneratedDocumentAsync(sourceGeneratedDocument1.Id);
        sourceText = await sourceGeneratedDocument1!.GetTextAsync();
        Assert.Equal("// Generated document 1", sourceText.ToString());

        // Doc 2 should be back to the original
        sourceGeneratedDocument2 = await solution.GetRequiredProject(project.Id).GetSourceGeneratedDocumentAsync(sourceGeneratedDocument2.Id);
        sourceText = await sourceGeneratedDocument2!.GetTextAsync();
        Assert.Equal("// Generated document 2", sourceText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task WithTextWorksOnUnrealizedGeneratedDocument(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World"));

        var project = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference);

        var sourceGeneratedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocument.Identity;

        // Now remove the generator, and re-freeze it as a completely new source generated document
        project = project.RemoveAnalyzerReference(analyzerReference);
        var newDocument = await FreezeAndGetDocument(project, sourceGeneratedDocumentIdentity);

        newDocument = newDocument.WithText(SourceText.From("// Changed frozen document"));

        var syntaxTrees = (await newDocument.Project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees;
        var frozenTree = Assert.Single(syntaxTrees);
        Assert.Equal("// Changed frozen document", frozenTree.ToString());

        static async Task<SourceGeneratedDocument> FreezeAndGetDocument(Project project, SourceGeneratedDocumentIdentity identity)
        {
            var frozenWithSingleDocument = project.Solution.WithFrozenSourceGeneratedDocument(
                identity, DateTime.Now, SourceText.From("// Frozen Document"));
            Assert.Equal("// Frozen Document", (await frozenWithSingleDocument.GetTextAsync()).ToString());
            var syntaxTrees = (await frozenWithSingleDocument.Project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees;
            var frozenTree = Assert.Single(syntaxTrees);
            Assert.Equal("// Frozen Document", frozenTree.ToString());
            return (SourceGeneratedDocument)frozenWithSingleDocument;
        }
    }

    [Theory, CombinatorialData]
    public async Task WithSyntaxRootWorksOnUnrealizedGeneratedDocument(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World"));

        var project = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference);

        var sourceGeneratedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocument.Identity;

        // Now remove the generator, and re-freeze it as a completely new source generated document
        project = project.RemoveAnalyzerReference(analyzerReference);
        var newDocument = await FreezeAndGetDocument(project, sourceGeneratedDocumentIdentity);

        var root = await newDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var modifiedRoot = root.WithTrailingTrivia(root.GetLeadingTrivia());
        newDocument = newDocument.WithSyntaxRoot(modifiedRoot);

        var syntaxTrees = (await newDocument.Project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees;
        var frozenTree = Assert.Single(syntaxTrees);
        Assert.Equal("// Frozen Document// Frozen Document", frozenTree.ToString());

        static async Task<SourceGeneratedDocument> FreezeAndGetDocument(Project project, SourceGeneratedDocumentIdentity identity)
        {
            var frozenWithSingleDocument = project.Solution.WithFrozenSourceGeneratedDocument(
                identity, DateTime.Now, SourceText.From("// Frozen Document"));
            Assert.Equal("// Frozen Document", (await frozenWithSingleDocument.GetTextAsync()).ToString());
            var syntaxTrees = (await frozenWithSingleDocument.Project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees;
            var frozenTree = Assert.Single(syntaxTrees);
            Assert.Equal("// Frozen Document", frozenTree.ToString());
            return (SourceGeneratedDocument)frozenWithSingleDocument;
        }
    }

    [Theory, CombinatorialData]
    public async Task SolutionChanges_IncludesFrozenSourceGeneratedDocuments(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World"));

        var project = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference);

        var sourceGeneratedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocument.Identity;

        // Do some assertions with freezing that document
        var newSolution = await FreezeDocumentAndGetSolution(project, sourceGeneratedDocumentIdentity);
        var changes = new SolutionChanges(newSolution, project.Solution);
        var documentId = Assert.Single(changes.GetExplicitlyChangedSourceGeneratedDocuments());
        Assert.Equal(documentId, sourceGeneratedDocument.Id);

        static async Task<Solution> FreezeDocumentAndGetSolution(Project project, SourceGeneratedDocumentIdentity identity)
        {
            var frozenWithSingleDocument = project.Solution.WithFrozenSourceGeneratedDocument(
                identity, DateTime.Now, SourceText.From("// Frozen Document"));
            Assert.Equal("// Frozen Document", (await frozenWithSingleDocument.GetTextAsync()).ToString());
            var syntaxTrees = (await frozenWithSingleDocument.Project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees;
            var frozenTree = Assert.Single(syntaxTrees);
            Assert.Equal("// Frozen Document", frozenTree.ToString());
            return frozenWithSingleDocument.Project.Solution;
        }
    }

    [Theory, CombinatorialData]
    public async Task SolutionChanges_ExcludesRemovedFrozenSourceGeneratedDocuments(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var analyzerReference = new TestGeneratorReference(
            new SingleFileTestGenerator("// Hello, World"));

        var project = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference);

        var sourceGeneratedDocument = Assert.Single(await project.GetSourceGeneratedDocumentsAsync());
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocument.Identity;

        // Do some assertions with freezing that document
        await FreezeDocumentAndGetSolution(project, sourceGeneratedDocumentIdentity);

        // Now remove the generator, and re-freeze it as a completely new source generated document
        project = project.RemoveAnalyzerReference(analyzerReference);
        var newSolution = await FreezeDocumentAndGetSolution(project, sourceGeneratedDocumentIdentity);

        var changes = new SolutionChanges(newSolution, project.Solution);
        Assert.Empty(changes.GetExplicitlyChangedSourceGeneratedDocuments());

        static async Task<Solution> FreezeDocumentAndGetSolution(Project project, SourceGeneratedDocumentIdentity identity)
        {
            var frozenWithSingleDocument = project.Solution.WithFrozenSourceGeneratedDocument(
                identity, DateTime.Now, SourceText.From("// Frozen Document"));
            Assert.Equal("// Frozen Document", (await frozenWithSingleDocument.GetTextAsync()).ToString());
            var syntaxTrees = (await frozenWithSingleDocument.Project.GetRequiredCompilationAsync(CancellationToken.None)).SyntaxTrees;
            var frozenTree = Assert.Single(syntaxTrees);
            Assert.Equal("// Frozen Document", frozenTree.ToString());
            return frozenWithSingleDocument.Project.Solution;
        }
    }

    [Theory, CombinatorialData]
    public async Task TwoProjectInstancesOnlyInitializeGeneratorOnce(TestHost testHost)
    {
        using var workspace = CreateWorkspace(testHost: testHost);

        var initializationCount = 0;

        var allowGeneratorToCompleteEvent = new ManualResetEventSlim(initialState: false);
        var generatorBeingInitializedEvent = new ManualResetEventSlim(initialState: false);

        var analyzerReference = new TestGeneratorReference(
            new PipelineCallbackGenerator(
                _ =>
                {
                    generatorBeingInitializedEvent.Set();
                    if (Interlocked.Increment(ref initializationCount) == 1)
                        allowGeneratorToCompleteEvent.Wait();
                }));

        // Create two projects that contain this generator, but do not request anything yet.
        var project = AddEmptyProject(workspace.CurrentSolution).AddAnalyzerReference(analyzerReference);
        var project2 = project.AddDocument("Test.cs", "").Project;

        // Now we'll request generators for both in "parallel". We'll wait until the first generator is initializing before we start the second work
        var first = Task.Run(() => project.GetCompilationAsync());

        generatorBeingInitializedEvent.Wait();

        // The generator is being initialized now, so let's start the second request
        var second = Task.Run(() => project2.GetCompilationAsync());

        allowGeneratorToCompleteEvent.Set();

        await first;
        await second;

        Assert.Equal(1, initializationCount);
    }

#if NET

    private sealed class DoNotLoadAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public static readonly IAnalyzerAssemblyLoader Instance = new DoNotLoadAssemblyLoader();

        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
            => throw new InvalidOperationException("These tests should not be loading analyzer assemblies in those host workspace, only in the remote one.");
    }

    [Theory, CombinatorialData]
    internal async Task UpdatingAnalyzerReferenceReloadsGenerators(
        SourceGeneratorExecutionPreference executionPreference)
    {
        using var workspace = CreateWorkspace([], TestHost.OutOfProcess);
        var mefServices = (VisualStudioMefHostServices)workspace.Services.HostServices;

        // Ensure the local and remote sides agree on how we're executing source generators.
        var configService = (TestWorkspaceConfigurationService)workspace.Services.GetRequiredService<IWorkspaceConfigurationService>();
        configService.Options = configService.Options with { SourceGeneratorExecution = executionPreference };

        using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);

        var workspaceConfigurationService = workspace.Services.GetRequiredService<IWorkspaceConfigurationService>();

        _ = await client.TryInvokeAsync<IRemoteInitializationService, (int, string?)>(
            (service, cancellationToken) => service.InitializeAsync(workspaceConfigurationService.Options with { SourceGeneratorExecution = executionPreference }, TempRoot.Root, cancellationToken),
            CancellationToken.None).ConfigureAwait(false);

        var solution = workspace.CurrentSolution;

        var project1 = solution.AddProject("P1", "P1", LanguageNames.CSharp);

        using var tempRoot = new TempRoot();
        var tempDirectory = tempRoot.CreateDirectory();

        var analyzerPath = Path.Combine(tempDirectory.Path, "Microsoft.CodeAnalysis.TestAnalyzerReference.dll");

        var analyzerAssemblyLoaderProvider = workspace.Services.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        // Add and test the v1 generator first.
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"Microsoft.CodeAnalysis.UnitTests.Resources.Microsoft.CodeAnalysis.TestAnalyzerReference.dll.v1"))
            using (var destination = File.OpenWrite(analyzerPath))
            {
                stream!.CopyTo(destination);
            }

            // Pass in an always throwing assembly loader so we can be sure that no loading happens on the host side.
            project1 = project1.WithAnalyzerReferences([new AnalyzerFileReference(analyzerPath, DoNotLoadAssemblyLoader.Instance)]);

            var generatedDocuments = await project1.GetSourceGeneratedDocumentsAsync();
            var helloWorldDoc = generatedDocuments.Single(d => d.Name == "HelloWorld.cs");

            var contents = await helloWorldDoc.GetTextAsync();
            Assert.True(contents.ToString().Contains("Hello, World 1!"));
        }

        // Now, overwrite the analyzer reference with a new version that generates different contents
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"Microsoft.CodeAnalysis.UnitTests.Resources.Microsoft.CodeAnalysis.TestAnalyzerReference.dll.v2"))
            using (var destination = File.OpenWrite(analyzerPath))
            {
                stream!.CopyTo(destination);
            }

            // Make a new analyzer reference to that location (note: with the same throwing assembly loader).  on the
            // host side, this will simply instantiate a new reference.  But this will cause all the machinery to run
            // syncing this new reference to the oop side, which will load the analyzer reference in a dedicated ALC.
            project1 = project1.WithAnalyzerReferences([new AnalyzerFileReference(analyzerPath, DoNotLoadAssemblyLoader.Instance)]);

            // In balanced mode, emulate the project system notifying about the updated reference on disk, which will
            // cause us to update source generators versions.
            if (executionPreference is SourceGeneratorExecutionPreference.Balanced)
            {
                Assert.True(workspace.TryApplyChanges(project1.Solution));
                workspace.EnqueueUpdateSourceGeneratorVersion(project1.Id, forceRegeneration: true);

                var waiter = (IAsynchronousOperationWaiter)mefServices.GetExportedValue<IAsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.SourceGenerators);
                await waiter.ExpeditedWaitAsync();

                project1 = workspace.CurrentSolution.GetRequiredProject(project1.Id);
            }

            var generatedDocuments = await project1.GetSourceGeneratedDocumentsAsync();
            var helloWorldDoc = generatedDocuments.Single(d => d.Name == "HelloWorld.cs");

            // Note that the contents are now different than what we saw before.  This is with an analyzer at the same path,
            // with the same assembly name and type name for the generator.  Because there is a dedicated ALC, this reloads
            // fine.
            var contents = await helloWorldDoc.GetTextAsync();
            Assert.True(contents.ToString().Contains("Hello, World 2!"));
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79587")]
    internal async Task TestChangeToExecutionVersionBeforeTryApplyChanges(
        SourceGeneratorExecutionPreference executionPreference,
        bool majorVersionUpdate)
    {
        using var workspace = TestWorkspace.CreateCSharp(
            "// First file",
            composition: FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess));

        var configService = (TestWorkspaceConfigurationService)workspace.Services.GetRequiredService<IWorkspaceConfigurationService>();
        configService.Options = configService.Options with { SourceGeneratorExecution = executionPreference };

        // want to access the true workspace solution (which will be a fork of the solution we're producing here).
        var initialSolution = workspace.CurrentSolution;
        var initialExecutionMap = initialSolution.CompilationState.SourceGeneratorExecutionVersionMap.Map;

        var projectId1 = initialSolution.Projects.Single().Id;
        Assert.True(initialExecutionMap.ContainsKey(projectId1));

        // Simulate the host making a change to the sg execution version, to force generators to rerun.
        var forceRegeneration = majorVersionUpdate;
        workspace.EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration);
        await WaitForSourceGeneratorsAsync(workspace);

        var solutionWithChangedExecutionVersion = workspace.CurrentSolution;

        // Now, fork the *original* solution and try to apply it back.  This should succeed
        // as the change to execution version should not impact the solution content version.
        var solutionWithDocumentAdded = initialSolution.AddDocument(
            DocumentId.CreateNewId(projectId1), "Y.cs", "// Contents");

        var expectVersionChange = executionPreference is SourceGeneratorExecutionPreference.Balanced || forceRegeneration;

        // The content forked solution should have an SG execution version *less than* the one we just changed.
        // Note: this will be patched up once we call TryApplyChanges.
        if (expectVersionChange)
        {
            Assert.True(
                solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]
                > solutionWithDocumentAdded.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }
        else
        {
            Assert.Equal(
                solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1],
                solutionWithDocumentAdded.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }

        Assert.True(workspace.TryApplyChanges(solutionWithDocumentAdded));

        var finalSolution = workspace.CurrentSolution;
        Assert.Equal(2, finalSolution.Projects.Single().Documents.Count());

        if (expectVersionChange)
        {
            // In balanced (or if we forced regen) mode, the execution version should have been updated to the new value.
            Assert.NotEqual(initialExecutionMap[projectId1], solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
            Assert.NotEqual(initialExecutionMap[projectId1], finalSolution.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }
        else
        {
            // In automatic mode, nothing should change wrt to execution versions (unless we specified force-regenerate).
            Assert.Equal(initialExecutionMap[projectId1], solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
            Assert.Equal(initialExecutionMap[projectId1], finalSolution.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }

        // The final execution version for the project should match the changed execution version, no matter what.
        // Proving that the content change happened, but didn't drop the execution version change.
        Assert.Equal(solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1], finalSolution.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
    }

    private static async Task WaitForSourceGeneratorsAsync(TestWorkspace workspace)
    {
        var operations = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        await operations.WaitAllAsync(workspace, [FeatureAttribute.Workspace, FeatureAttribute.SourceGenerators]);
    }

#endif
}
