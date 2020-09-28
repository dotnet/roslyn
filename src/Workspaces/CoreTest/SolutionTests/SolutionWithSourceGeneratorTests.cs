// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SolutionWithSourceGeneratorTests : TestBase
    {
        private static Project AddEmptyProject(Solution solution)
        {
            return solution.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    name: "TestProject",
                    assemblyName: "TestProject",
                    language: LanguageNames.CSharp)).Projects.Single();
        }

        [Theory]
        [CombinatorialData]
        public async Task SourceGeneratorBasedOnAdditionalFileGeneratesSyntaxTreesOnce(
            bool fetchCompilationBeforeAddingGenerator)
        {
            using var workspace = new AdhocWorkspace();
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
            Assert.Equal("Microsoft.CodeAnalysis.Workspaces.UnitTests\\Microsoft.CodeAnalysis.UnitTests.SolutionWithSourceGeneratorTests+GenerateFileForEachAdditionalFileWithContentsCommented\\Test.generated.cs", generatedTree.FilePath);
        }

        [Fact]
        public async Task SourceGeneratorContentStillIncludedAfterSourceFileChange()
        {
            using var workspace = new AdhocWorkspace();
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
                Assert.Null(project.GetDocument(generatedSyntaxTree));

                Assert.Equal(expectedGeneratedContents, generatedSyntaxTree.GetText().ToString());
            }
        }

        [Fact]
        public async Task SourceGeneratorContentChangesAfterAdditionalFileChanges()
        {
            using var workspace = new AdhocWorkspace();
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
                Assert.Null(project.GetDocument(generatedSyntaxTree));

                Assert.Equal(expectedGeneratedContents, generatedSyntaxTree.GetText().ToString());
            }
        }

        [Fact]
        public async Task PartialCompilationsIncludeGeneratedFilesAfterFullGeneration()
        {
            using var workspace = new AdhocWorkspace();
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
        public async Task CompilationsInCompilationReferencesIncludeGeneratedSourceFiles()
        {
            using var workspace = new AdhocWorkspace();
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

        private sealed class TestGeneratorReference : AnalyzerReference
        {
            private readonly ISourceGenerator _generator;

            public TestGeneratorReference(ISourceGenerator generator)
            {
                _generator = generator;
            }

            public override string? FullPath => null;
            public override object Id => this;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => ImmutableArray<DiagnosticAnalyzer>.Empty;
            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => ImmutableArray<DiagnosticAnalyzer>.Empty;
            public override ImmutableArray<ISourceGenerator> GetGenerators() => ImmutableArray.Create(_generator);
        }

        private sealed class GenerateFileForEachAdditionalFileWithContentsCommented : ISourceGenerator
        {
            public void Execute(GeneratorExecutionContext context)
            {
                foreach (var file in context.AdditionalFiles)
                {
                    AddSourceForAdditionalFile(context, file);
                }
            }

            public void Initialize(GeneratorInitializationContext context)
            {
                // TODO: context.RegisterForAdditionalFileChanges(UpdateContext);
            }

            private static void AddSourceForAdditionalFile(GeneratorExecutionContext context, AdditionalText file)
            {
                // We're going to "comment" out the contents of the file when generating this
                var sourceText = file.GetText(context.CancellationToken);
                Contract.ThrowIfNull(sourceText, "Failed to fetch the text of an additional file.");

                var changes = sourceText.Lines.SelectAsArray(l => new TextChange(new TextSpan(l.Start, length: 0), "// "));
                var generatedText = sourceText.WithChanges(changes);

                // TODO: remove the generatedText.ToString() when I don't have to specify the encoding
                context.AddSource(GetGeneratedFileName(file.Path), SourceText.From(generatedText.ToString(), encoding: Encoding.UTF8));
            }

            private static string GetGeneratedFileName(string path) => $"{Path.GetFileNameWithoutExtension(path)}.generated";
        }
    }
}
